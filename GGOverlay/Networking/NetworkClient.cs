using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GGOverlay.Networking
{
    public class NetworkClient
    {
        private TcpClient _client;
        private const int Port = 25565;
        public event Action<string> OnDataReceived;

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

        private async Task ReceiveDataAsync()
        {
            NetworkStream stream = _client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead;

            try
            {
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Log($"Received from server: {message}");
                    OnDataReceived?.Invoke(message);
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

        private void Log(string message)
        {
            OnDataReceived?.Invoke(message);
        }

    }
}
