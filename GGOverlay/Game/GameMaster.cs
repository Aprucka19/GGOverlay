using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Networking;
using Newtonsoft.Json;
using System.Windows;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

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

        // Last rule trigger times
        private Dictionary<string, DateTime> _lastRuleTriggerTime;

        public event Action<string> OnLog;
        public event Action UIUpdate;
        public event Action<Rule, PlayerInfo> OnIndividualPunishmentTriggered;
        public event Action<Rule> OnGroupPunishmentTriggered;

        // Added OnDisconnect event
        public event Action OnDisconnect;

        // Constructor initializes the objects
        public GameMaster()
        {
            _networkServer = new NetworkServer();
            _players = new List<PlayerInfo>();
            _gameRules = new GameRules();
            _clientPlayerMap = new Dictionary<TcpClient, PlayerInfo>();
            _lastRuleTriggerTime = new Dictionary<string, DateTime>();

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
            else if (message.StartsWith("TRIGGERINDIVIDUALRULE:"))
            {
                string[] parts = message.Substring("TRIGGERINDIVIDUALRULE:".Length).Split(new[] { ':' }, 2);
                if (parts.Length == 2)
                {
                    string serializedRule = parts[0];
                    string serializedPlayer = parts[1];

                    try
                    {
                        Rule rule = JsonConvert.DeserializeObject<Rule>(serializedRule);
                        PlayerInfo player = JsonConvert.DeserializeObject<PlayerInfo>(serializedPlayer);

                        await HandleTriggerIndividualRule(rule, player);
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Error deserializing rule or player info: {ex.Message}");
                    }
                }
            }
            else if (message.StartsWith("TRIGGERGROUPRULE:"))
            {
                string serializedRule = message.Substring("TRIGGERGROUPRULE:".Length);

                try
                {
                    Rule rule = JsonConvert.DeserializeObject<Rule>(serializedRule);
                    await HandleTriggerGroupRule(rule);
                }
                catch (Exception ex)
                {
                    LogMessage($"Error deserializing rule: {ex.Message}");
                }
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
                if (_localPlayer != null)
                {
                    playerList.Add(_localPlayer);
                }

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

        // Handle TriggerIndividualRule logic
        private async Task HandleTriggerIndividualRule(Rule rule, PlayerInfo player)
        {
            // Generate a unique key for the rule and player combo
            string ruleKey = $"INDIVIDUAL:{GetRuleKey(rule)}:{GetPlayerKey(player)}";

            DateTime now = DateTime.Now;

            if (_lastRuleTriggerTime.ContainsKey(ruleKey))
            {
                DateTime lastTriggerTime = _lastRuleTriggerTime[ruleKey];
                if ((now - lastTriggerTime).TotalSeconds < 10)
                {
                    // Ignore the trigger
                    LogMessage($"Ignored trigger of rule '{rule.ToString}' for player '{player.Name}' due to cooldown.");
                    return;
                }
            }

            // Update the last trigger time
            _lastRuleTriggerTime[ruleKey] = now;

            // Calculate adjusted punishment quantity
            int adjustedPunishmentQuantity = (int)Math.Round(rule.PunishmentQuantity * player.DrinkModifier, MidpointRounding.AwayFromZero);

            // Find the player in the list
            PlayerInfo targetPlayer = _players.FirstOrDefault(p => p.Name == player.Name);
            if (targetPlayer != null)
            {
                // Add the adjusted punishment quantity to the player's drink count
                targetPlayer.DrinkCount += adjustedPunishmentQuantity;
            }

            // Send out a Player Update
            await SendPlayerListUpdateAsync();
            UIUpdate?.Invoke();

            // Send out a message to all clients to trigger the rule
            string serializedRule = JsonConvert.SerializeObject(rule);
            string serializedPlayer = JsonConvert.SerializeObject(player);
            string triggerMessage = $"TRIGGERINDIVIDUALRULE:{serializedRule}:{serializedPlayer}";
            await BroadcastMessageAsync(triggerMessage);

            // Invoke the punishment for the GameMaster
            OnIndividualPunishmentTriggered?.Invoke(rule, player);
        }

        // Handle TriggerGroupRule logic
        private async Task HandleTriggerGroupRule(Rule rule)
        {
            string ruleKey = $"GROUP:{GetRuleKey(rule)}";

            DateTime now = DateTime.Now;

            if (_lastRuleTriggerTime.ContainsKey(ruleKey))
            {
                DateTime lastTriggerTime = _lastRuleTriggerTime[ruleKey];
                if ((now - lastTriggerTime).TotalSeconds < 10)
                {
                    // Ignore the trigger
                    LogMessage($"Ignored trigger of group rule '{rule.ToString}' due to cooldown.");
                    return;
                }
            }

            // Update the last trigger time
            _lastRuleTriggerTime[ruleKey] = now;

            // Update drink counters for all players
            foreach (var player in _players)
            {
                int adjustedPunishmentQuantity = (int)Math.Round(rule.PunishmentQuantity * player.DrinkModifier, MidpointRounding.AwayFromZero);
                player.DrinkCount += adjustedPunishmentQuantity;
            }

            // Send out a Player Update
            await SendPlayerListUpdateAsync();
            UIUpdate?.Invoke();

            // Send out a message to all clients to trigger the rule
            string serializedRule = JsonConvert.SerializeObject(rule);
            string triggerMessage = $"TRIGGERGROUPRULE:{serializedRule}";
            await BroadcastMessageAsync(triggerMessage);

            // Invoke the punishment for the GameMaster
            OnGroupPunishmentTriggered?.Invoke(rule);
        }

        // Helper methods to generate unique keys
        private string GetRuleKey(Rule rule)
        {
            string serializedRule = JsonConvert.SerializeObject(rule);
            return ComputeHash(serializedRule);
        }

        private string GetPlayerKey(PlayerInfo player)
        {
            string serializedPlayer = JsonConvert.SerializeObject(player);
            return ComputeHash(serializedPlayer);
        }

        private string ComputeHash(string input)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = sha256.ComputeHash(inputBytes);
                // Convert to a hexadecimal string
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        public void TriggerGroupRule(Rule rule)
        {
            // Since GameMaster is the server, we handle the trigger directly
            _ = HandleTriggerGroupRule(rule);
        }

        public void TriggerIndividualRule(Rule rule, PlayerInfo player)
        {
            // Since GameMaster is the server, we handle the trigger directly
            _ = HandleTriggerIndividualRule(rule, player);
        }
    }
}
