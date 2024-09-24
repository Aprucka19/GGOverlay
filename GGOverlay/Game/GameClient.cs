﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Networking;
using Newtonsoft.Json;

namespace GGOverlay.Game
{
    public class GameClient : IGameInterface
    {
        // NetworkClient object for connecting to the game
        private NetworkClient _networkClient;

        // Local player information
        public PlayerInfo _localPlayer { get; set; }

        // List of all players, including the local player
        public List<PlayerInfo> _players { get; set; }

        public GameRules _gameRules { get; set; }

        // UserData
        public UserData UserData { get; set; }

        public event Action<string> OnLog;
        public event Action OnDisconnect;
        public event Action UIUpdate;
        public event Action<Rule, PlayerInfo> OnIndividualPunishmentTriggered;
        public event Action<Rule> OnGroupPunishmentTriggered;

        // Constructor initializes the objects
        public GameClient()
        {
            _networkClient = new NetworkClient();
            _gameRules = new GameRules();
            _players = new List<PlayerInfo>();

            // Load UserData
            UserData = UserData.Load();

            // If UserData.LocalPlayer is set, assign to _localPlayer
            if (UserData.LocalPlayer != null)
            {
                _localPlayer = UserData.LocalPlayer;
            }

            // Setup logging for network client
            _networkClient.OnLog += LogMessage;

            // Setup event handlers
            _networkClient.OnMessageReceived += OnMessageReceived;
            _networkClient.OnDisconnected += OnDisconnected;
        }

        // Set the local player information
        public async void EditPlayer(string name, double drinkModifier)
        {
            _localPlayer = new PlayerInfo(name, drinkModifier);
            UserData.LocalPlayer = _localPlayer;
            UserData.Save(); // Save UserData
            UIUpdate?.Invoke();
            await SendMessageAsync("PLAYERUPDATE:" + _localPlayer.Send());
        }

        // Empty SetGameRules
        public async Task SetGameRules(string filepath)
        {
            // Clients do not set game rules; handled by the server
            await Task.CompletedTask;
        }

        // Join a game by connecting to a server at the given IP
        public async Task Start(int port, string ipAddress)
        {
            if (_networkClient != null)
            {
                try
                {
                    await _networkClient.ConnectAsync(ipAddress, port, 5); // 5 seconds timeout
                    LogMessage("Joined game successfully.");
                }
                catch (TimeoutException ex)
                {
                    LogMessage($"Connection timed out: {ex.Message}");
                    throw; // Re-throw exception
                }
                catch (Exception ex)
                {
                    LogMessage($"Error connecting to server: {ex.Message}");
                    throw; // Re-throw exception
                }
            }
            else
            {
                LogMessage("Error: Client object is null.");
                throw new Exception("Client object is null.");
            }
        }

        // Send a message to the server
        private async Task SendMessageAsync(string message)
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

            try
            {
                dynamic messageObject = JsonConvert.DeserializeObject(message);
                string messageType = messageObject.MessageType;

                if (messageType == "RULEUPDATE")
                {
                    // Handle rule update
                }
                else if (messageType == "PlayerListUpdate")
                {
                    // Handle player list update
                }
                else if (messageType == "TRIGGERINDIVIDUALRULE")
                {
                    Rule rule = JsonConvert.DeserializeObject<Rule>(messageObject.Rule.ToString());
                    PlayerInfo player = JsonConvert.DeserializeObject<PlayerInfo>(messageObject.Player.ToString());

                    // Invoke the punishment
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        OnIndividualPunishmentTriggered?.Invoke(rule, player);
                    });
                }
                else if (messageType == "TRIGGERGROUPRULE")
                {
                    Rule rule = JsonConvert.DeserializeObject<Rule>(messageObject.Rule.ToString());

                    // Invoke the punishment
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        OnGroupPunishmentTriggered?.Invoke(rule);
                    });
                }
                else
                {
                    LogMessage("Unknown message type received.");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error processing message: {ex.Message}");
            }
        }


        // Handle client disconnection
        private void OnDisconnected()
        {
            LogMessage("Disconnected from server.");

            // Safely invoke disconnection on the main thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                OnDisconnect?.Invoke();
            });
        }

        public void RequestUIUpdate()
        {
            UIUpdate?.Invoke();
        }

        // Log messages to the console or handle through a logger
        private void LogMessage(string message)
        {
            OnLog?.Invoke(message);
        }

        // Disconnect from the server
        public void Stop()
        {
            _networkClient.Disconnect();
            LogMessage("Client disconnected.");
            UserData.Save(); // Save UserData when stopping
        }

        // Implement TriggerGroupRule
        public async void TriggerGroupRule(Rule rule)
        {
            // Serialize the rule
            string serializedRule = JsonConvert.SerializeObject(rule);

            // Send message to the server
            string message = $"TRIGGERGROUPRULE:{serializedRule}";
            await SendMessageAsync(message);
        }

        // Implement TriggerIndividualRule
        public async void TriggerIndividualRule(Rule rule, PlayerInfo player)
        {
            // Serialize the rule and player info
            string serializedRule = JsonConvert.SerializeObject(rule);
            string serializedPlayer = JsonConvert.SerializeObject(player);

            // Send message to the server
            string message = $"TRIGGERINDIVIDUALRULE:{serializedRule}:{serializedPlayer}";
            await SendMessageAsync(message);
        }
    }
}
