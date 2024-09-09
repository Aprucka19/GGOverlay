using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace GGOverlay
{
    public partial class MainWindow : Window
    {
        private TcpListener _server;
        private TcpClient _client;
        private List<TcpClient> _connectedClients = new List<TcpClient>(); // Track connected clients
        private const int Port = 5000;
        private int _counterValue = 0;

        public MainWindow()
        {
            InitializeComponent();
            CounterTextBox.Text = _counterValue.ToString();
        }

        // Method to start the server
        private async void HostButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _server = new TcpListener(IPAddress.Any, Port);
                _server.Start();
                Log("Server started... Waiting for clients.");
                await Task.Run(() => AcceptClientsAsync());
            }
            catch (Exception ex)
            {
                Log($"Error starting server: {ex.Message}");
            }
        }

        // Method to connect to the server as a client
        private async void JoinButton_Click(object sender, RoutedEventArgs e)
        {
            string ipAddress = IpAddressTextBox.Text.Trim();
            if (string.IsNullOrEmpty(ipAddress))
            {
                Log("Please enter a valid IP address.");
                return;
            }

            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(ipAddress, Port);
                Log("Connected to the server.");
                await Task.Run(() => ReceiveDataAsync(_client));
            }
            catch (Exception ex)
            {
                Log($"Error connecting to server: {ex.Message}");
            }
        }

        // Accept incoming clients
        private async Task AcceptClientsAsync()
        {
            while (true)
            {
                var client = await _server.AcceptTcpClientAsync();
                Log("Client connected.");
                _connectedClients.Add(client); // Add client to list
                _ = Task.Run(() => ReceiveDataAsync(client));

                // Send initial counter value to new client
                await SendCounterValueAsync(client, _counterValue);
            }
        }

        // Receive data from a connected client
        private async Task ReceiveDataAsync(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
            {
                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Log($"Received: {message}");

                if (message.StartsWith("COUNTER:"))
                {
                    if (int.TryParse(message.Substring(8), out int newValue))
                    {
                        UpdateCounter(newValue);
                    }
                }
            }

            Log("Client disconnected.");
            _connectedClients.Remove(client); // Remove client on disconnect
            client.Close();
        }

        // Update the counter value and display it
        private void UpdateCounter(int newValue)
        {
            _counterValue = newValue;
            Application.Current.Dispatcher.Invoke(() =>
            {
                CounterTextBox.Text = _counterValue.ToString();
            });

            // Broadcast the updated counter value to all connected clients
            BroadcastCounterValue(_counterValue);
        }

        // Method for the Plus button click
        private void PlusButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateCounter(_counterValue + 1);
        }

        // Method for the Minus button click
        private void MinusButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateCounter(_counterValue - 1);
        }

        // Send the counter value to a specific client
        private async Task SendCounterValueAsync(TcpClient client, int value)
        {
            if (client != null && client.Connected)
            {
                string message = $"COUNTER:{value}";
                byte[] data = Encoding.UTF8.GetBytes(message);
                await client.GetStream().WriteAsync(data, 0, data.Length);
            }
        }

        // Broadcast the counter value to all connected clients
        private async void BroadcastCounterValue(int value)
        {
            string message = $"COUNTER:{value}";
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
                        Log($"Error sending to client: {ex.Message}");
                    }
                }
            }
        }

        // Log messages to the TextBox
        private void Log(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                LogTextBox.AppendText($"{message}\n");
                LogTextBox.ScrollToEnd();
            });
        }
    }


    public class EmptyStringToVisibilityConverter : IValueConverter
    {
        // Converts empty or null strings to Visibility.Collapsed, otherwise Visible
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && string.IsNullOrWhiteSpace(str))
            {
                return Visibility.Visible; // Show placeholder when the string is empty
            }
            return Visibility.Collapsed; // Hide placeholder when there is text
        }

        // Not needed but must be implemented
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
