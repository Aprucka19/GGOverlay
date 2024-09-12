using System;
using System.Windows;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Threading; // Required for Dispatcher
using Networking;

namespace GGOverlay.Game
{
    public class GameClient
    {
        // NetworkClient object for connecting to the game
        private NetworkClient _networkClient;

        // Local player information
        private PlayerInfo _localPlayer;

        // List of all players, including the local player
        private List<PlayerInfo> _players;

        public GameRules _gameRules;

        public event Action<string> OnLog;
        public event Action OnDisconnectedGameClient;
        public event Action UIUpdate;

        // Constructor initializes the objects
        public GameClient()
        {
            _networkClient = new NetworkClient();
            _players = new List<PlayerInfo>();
            _gameRules = new GameRules();

            // Setup logging for network client
            _networkClient.OnLog += LogMessage;

            // Setup event handlers
            _networkClient.OnMessageReceived += OnMessageReceived;
            _networkClient.OnDisconnected += OnDisconnected;
        }

        // Set the local player information
        public void SetLocalPlayer(PlayerInfo player)
        {
            _localPlayer = player;
            AddPlayer(player); // Add local player to the player list
            LogMessage($"Local player set: {player.Name}");
        }

        // Add a player to the game
        public void AddPlayer(PlayerInfo player)
        {
            if (player != null && !_players.Exists(p => p.Name == player.Name))
            {
                _players.Add(player);
                LogMessage($"Player added: {player.Name}");
            }
            else
            {
                LogMessage($"Player {player?.Name} is already in the game.");
            }
        }

        // Join a game by connecting to a server at the given IP
        public async Task<bool> JoinGame(string ipAddress, int port)
        {
            if (_networkClient != null)
            {
                try
                {
                    await _networkClient.ConnectAsync(ipAddress, port, 5); // 5 seconds timeout
                    LogMessage("Joined game successfully.");
                    return true;
                }
                catch (TimeoutException ex)
                {
                    LogMessage($"Connection timed out: {ex.Message}");
                }
                catch (Exception ex)
                {
                    LogMessage($"Error connecting to server: {ex.Message}");
                }
            }
            else
            {
                LogMessage("Error: Client object is null.");
            }
            return false;
        }

        // Send a message to the server
        public async Task SendMessageAsync(string message)
        {
            if (_networkClient != null && _networkClient.IsConnected)
            {
                await _networkClient.SendMessageAsync(message);
                LogMessage($"Sent message: {message}");
            }
        }

        // Handle incoming messages from the server
        private void OnMessageReceived(string message)
        {
            LogMessage($"Message received: {message}");

            // Check if the message is a game rules update
            if (message.StartsWith("RULEUPDATE:"))
            {
                // Extract the serialized rules from the message
                string serializedRules = message.Substring("RULEUPDATE:".Length);

                // Receive and apply the game rules
                _gameRules.Receive(serializedRules);
                LogMessage("Game rules updated successfully.");

                // Safely invoke UI updates on the main thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    UIUpdate?.Invoke();
                });
            }
        }

        // Handle client disconnection
        private void OnDisconnected()
        {
            LogMessage("Disconnected from server.");

            // Safely invoke disconnection on the main thread
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                OnDisconnectedGameClient?.Invoke();
            });
        }

        // Log messages to the console or handle through a logger
        private void LogMessage(string message)
        {
            OnLog?.Invoke(message);
        }

        // Disconnect from the server
        public void Disconnect()
        {
            _networkClient.Disconnect();
            LogMessage("Client disconnected.");
        }
    }
}
