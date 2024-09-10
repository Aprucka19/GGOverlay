using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using GGOverlay.Database;

namespace GGOverlay.Networking
{
    public class NetworkServer
    {
        private TcpListener _server;
        private List<TcpClient> _connectedClients = new List<TcpClient>();
        private const int Port = 25565;
        private DatabaseManager _databaseManager;

        // Delegate to handle log messages
        public event Action<string> OnLogMessage;
        public event Action<TcpClient> OnClientConnected;

        public NetworkServer(DatabaseManager databaseManager)
        {
            _databaseManager = databaseManager;
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
                    OnClientConnected?.Invoke(client);
                    await SendAllCounterValuesAsync(client);
                    Task.Run(() => ReceiveDataAsync(client));
                }
                catch (Exception ex)
                {
                    Log($"Error accepting client: {ex.Message}");
                }
            }
        }

        public void UpdateCounter(string counterName, int newValue)
        {
            try
            {
                _databaseManager.UpdateCounterValue(counterName, newValue);
                Log($"Updated {counterName} to {newValue}");
                BroadcastCounterValue(counterName, newValue);
            }
            catch (Exception ex)
            {
                Log($"Error updating counter {counterName}: {ex.Message}");
            }
        }

        private async Task ReceiveDataAsync(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead;

            try
            {
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Log($"Received from client: {message}");
                    if (message.StartsWith("COUNTER:"))
                    {
                        var parts = message.Substring(8).Split(':');
                        if (parts.Length == 2 && int.TryParse(parts[1], out int newValue))
                        {
                            UpdateCounter(parts[0], newValue);
                        }
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

        private async Task SendAllCounterValuesAsync(TcpClient client)
        {
            var counters = _databaseManager.GetAllCounterValues();
            foreach (var counter in counters)
            {
                await SendCounterValueAsync(client, counter.Key, counter.Value);
            }
        }

        private async Task SendCounterValueAsync(TcpClient client, string counterName, int value)
        {
            if (client != null && client.Connected)
            {
                string message = $"COUNTER:{counterName}:{value}";
                byte[] data = Encoding.UTF8.GetBytes(message);
                await client.GetStream().WriteAsync(data, 0, data.Length);
                Log($"Sent {counterName} value {value} to client.");
            }
        }

        private async void BroadcastCounterValue(string counterName, int value)
        {
            string message = $"COUNTER:{counterName}:{value}";
            byte[] data = Encoding.UTF8.GetBytes(message);

            foreach (TcpClient client in _connectedClients)
            {
                if (client.Connected)
                {
                    try
                    {
                        await client.GetStream().WriteAsync(data, 0, data.Length);
                        Log($"Broadcasted {counterName} value {value} to all clients.");
                    }
                    catch (Exception ex)
                    {
                        Log($"Error sending to client: {ex.Message}");
                    }
                }
            }
        }

        // Method to log messages using the delegate
        private void Log(string message)
        {
            OnLogMessage?.Invoke(message);
        }
    }
}
