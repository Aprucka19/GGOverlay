// Networking/NetworkClient.cs
using GGOverlay.Utilities; // Add this using statement
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GGOverlay.Networking
{
    public class NetworkClient
    {
        private TcpClient _client;
        private const int Port = 5000;

        public event Action<string> OnMessageReceived;

        public async Task ConnectAsync(string ipAddress)
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(ipAddress, Port);
                Logger.Log("Connected to the server.");
                await Task.Run(() => ReceiveDataAsync());
            }
            catch (Exception ex)
            {
                Logger.Log($"Error connecting to server: {ex.Message}");
            }
        }

        private async Task ReceiveDataAsync()
        {
            NetworkStream stream = _client.GetStream();
            byte[] buffer = new byte[1000024];
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
            {
                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                OnMessageReceived?.Invoke(message);
            }

            Logger.Log("Disconnected from server.");
        }

        public async Task SendMessageAsync(string message)
        {
            if (_client != null && _client.Connected)
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                await _client.GetStream().WriteAsync(data, 0, data.Length);
            }
        }
    }
}
