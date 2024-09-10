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
                    OnDataReceived?.Invoke("Error connecting to server.");
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
            byte[] buffer = new byte[4096]; // Increase buffer size to accommodate larger data
            int bytesRead;

            try
            {
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Log($"Received from server: {message}");
                    OnDataReceived?.Invoke(message);

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
                        _databaseManager.ExecuteNonQuery(change["Query"].ToString(), (Dictionary<string, object>)change["Parameters"]);
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
                // Example logic for clearing and repopulating the local database
                _databaseManager.ExecuteNonQuery($"DELETE FROM {tableName};", new Dictionary<string, object>());

                foreach (var row in fullState[tableName])
                {
                    var insertQuery = BuildInsertQuery(tableName, row);
                    _databaseManager.ExecuteNonQuery(insertQuery, row);
                }
            }

            Log("Full database state updated successfully.");
        }

        // Utility method to build an insert query from a row of data
        private string BuildInsertQuery(string tableName, Dictionary<string, object> row)
        {
            var columns = string.Join(", ", row.Keys);
            var parameters = string.Join(", ", row.Keys);
            return $"INSERT INTO {tableName} ({columns}) VALUES ({parameters});";
        }

        private void Log(string message)
        {
            OnDataReceived?.Invoke(message);
        }
    }
}
