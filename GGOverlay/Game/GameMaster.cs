using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Networking;
using Newtonsoft.Json;
using System.Windows;
using System.Net.Sockets;

namespace GGOverlay.Game
{
    public class GameMaster : IGameInterface
    {
        // NetworkServer object for hosting the game
        private NetworkServer _networkServer;

        // Player information
        public List<PlayerInfo> _players { get; set; }
        private Dictionary<TcpClient, PlayerInfo> _clientPlayerMap;

        // Local Player
        public PlayerInfo _localPlayer { get; set; }

        // Game rules
        public GameRules _gameRules { get; set; }

        // UserData
        public UserData UserData { get; set; }

        public event Action<string> OnLog;
        public event Action UIUpdate;

        // Nothing, here for client
        public event Action OnDisconnect;

        // Constructor initializes the objects
        public GameMaster()
        {
            _networkServer = new NetworkServer();
            _players = new List<PlayerInfo>();
            _gameRules = new GameRules();
            _clientPlayerMap = new Dictionary<TcpClient, PlayerInfo>();

            // Load UserData
            UserData = UserData.Load();

            // If UserData.LocalPlayer is set, assign to _localPlayer
            if (UserData.LocalPlayer != null)
            {
                _localPlayer = UserData.LocalPlayer;
                var playerList = _clientPlayerMap.Values.ToList();
                playerList.Add(_localPlayer);

                _players = playerList;
            }

            // Setup logging for network server
            _networkServer.OnLog += LogMessage;

            // Setup event handlers
            _networkServer.OnClientConnected += OnClientConnected;
            _networkServer.OnMessageReceived += OnServerMessageReceived;
            _networkServer.OnClientDisconnected += OnClientDisconnected; // Subscribe to client disconnection

        }

        public async Task SetGameRules(string filepath)
        {
            _gameRules.LoadFromFile(filepath);
            UIUpdate?.Invoke();
            await BroadcastMessageAsync("RULEUPDATE:" + _gameRules.Send());
        }

        // In GameMaster.cs
        public async void BroadcastGameRules()
        {
            if (_gameRules != null)
            {
                await BroadcastMessageAsync("RULEUPDATE:" + _gameRules.Send());
            }
        }

        // Set the local player information
        public async void EditPlayer(string name, double drinkModifier)
        {
            _localPlayer = new PlayerInfo(name, drinkModifier);
            UserData.LocalPlayer = _localPlayer;
            UserData.Save(); // Save UserData
            await SendPlayerListUpdateAsync();
            UIUpdate?.Invoke();
        }

        // Start method for hosting the game
        public async Task Start(int port, string ipAddress = null)
        {
            if (_networkServer != null)
            {
                try
                {
                    await _networkServer.StartAsync(port);
                }
                catch (Exception ex)
                {
                    LogMessage($"Error hosting the game: {ex.Message}");
                    throw;
                }
            }
            else
            {
                LogMessage("Error: Server object is null.");
            }
        }

        // Handle client connections to the server
        private async void OnClientConnected(TcpClient client)
        {
            LogMessage($"Client connected: {client.Client.RemoteEndPoint}");
            if (_gameRules != null)
            {
                await _networkServer.SendMessageToClientAsync("RULEUPDATE:" + _gameRules.Send(), client);
            }
            if (_clientPlayerMap.Count > 0 || _localPlayer != null)
            {
                await SendPlayerListUpdateAsync();
            }
        }

        private async void OnServerMessageReceived(string message, TcpClient client)
        {
            LogMessage($"Received from client {client.Client.RemoteEndPoint}: {message}");

            // Check if the message is a player update
            if (message.StartsWith("PLAYERUPDATE:"))
            {
                // Extract the serialized player info from the message
                string serializedPlayer = message.Substring("PLAYERUPDATE:".Length);
                PlayerInfo updatedPlayer = new PlayerInfo();

                try
                {
                    // Deserialize the received player info
                    updatedPlayer.Receive(serializedPlayer);
                }
                catch (Exception ex)
                {
                    LogMessage($"Error deserializing player info: {ex.Message}");
                    return;
                }

                // Check if the TcpClient already exists in the dictionary
                if (_clientPlayerMap.ContainsKey(client))
                {
                    // Update the existing player's information
                    _clientPlayerMap[client] = updatedPlayer;
                    LogMessage($"Updated player info for client {client.Client.RemoteEndPoint}");
                }
                else
                {
                    // Add the new player information
                    _clientPlayerMap.Add(client, updatedPlayer);
                    LogMessage($"Added new player info for client {client.Client.RemoteEndPoint}");
                }

                // Send an updated player list to all clients
                await SendPlayerListUpdateAsync();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    UIUpdate?.Invoke();
                });
            }
        }

        // Handle client disconnections
        private async void OnClientDisconnected(TcpClient client)
        {
            LogMessage($"Client disconnected: {client.Client.RemoteEndPoint}");

            // Remove the disconnected client from the player map
            if (_clientPlayerMap.ContainsKey(client))
            {
                _clientPlayerMap.Remove(client);
                LogMessage($"Removed player info for disconnected client {client.Client.RemoteEndPoint}");
            }

            // Send an updated player list to all clients
            await SendPlayerListUpdateAsync();
            Application.Current.Dispatcher.Invoke(() =>
            {
                UIUpdate?.Invoke();
            });
        }

        // Method to send a PlayerListUpdate with the current list of player infos
        private async Task SendPlayerListUpdateAsync()
        {
            try
            {
                // Create a list of all player infos
                var playerList = _clientPlayerMap.Values.ToList();
                playerList.Add(_localPlayer);

                _players = playerList;

                // Serialize the player list to a JSON string
                string serializedPlayerList = JsonConvert.SerializeObject(playerList, Formatting.Indented);

                // Send the PlayerListUpdate message to all clients
                string message = $"PlayerListUpdate:{serializedPlayerList}";
                await _networkServer.BroadcastMessageAsync(message);
                LogMessage("Sent updated player list to all clients.");
            }
            catch (Exception ex)
            {
                LogMessage($"Error sending player list update: {ex.Message}");
            }
        }

        public void RequestUIUpdate()
        {
            UIUpdate?.Invoke();
        }

        // Send a message to all clients
        private async Task BroadcastMessageAsync(string message)
        {
            await _networkServer.BroadcastMessageAsync(message);
            LogMessage($"Broadcasted message: {message}");
        }

        // Log messages to the console or handle through a logger
        private void LogMessage(string message)
        {
            OnLog?.Invoke(message);
        }

        // Stop the server
        public void Stop()
        {
            _networkServer.Stop();
            UserData.Save(); // Save UserData when stopping
        }
    }
}
