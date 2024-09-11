// Data/GameData.cs
using GGOverlay.Networking;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace GGOverlay.Data
{
    public class GameData
    {
        public List<Profile> Profiles { get; private set; }
        public Counter Counter { get; private set; }
        private NetworkServer _server;
        private NetworkClient _client;

        public event Action OnDataUpdated; // Event to notify MainWindow when data updates

        public GameData(NetworkServer server = null, NetworkClient client = null)
        {
            Profiles = new List<Profile>();
            Counter = new Counter();
            _server = server;
            _client = client;

            if (_server != null)
            {
                _server.OnMessageReceived += HandleMessageReceived; // Corrected delegate usage
                _server.OnClientConnected += SendCurrentGameDataToClient;
            }

            if (_client != null)
            {
                _client.OnMessageReceived += (message) => HandleMessageReceived(message, null); // Corrected delegate usage
            }
        }

        // Add a profile and propagate changes if necessary
        public async Task AddProfile(Profile profile)
        {
            Profiles.Add(profile);
            OnDataUpdated?.Invoke(); // Notify UI to update
            await SendProfileUpdateAsync(profile); // Propagate the profile update
        }

        // Update the counter and propagate changes
        public async Task UpdateCounter(int newValue)
        {
            Counter.SetValue(newValue);
            OnDataUpdated?.Invoke(); // Notify UI to update
            await SendCounterUpdateAsync(); // Propagate the counter update
        }

        // Send the current GameData state to a new client when they connect
        private async void SendCurrentGameDataToClient(TcpClient client)
        {
            await SendGameDataAsync(client);
        }

        // Send a profile update, either broadcasting as a host or sending to the server as a client
        private async Task SendProfileUpdateAsync(Profile profile, TcpClient excludeClient = null)
        {
            string serializedProfile = $"PROFILE:{SerializeProfile(profile)}";

            if (_server != null) // If this instance is the host
            {
                await BroadcastMessageAsync(serializedProfile, excludeClient);
            }
            else if (_client != null) // If this instance is a client
            {
                await _client.SendMessageAsync(serializedProfile);
            }
        }

        // Send a counter update, either broadcasting as a host or sending to the server as a client
        private async Task SendCounterUpdateAsync(TcpClient excludeClient = null)
        {
            string message = $"COUNTER:{Counter.Value}";

            if (_server != null) // If this instance is the host
            {
                await BroadcastMessageAsync(message, excludeClient);
            }
            else if (_client != null) // If this instance is a client
            {
                await _client.SendMessageAsync(message);
            }
        }

        // Send the entire GameData object
        private async Task SendGameDataAsync(TcpClient excludeClient = null)
        {
            string serializedData = $"GAMEDATA:{Serialize()}";

            if (_server != null) // If this instance is the host
            {
                await BroadcastMessageAsync(serializedData, excludeClient);
            }
            else if (_client != null) // If this instance is a client
            {
                await _client.SendMessageAsync(serializedData);
            }
        }

        // Broadcast a message to all connected clients, excluding one client if specified
        private async Task BroadcastMessageAsync(string message, TcpClient excludeClient = null)
        {
            if (_server == null) return; // No server, nothing to broadcast

            foreach (var client in _server.ConnectedClients)
            {
                if (client != excludeClient && client.Connected) // Skip the excluded client
                {
                    try
                    {
                        byte[] data = System.Text.Encoding.UTF8.GetBytes(message);
                        await client.GetStream().WriteAsync(data, 0, data.Length);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending to client: {ex.Message}");
                    }
                }
            }
        }

        // Handle incoming messages and update GameData accordingly
        private void HandleMessageReceived(string message, TcpClient senderClient = null)
        {
            try
            {
                if (message.StartsWith("GAMEDATA:"))
                {
                    Deserialize(message.Substring(9));
                    OnDataUpdated?.Invoke(); // Notify UI to update
                }
                else if (message.StartsWith("PROFILE:"))
                {
                    var newProfile = DeserializeProfile(message.Substring(8));
                    Profiles.Add(newProfile);
                    OnDataUpdated?.Invoke(); // Notify UI to update
                    if(_server != null)
                        _ = SendProfileUpdateAsync(newProfile, senderClient); // Propagate to other clients if host
                }
                else if (message.StartsWith("COUNTER:") && int.TryParse(message.Substring(8), out int newValue))
                {
                    Counter.SetValue(newValue);
                    OnDataUpdated?.Invoke(); // Notify UI to update
                    if (_server != null)
                        _ = SendCounterUpdateAsync(senderClient); // Propagate to other clients if host
                }
            }
            catch (JsonReaderException ex)
            {
                Console.WriteLine($"JSON deserialization error: {ex.Message}");
            }
        }

        // Serialize the GameData object
        private string Serialize()
        {
            try
            {
                return JsonConvert.SerializeObject(this);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error serializing GameData: {ex.Message}");
                return string.Empty;
            }
        }

        // Deserialize the GameData object from a string
        private void Deserialize(string data)
        {
            try
            {
                var deserializedData = JsonConvert.DeserializeObject<GameData>(data);
                Profiles = deserializedData.Profiles ?? new List<Profile>(); // Fallback to avoid null
                Counter = deserializedData.Counter ?? new Counter(); // Fallback to avoid null
            }
            catch (JsonReaderException ex)
            {
                Console.WriteLine($"Error deserializing GameData: {ex.Message}");
            }
        }

        // Serialize a profile
        private string SerializeProfile(Profile profile)
        {
            try
            {
                return JsonConvert.SerializeObject(profile);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error serializing profile: {ex.Message}");
                return string.Empty;
            }
        }

        // Deserialize a profile
        private Profile DeserializeProfile(string data)
        {
            try
            {
                return JsonConvert.DeserializeObject<Profile>(data);
            }
            catch (JsonReaderException ex)
            {
                Console.WriteLine($"Error deserializing profile: {ex.Message}");
                return new Profile(); // Return an empty profile on error
            }
        }
    }
}
