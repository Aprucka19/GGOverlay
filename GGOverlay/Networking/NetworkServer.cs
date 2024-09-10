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

        public event Action<TcpClient> OnClientConnected;

        public NetworkServer(DatabaseManager databaseManager)
        {
            _databaseManager = databaseManager;
        }

        public void StartServer()
        {
            _server = new TcpListener(IPAddress.Any, Port);
            _server.Start();
            Task.Run(AcceptClientsAsync);
        }

        private async Task AcceptClientsAsync()
        {
            while (true)
            {
                var client = await _server.AcceptTcpClientAsync();
                _connectedClients.Add(client);
                OnClientConnected?.Invoke(client);
                await SendAllCounterValuesAsync(client);
                Task.Run(() => ReceiveDataAsync(client));
            }
        }

        public void UpdateCounter(string counterName, int newValue)
        {
            _databaseManager.UpdateCounterValue(counterName, newValue);
            BroadcastCounterValue(counterName, newValue);
        }

        private async Task ReceiveDataAsync(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
            {
                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                if (message.StartsWith("COUNTER:"))
                {
                    var parts = message.Substring(8).Split(':');
                    if (parts.Length == 2 && int.TryParse(parts[1], out int newValue))
                    {
                        UpdateCounter(parts[0], newValue);
                    }
                }
            }
            _connectedClients.Remove(client);
            client.Close();
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
                    await client.GetStream().WriteAsync(data, 0, data.Length);
                }
            }
        }
    }
}
