using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using GGOverlay.Database;
using Newtonsoft.Json;

namespace GGOverlay.Networking
{
    public class NetworkServer
    {
        private TcpListener _server;
        private List<TcpClient> _connectedClients = new List<TcpClient>();
        private const int Port = 25565;
        private DatabaseManager _databaseManager;

        public event Action<string> OnLogMessage;

        public NetworkServer(DatabaseManager databaseManager)
        {
            _databaseManager = databaseManager;
            _databaseManager.OnDatabaseChanged += HandleDatabaseChange;  // Subscribe to changes
        }

        public void StartServer()
        {
            try
            {
                _server = new TcpListener(IPAddress.Any, Port);
                _server.Start();
                Log("Server started on port 25565.");
                Task.Run(AcceptClientsAsync);
            }
            catch (Exception ex)
            {
                Log($"Error starting server: {ex.Message}");
            }
        }

        private async Task AcceptClientsAsync()
        {
            while (true)
            {
                try
                {
                    var client = await _server.AcceptTcpClientAsync();
                    Log($"Client connected from {client.Client.RemoteEndPoint}");
                    _connectedClients.Add(client);
                    await SendInitialDatabaseStateAsync(client);  // Send initial state of the database
                    Task.Run(() => ReceiveDataAsync(client));
                }
                catch (Exception ex)
                {
                    Log($"Error accepting client: {ex.Message}");
                }
            }
        }

        private async Task ReceiveDataAsync(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[4096];
            int bytesRead;

            try
            {
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Log($"Received from client: {message}");

                    try
                    {
                        // Deserialize the received message
                        var change = JsonConvert.DeserializeObject<Dictionary<string, object>>(message);

                        // Extract the query and parameters correctly using JObject
                        string query = change["Query"].ToString();

                        // Convert 'Parameters' from JObject to Dictionary<string, object>
                        var parameters = ((Newtonsoft.Json.Linq.JObject)change["Parameters"]).ToObject<Dictionary<string, object>>();

                        // Execute the query with the extracted parameters
                        _databaseManager.ExecuteNonQuery(query, parameters, suppressBroadcast: true);

                        // Optionally, broadcast to other clients if needed
                        BroadcastChangeToOthers(change, client);
                    }
                    catch (Exception ex)
                    {
                        Log($"Error processing data from client: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error receiving data from client: {ex.Message}");
            }
            finally
            {
                _connectedClients.Remove(client);
                client.Close();
                Log("Client disconnected.");
            }
        }


        private async void BroadcastChangeToOthers(Dictionary<string, object> change, TcpClient sender)
        {
            byte[] data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(change));

            foreach (TcpClient client in _connectedClients)
            {
                if (client != sender && client.Connected)
                {
                    try
                    {
                        await client.GetStream().WriteAsync(data, 0, data.Length);
                        Log("Broadcasted change to other clients.");
                    }
                    catch (Exception ex)
                    {
                        Log($"Error sending to client: {ex.Message}");
                    }
                }
            }
        }


        private async Task SendInitialDatabaseStateAsync(TcpClient client)
        {
            try
            {
                // Retrieve the entire database state
                var currentState = _databaseManager.GetAllData();

                // Serialize the current state
                string serializedState = JsonConvert.SerializeObject(currentState);

                // Send the serialized state to the client
                byte[] data = Encoding.UTF8.GetBytes(serializedState);
                await client.GetStream().WriteAsync(data, 0, data.Length);
                Log("Sent initial database state to client.");
            }
            catch (Exception ex)
            {
                Log($"Error sending initial database state: {ex.Message}");
            }
        }


        private async void BroadcastChange(string changeMessage)
        {
            byte[] data = Encoding.UTF8.GetBytes(changeMessage);

            foreach (TcpClient client in _connectedClients)
            {
                if (client.Connected)
                {
                    try
                    {
                        await client.GetStream().WriteAsync(data, 0, data.Length);
                        Log("Broadcasted change to all clients.");
                    }
                    catch (Exception ex)
                    {
                        Log($"Error sending to client: {ex.Message}");
                    }
                }
            }
        }

        // Broadcasts any change in the database to all connected clients
        private void HandleDatabaseChange(string changeMessage)
        {
            BroadcastChange(changeMessage);
        }

        private void Log(string message)
        {
            OnLogMessage?.Invoke(message);
        }
    }
}
