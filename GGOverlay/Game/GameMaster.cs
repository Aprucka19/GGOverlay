using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using Networking;

namespace GGOverlay.Game
{
    public class GameMaster
    {
        // NetworkServer object for hosting the game
        private NetworkServer _networkServer;

        // Player information
        private List<PlayerInfo> _players;

        // Game rules
        public GameRules _gameRules;

        public event Action<string> OnLog;
        public event Action UIUpdate;

        // Constructor initializes the objects
        public GameMaster()
        {
            _networkServer = new NetworkServer();
            _players = new List<PlayerInfo>();
            _gameRules = new GameRules();

            // Setup logging for network server
            _networkServer.OnLog += LogMessage;

            // Setup event handlers
            _networkServer.OnClientConnected += OnClientConnected;
            _networkServer.OnMessageReceived += OnServerMessageReceived;
        }

        public async Task SetGameRules(string filepath)
        {
            _gameRules.LoadFromFile(filepath);
            UIUpdate?.Invoke();
            await BroadcastMessageAsync("RULEUPDATE:"+_gameRules.Send());
        }


        // Host a game by starting the server
        public async Task HostGame(int port)
        {
            if (_networkServer != null)
            {
                try
                {
                    await _networkServer.StartAsync(port);
                }
                catch (Exception ex)
                {
                    // Log the error if an exception occurs during server startup
                    LogMessage($"Error hosting the game: {ex.Message}");
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
            if(_gameRules != null)
            {
                await _networkServer.SendMessageToClientAsync("RULEUPDATE:" + _gameRules.Send(),client);
            }
        }

        // Handle incoming messages from clients
        private async void OnServerMessageReceived(string message, TcpClient client)
        {
            LogMessage($"Received from client {client.Client.RemoteEndPoint}: {message}");
            await _networkServer.BroadcastMessageToAllExceptOneAsync(message, client); // Relay the message to other clients
        }

        // Send a message to all clients
        public async Task BroadcastMessageAsync(string message)
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
        public void StopServer()
        {
            _networkServer.Stop();
        }
    }
}
