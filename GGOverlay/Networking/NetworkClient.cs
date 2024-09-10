using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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

        public async Task SendCounterUpdateAsync(string counterName, int newValue)
        {
            if (_client != null && _client.Connected)
            {
                try
                {
                    string message = $"COUNTER:{counterName}:{newValue}";
                    byte[] data = Encoding.UTF8.GetBytes(message);
                    await _client.GetStream().WriteAsync(data, 0, data.Length);
                    Log($"Sent {counterName} update {newValue} to server.");
                }
                catch (Exception ex)
                {
                    Log($"Error sending counter update to server: {ex.Message}");
                }
            }
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

                    // Handle counter updates
                    if (message.StartsWith("COUNTER:"))
                    {
                        var parts = message.Substring(8).Split(':');
                        if (parts.Length == 2 && int.TryParse(parts[1], out int newValue))
                        {
                            string counterName = parts[0];
                            UpdateCounterUI(counterName, newValue);
                        }
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

        // Method to update the UI with the new counter value
        private void UpdateCounterUI(string counterName, int newValue)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                switch (counterName)
                {
                    case "Counter1":
                        ((MainWindow)Application.Current.MainWindow).Counter1TextBox.Text = newValue.ToString();
                        break;
                    case "Counter2":
                        ((MainWindow)Application.Current.MainWindow).Counter2TextBox.Text = newValue.ToString();
                        break;
                    case "Counter3":
                        ((MainWindow)Application.Current.MainWindow).Counter3TextBox.Text = newValue.ToString();
                        break;
                }
            });
        }


        private void Log(string message)
        {
            OnDataReceived?.Invoke(message);
        }

    }
}
