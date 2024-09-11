// Networking/NetworkServer.cs
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GGOverlay.Networking
{
    public class NetworkServer
    {
        private TcpListener _server;
        private List<TcpClient> _connectedClients = new List<TcpClient>();
        private const int Port = 5000;

        public event Action<string> OnLog;
        public event Action<string> OnMessageReceived;

        public async Task StartAsync()
        {
            try
            {
                _server = new TcpListener(IPAddress.Any, Port);
                _server.Start();
                OnLog?.Invoke("Server started... Waiting for clients.");
                await Task.Run(() => AcceptClientsAsync());
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Error starting server: {ex.Message}");
            }
        }

        private async Task AcceptClientsAsync()
        {
            while (true)
            {
                var client = await _server.AcceptTcpClientAsync();
                OnLog?.Invoke("Client connected.");
                _connectedClients.Add(client);
                _ = Task.Run(() => ReceiveDataAsync(client));
            }
        }

        private async Task ReceiveDataAsync(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
            {
                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                OnMessageReceived?.Invoke(message);
            }

            OnLog?.Invoke("Client disconnected.");
            _connectedClients.Remove(client);
            client.Close();
        }

        public async Task BroadcastMessageAsync(string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            foreach (TcpClient client in _connectedClients)
            {
                if (client.Connected)
                {
                    try
                    {
                        await client.GetStream().WriteAsync(data, 0, data.Length);
                    }
                    catch (Exception ex)
                    {
                        OnLog?.Invoke($"Error sending to client: {ex.Message}");
                    }
                }
            }
        }
    }
}
