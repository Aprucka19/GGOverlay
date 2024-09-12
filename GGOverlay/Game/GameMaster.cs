using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using GGOverlay.Game;
using Networking;

namespace GGOverlay.Game
{
    public class GameMaster
    {
        // Network objects
        private NetworkClient _networkClient;
        private NetworkServer _networkServer;

        // Player information
        private PlayerInfo _localPlayer;
        private List<PlayerInfo> _players;

        // Game rules
        private GameRules _gameRules;

        // Constructor initializes the objects
        public GameMaster()
        {
            _networkClient = new NetworkClient();
            _networkServer = new NetworkServer();
            _players = new List<PlayerInfo>();
            _gameRules = new GameRules();

            // Setup logging for network client and server
            _networkClient.OnLog += LogMessage;
            _networkServer.OnLog += LogMessage;

            // Setup event handlers
            _networkServer.OnClientConnected += OnClientConnected;
            _networkClient.OnMessageReceived += OnMessageReceived;
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
                LogMessage($"Player {player.Name} is already in the game.");
            }
        }

        // Set the game rules
        public void SetGameRules(GameRules rules)
        {
            _gameRules = rules;
            LogMessage("Game rules set.");
        }

        // Host a game by starting the server
        public async Task HostGame(int port)
        {
            if (_networkServer != null)
            {
                await _networkServer.StartAsync(port);
                LogMessage("Game hosted successfully.");
            }
            else
            {
                LogMessage("Error: Server could not be started.");
            }
        }

        // Join a game by connecting to a server at the given IP
        public async Task JoinGame(string ipAddress, int port)
        {
            if (_networkClient != null)
            {
                await _networkClient.ConnectAsync(ipAddress, port);
                LogMessage("Joined game successfully.");
            }
            else
            {
                LogMessage("Error: Client could not connect to the server.");
            }
        }

        // Handle client connections to the server
        private void OnClientConnected(TcpClient client)
        {
            LogMessage($"Client connected: {client.Client.RemoteEndPoint}");
            // Handle any further actions when a client connects
        }

        // Handle incoming messages from the network
        private void OnMessageReceived(string message)
        {
            LogMessage($"Message received: {message}");
            // Handle the message as per game logic
        }

        // Log messages to the console or handle through a logger
        private void LogMessage(string message)
        {
            Console.WriteLine(message);
            // You can connect this to a logging system or UI as needed
        }
    }
}
