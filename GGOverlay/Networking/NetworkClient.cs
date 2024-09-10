using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using GGOverlay.Database;

namespace GGOverlay.Networking
{
    public class NetworkClient
    {
        private TcpClient _client;
        private const int Port = 25565;
        private DatabaseManager _databaseManager;
        public event Action<string> OnDataReceived;

        public NetworkClient(DatabaseManager databaseManager)
        {
            _databaseManager = databaseManager;
            _databaseManager.OnDatabaseChanged += SendDatabaseChange; // Send changes to server
        }

        public void ConnectToServer(string ipAddress)
        {
            _client = new TcpClient();
            _client.ConnectAsync(ipAddress, Port).ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    Log("Successfully connected to the server.");
                    Task.Run(() => ReceiveDataAsync());
                }
                else
                {
                    Log($"Error connecting to server: {task.Exception?.Message}");

                }
            });
        }

        private async void SendDatabaseChange(string changeMessage)
        {
            if (_client != null && _client.Connected)
            {
                try
                {
                    byte[] data = Encoding.UTF8.GetBytes(changeMessage);
                    await _client.GetStream().WriteAsync(data, 0, data.Length);
                    Log("Sent database change to server.");
                }
                catch (Exception ex)
                {
                    Log($"Error sending change to server: {ex.Message}");
                }
            }
        }

        private async Task ReceiveDataAsync()
        {
            NetworkStream stream = _client.GetStream();
            byte[] buffer = new byte[4096];
            int bytesRead;

            try
            {
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Log($"Received from server: {message}");

                    // Try to deserialize as a full database state first
                    try
                    {
                        var fullState = JsonConvert.DeserializeObject<Dictionary<string, List<Dictionary<string, object>>>>(message);

                        // If deserialization is successful, handle full state update
                        if (fullState != null)
                        {
                            HandleFullStateUpdate(fullState);
                            continue; // Skip processing as a change message
                        }
                    }
                    catch (Exception) { /* Ignore and try as change message */ }

                    // Deserialize and apply changes as individual updates
                    try
                    {
                        var change = JsonConvert.DeserializeObject<Dictionary<string, object>>(message);

                        // Extract the query and parameters correctly using JObject
                        string query = change["Query"].ToString();

                        // Convert 'Parameters' from JObject to Dictionary<string, object>
                        var parameters = ((Newtonsoft.Json.Linq.JObject)change["Parameters"]).ToObject<Dictionary<string, object>>();

                        // Execute the query with the extracted parameters, suppressing broadcasts to avoid loops
                        _databaseManager.ExecuteNonQuery(query, parameters, suppressBroadcast: true);
                    }
                    catch (Exception ex)
                    {
                        Log($"Error processing data: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error receiving data from server: {ex.Message}");
            }
            finally
            {
                _client.Close();
                Log("Disconnected from server.");
            }
        }


        // Handle the full state update from the server
        private void HandleFullStateUpdate(Dictionary<string, List<Dictionary<string, object>>> fullState)
        {
            // Clear current state and replace with the new state
            foreach (var tableName in fullState.Keys)
            {
                // Clear the table
                _databaseManager.ExecuteNonQuery($"DELETE FROM {tableName};", new Dictionary<string, object>(), suppressBroadcast: true);

                // Insert each row into the table
                foreach (var row in fullState[tableName])
                {
                    // Build insert query and parameters
                    var insertQuery = BuildInsertQuery(tableName, row, out var parameters);
                    _databaseManager.ExecuteNonQuery(insertQuery, parameters, suppressBroadcast: true);
                }
            }

            Log("Full database state updated successfully.");
        }


        // Utility method to build an insert query from a row of data
        private string BuildInsertQuery(string tableName, Dictionary<string, object> row, out Dictionary<string, object> parameters)
        {
            var columns = string.Join(", ", row.Keys);
            var placeholders = string.Join(", ", row.Keys.Select(k => "@" + k)); // Create placeholders like @Id, @Name
            parameters = row.ToDictionary(k => "@" + k.Key, v => v.Value); // Create parameters with keys like @Id, @Name

            return $"INSERT INTO {tableName} ({columns}) VALUES ({placeholders});";
        }


        private void Log(string message)
        {
            OnDataReceived?.Invoke(message);
        }
    }
}
