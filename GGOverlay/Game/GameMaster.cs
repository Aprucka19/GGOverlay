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
using System.Timers;

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

        // Timer-related fields
        private System.Timers.Timer _timer;
        public double _elapsedMinutes { get; set; }

        public event Action<string> OnLog;
        public event Action UIUpdate;
        public event Action<Rule, PlayerInfo> OnPunishmentTriggered;

        private int _paceHitCount = 0;


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

            // Initialize timer
            _timer = new System.Timers.Timer(60000); // Timer ticks every 60,000 milliseconds (1 minute)
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;
            _elapsedMinutes = 0;

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

        // Method to start the timer
        public void StartTimer()
        {
            if (!_timer.Enabled)
            {
                _elapsedMinutes = 0; // Reset elapsed time
                _timer.Start();
                LogMessage("Timer started.");
            }
        }

        // Method to stop the timer
        public void StopTimer()
        {
            if (_timer.Enabled)
            {
                _timer.Stop();
                _elapsedMinutes = 0;
                LogMessage("Timer stopped.");
            }
        }

        private async void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // Check if _gameRules is not null
            if (_gameRules == null)
            {
                // Log message or handle the case where _gameRules has not been assigned yet
                LogMessage("Game rules have not been assigned.");
                return;
            }

            _elapsedMinutes += 1;

            // Send the updated _elapsedMinutes to all clients
            await SendElapsedMinutesUpdateAsync();

            if (_gameRules.Pace > 0 && _elapsedMinutes % _gameRules.Pace == 0)
            {
                _paceHitCount++;
                // Trigger the function (placeholder)
                OnPaceReached();
            }
        }

        // New method to send _elapsedMinutes to clients
        private async Task SendElapsedMinutesUpdateAsync()
        {
            try
            {
                var messageObject = new ElapsedMinutesUpdateMessage { ElapsedMinutes = _elapsedMinutes };
                string serializedMessage = JsonConvert.SerializeObject(messageObject);

                await _networkServer.BroadcastMessageAsync(serializedMessage);
                LogMessage($"Sent elapsed minutes update: {_elapsedMinutes} minutes.");
            }
            catch (Exception ex)
            {
                LogMessage($"Error sending elapsed minutes update: {ex.Message}");
            }
        }


        private async Task OnPaceReached()
        {
            // Loop through each player
            foreach (var player in _players)
            {
                // Calculate the expected drink count
                double expectedDrinkCount = _gameRules.PaceQuantity * player.DrinkModifier * _paceHitCount;

                // If player's drink count is less than expected
                if (player.DrinkCount < expectedDrinkCount)
                {
                    // Calculate the difference
                    int difference = (int)Math.Ceiling((expectedDrinkCount - player.DrinkCount) / player.DrinkModifier);

                    // Create a custom rule
                    Rule customRule = new Rule
                    {
                        RuleDescription = "Pace Punishment",
                        PunishmentDescription = "{0} needs to keep pace and drink {1}.",
                        PunishmentQuantity = difference,
                        PunishmentType = PunishmentType.Individual

                    };

                    // Log the punishment
                    LogMessage($"Pace Punishment for {player.Name}: Needs to drink {difference} to keep pace.");

                    // Handle the individual punishment
                    await HandleTriggerIndividualRule(customRule, player);
                }
            }
        }


        public async Task SetGameRules(string filepath)
        {
            StopTimer();
            _gameRules.LoadFromFile(filepath);
            UIUpdate?.Invoke();
            await BroadcastMessageAsync("RULEUPDATE:" + _gameRules.Send());
        }

        // In GameMaster.cs
        public async void BroadcastGameRules()
        {
            if (_gameRules != null)
            {
                var messageObject = new RuleUpdateMessage { Rules = _gameRules.Rules };
                string serializedMessage = JsonConvert.SerializeObject(messageObject);

                await BroadcastMessageAsync(serializedMessage);
            }
        }


        // Set the local player information
        public async void EditPlayer(string name, double drinkModifier, int drinkCount = 0)
        {
            _localPlayer = new PlayerInfo(name, drinkModifier);
            _localPlayer.DrinkCount = drinkCount;
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
                var messageObject = new RuleUpdateMessage { Rules = _gameRules.Rules };
                string serializedMessage = JsonConvert.SerializeObject(messageObject);

                await _networkServer.SendMessageToClientAsync(serializedMessage, client);
            }
            if (_clientPlayerMap.Count > 0 || _localPlayer != null)
            {
                await SendPlayerListUpdateAsync();
            }
            await SendElapsedMinutesUpdateAsync();
        }


        private async void OnServerMessageReceived(string message, TcpClient client)
        {
            LogMessage($"Received from client {client.Client.RemoteEndPoint}: {message}");

            try
            {
                // Deserialize the message to a dynamic object
                dynamic messageObject = JsonConvert.DeserializeObject(message);

                string messageType = messageObject.MessageType;

                if (messageType == "TRIGGERINDIVIDUALRULE")
                {
                    Rule rule = JsonConvert.DeserializeObject<Rule>(messageObject.Rule.ToString());
                    PlayerInfo player = JsonConvert.DeserializeObject<PlayerInfo>(messageObject.Player.ToString());

                    await HandleTriggerIndividualRule(rule, player);
                }
                if (messageType == "TRIGGERALLBUTONERULE")
                {
                    Rule rule = JsonConvert.DeserializeObject<Rule>(messageObject.Rule.ToString());
                    PlayerInfo player = JsonConvert.DeserializeObject<PlayerInfo>(messageObject.Player.ToString());

                    await HandleTriggerAllButOneRule(rule, player);
                }
                else if (messageType == "TRIGGERGROUPRULE")
                {
                    Rule rule = JsonConvert.DeserializeObject<Rule>(messageObject.Rule.ToString());

                    await HandleTriggerGroupRule(rule);
                }
                else if (messageType == "PLAYERUPDATE")
                {
                    // Deserialize the player info
                    PlayerInfo updatedPlayer = JsonConvert.DeserializeObject<PlayerInfo>(messageObject.Player.ToString());

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
                else if (messageType == "TRIGGEREVENTPACERULE") // New handling for EventPace
                {
                    Rule rule = JsonConvert.DeserializeObject<Rule>(messageObject.Rule.ToString());

                    await HandleTriggerEventPaceRule(rule);
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

                // Create a PlayerListUpdateMessage
                var messageObject = new PlayerListUpdateMessage { Players = playerList };
                string serializedMessage = JsonConvert.SerializeObject(messageObject);

                // Send the PlayerListUpdate message to all clients
                await _networkServer.BroadcastMessageAsync(serializedMessage);
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
            StopTimer();
        }

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
                    // Ignore the trigger due to cooldown
                    LogMessage($"Ignored trigger of rule '{rule.RuleDescription}' for player '{player.Name}' due to cooldown.");
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

            // Update UI and invoke events on the UI thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                UIUpdate?.Invoke();
                OnPunishmentTriggered?.Invoke(rule, player);
            });

            // Send out a message to all clients to trigger the rule
            var messageObject = new TriggerIndividualRuleMessage { Rule = rule, Player = player };
            string triggerMessage = JsonConvert.SerializeObject(messageObject);

            await BroadcastMessageAsync(triggerMessage);
        }


        private async Task HandleTriggerAllButOneRule(Rule rule, PlayerInfo player)
        {
            // Generate a unique key for the rule (we no longer need the player key for AllButOne)
            string ruleKey = $"ALLBUTONE:{GetRuleKey(rule)}";

            DateTime now = DateTime.Now;

            if (_lastRuleTriggerTime.ContainsKey(ruleKey))
            {
                DateTime lastTriggerTime = _lastRuleTriggerTime[ruleKey];
                if ((now - lastTriggerTime).TotalSeconds < 10)
                {
                    // Ignore the trigger due to cooldown
                    LogMessage($"Ignored trigger of rule '{rule.RuleDescription}' due to cooldown.");
                    return;
                }
            }

            // Update the last trigger time for this rule
            _lastRuleTriggerTime[ruleKey] = now;

            // Loop through all players except the input player
            foreach (var targetPlayer in _players)
            {
                if (targetPlayer.Name != player.Name) // Skip the input player
                {
                    // Calculate adjusted punishment quantity
                    int adjustedPunishmentQuantity = (int)Math.Round(rule.PunishmentQuantity * targetPlayer.DrinkModifier, MidpointRounding.AwayFromZero);

                    // Add the adjusted punishment quantity to the player's drink count
                    targetPlayer.DrinkCount += adjustedPunishmentQuantity;
                }
            }

            // Send out a Player Update (this will update all clients with the modified player list)
            await SendPlayerListUpdateAsync();

            // Update UI and invoke events on the UI thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                UIUpdate?.Invoke();
                OnPunishmentTriggered?.Invoke(rule, player); // Raise event for UI
            });

            // Send out a message to all clients to trigger the AllButOne rule
            var messageObject = new TriggerAllButOneRuleMessage { Rule = rule, Player = player };
            string triggerMessage = JsonConvert.SerializeObject(messageObject);

            // Broadcast the trigger message to all clients
            await BroadcastMessageAsync(triggerMessage);
        }


        private async Task HandleTriggerGroupRule(Rule rule)
        {
            string ruleKey = $"GROUP:{GetRuleKey(rule)}";

            DateTime now = DateTime.Now;

            if (_lastRuleTriggerTime.ContainsKey(ruleKey))
            {
                DateTime lastTriggerTime = _lastRuleTriggerTime[ruleKey];
                if ((now - lastTriggerTime).TotalSeconds < 10)
                {
                    // Ignore the trigger due to cooldown
                    LogMessage($"Ignored trigger of group rule '{rule.RuleDescription}' due to cooldown.");
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

            // Update UI and invoke events on the UI thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                UIUpdate?.Invoke();
                OnPunishmentTriggered?.Invoke(rule,null);
            });

            // Send out a message to all clients to trigger the rule
            var messageObject = new TriggerGroupRuleMessage { Rule = rule };
            string triggerMessage = JsonConvert.SerializeObject(messageObject);

            await BroadcastMessageAsync(triggerMessage);
        }


        private string GetRuleKey(Rule rule)
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new IgnorePunishmentCounterContractResolver(),
                Formatting = Formatting.None
            };

            string serializedRule = JsonConvert.SerializeObject(rule, settings);
            return ComputeHash(serializedRule);
        }


        private string GetPlayerKey(PlayerInfo player)
        {
            // Create an anonymous object with only the necessary properties
            var keyObject = new
            {
                player.Name,
                player.DrinkModifier
            };

            // Serialize the anonymous object to JSON
            string serializedPlayer = JsonConvert.SerializeObject(keyObject);

            // Compute and return the hash
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

        public void TriggerRule(Rule rule, PlayerInfo player = null)
        {
            switch (rule.PunishmentType)
            {
                case PunishmentType.Group:
                    _ = HandleTriggerGroupRule(rule);
                    break;

                case PunishmentType.Individual:
                    if (player == null)
                    {
                        throw new ArgumentNullException(nameof(player), "Player must be provided for Individual punishment.");
                    }
                    _ = HandleTriggerIndividualRule(rule, player);
                    break;

                case PunishmentType.AllButOne:
                    if (player == null)
                    {
                        throw new ArgumentNullException(nameof(player), "Exempted player must be provided for AllButOne punishment.");
                    }
                    _ = HandleTriggerAllButOneRule(rule, player);
                    break;

                case PunishmentType.EventPace:
                    _ = HandleTriggerEventPaceRule(rule); // Added this case
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported PunishmentType: {rule.PunishmentType}");
            }
        }

        private async Task HandleTriggerEventPaceRule(Rule ruleFromClient)
        {
            // Find the matching rule in the GameMaster's _gameRules.Rules list
            Rule matchingRule = _gameRules.Rules.FirstOrDefault(r =>
                r.RuleDescription == ruleFromClient.RuleDescription &&
                r.PunishmentType == ruleFromClient.PunishmentType &&
                r.PunishmentQuantity == ruleFromClient.PunishmentQuantity &&
                r.PunishmentDescription == ruleFromClient.PunishmentDescription);

            if (matchingRule == null)
            {
                LogMessage($"No matching rule found for EventPace rule '{ruleFromClient.RuleDescription}'.");
                return;
            }

            // Use the matching rule's _punishmentCounter
            // Generate a unique key for the rule
            string ruleKey = $"EVENTPACE:{GetRuleKey(matchingRule)}";

            DateTime now = DateTime.Now;

            if (_lastRuleTriggerTime.ContainsKey(ruleKey))
            {
                DateTime lastTriggerTime = _lastRuleTriggerTime[ruleKey];
                if ((now - lastTriggerTime).TotalSeconds < 10)
                {
                    // Ignore the trigger due to cooldown
                    LogMessage($"Ignored trigger of EventPace rule '{matchingRule.RuleDescription}' due to cooldown.");
                    return;
                }
            }

            // Update the last trigger time
            _lastRuleTriggerTime[ruleKey] = now;

            // Increment the punishment counter in the matching rule
            matchingRule._punishmentCounter++;

            // Loop through each player
            foreach (var player in _players)
            {
                // Calculate the expected drink count
                double expectedDrinkCount = matchingRule._punishmentCounter * matchingRule.PunishmentQuantity * player.DrinkModifier;

                // If player's drink count is less than expected
                if (player.DrinkCount < expectedDrinkCount)
                {
                    // Calculate the difference
                    int difference = (int)Math.Ceiling((expectedDrinkCount - player.DrinkCount) / player.DrinkModifier);

                    // Create a custom rule for the punishment
                    Rule customRule = new Rule
                    {
                        RuleDescription = matchingRule.RuleDescription, // Use the original rule description
                        PunishmentDescription = matchingRule.PunishmentDescription, // Use the original punishment description
                        PunishmentQuantity = difference,
                        PunishmentType = PunishmentType.Individual
                    };

                    // Log the punishment
                    LogMessage($"EventPace Punishment for {player.Name}: Needs to drink {difference} to keep pace.");

                    // Handle the individual punishment
                    await HandleTriggerIndividualRule(customRule, player);
                }
            }
        }




        public void FinishDrink()
        {
            // Calculate the punishment quantity
            int desiredSips = 20 - (_localPlayer.DrinkCount % 20);

            double unroundedPunishmentQuantity = desiredSips / _localPlayer.DrinkModifier;
            int punishmentQuantity = (int)Math.Round(unroundedPunishmentQuantity, MidpointRounding.AwayFromZero);

            // Create the new individual rule
            Rule finishDrinkRule = new Rule
            {
                RuleDescription = "Finish Drink",
                PunishmentDescription = "{0} drank {1} to finish their drink.",
                PunishmentQuantity = punishmentQuantity,
                PunishmentType = PunishmentType.Individual
            };

            // Handle the trigger directly
            _ = HandleTriggerIndividualRule(finishDrinkRule, _localPlayer);
        }
    }
}
