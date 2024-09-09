using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GGOverlay
{
    public partial class MainWindow : Window
    {
        private TcpListener _server;
        private TcpClient _client;
        private const int Port = 5000;

        public MainWindow()
        {
            InitializeComponent();
            IpAddressTextBox.GotFocus += (s, e) => PlaceholderText.Visibility = Visibility.Collapsed;
            IpAddressTextBox.LostFocus += (s, e) =>
            {
                if (string.IsNullOrEmpty(IpAddressTextBox.Text))
                    PlaceholderText.Visibility = Visibility.Visible;
            };
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
                _ = Task.Run(() => ReceiveDataAsync(client));
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
                // Echo back to the client
                await stream.WriteAsync(buffer, 0, bytesRead);
            }

            Log("Client disconnected.");
            client.Close();
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
}
