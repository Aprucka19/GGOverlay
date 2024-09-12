using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Networking;

namespace GGOverlay
{
    public partial class MainWindow : Window
    {
        private NetworkServer _server;
        private NetworkClient _client;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void HostButton_Click(object sender, RoutedEventArgs e)
        {
            _server = new NetworkServer();
            _server.OnLog += LogMessage;
            _server.OnMessageReceived += OnServerMessageReceived;
            _server.OnClientConnected += OnClientConnected;

            HostButton.IsEnabled = false;
            JoinButton.IsEnabled = false;
            DisconnectButton.IsEnabled = true;
            SendButton.IsEnabled = true;

            string ipAddress = IpTextBox.Text;
            await _server.StartAsync(ipAddress, 9000); // Arbitrary port, can be adjusted
        }

        private async void JoinButton_Click(object sender, RoutedEventArgs e)
        {
            _client = new NetworkClient();
            _client.OnLog += LogMessage;
            _client.OnMessageReceived += OnClientMessageReceived;
            _client.OnDisconnected += OnClientDisconnected; // Handle client disconnection

            HostButton.IsEnabled = false;
            JoinButton.IsEnabled = false;
            DisconnectButton.IsEnabled = true;
            SendButton.IsEnabled = true;

            string ipAddress = IpTextBox.Text;
            await _client.ConnectAsync(ipAddress, 9000);
            LogMessage("Connected to server.");
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            Disconnect();

            HostButton.IsEnabled = true;
            JoinButton.IsEnabled = true;
            DisconnectButton.IsEnabled = false;
            SendButton.IsEnabled = false;
        }

        private void Disconnect()
        {
            if (_server != null)
                _server.Stop();
            else if (_client != null)
                _client.Disconnect();
            LogMessage("Disconnected.");
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string message = MessageTextBox.Text;
            MessageTextBox.Clear();

            if (_client != null && _client.IsConnected)
            {
                await _client.SendMessageAsync(message);
                LogMessage($"Sent message: {message}");
            }
            else if (_server != null)
            {
                await _server.BroadcastMessageAsync(message);
                LogMessage($"Broadcasted message: {message}");
            }

            // Keep the focus on the MessageTextBox after sending
            MessageTextBox.Focus();
        }

        private void LogMessage(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                LogTextBox.AppendText($"{message}\n");
                LogScrollViewer.ScrollToEnd(); // Scroll to the bottom of the log automatically
            });
        }

        private void OnClientMessageReceived(string message)
        {
            LogMessage($"Received from server: {message}");
        }

        private void OnServerMessageReceived(string message, TcpClient client)
        {
            LogMessage($"Received from client {client.Client.RemoteEndPoint}: {message}");
            // Relay the message to other clients
            _server.BroadcastMessageToAllExceptOneAsync(message, client).ConfigureAwait(false);
        }

        private void OnClientConnected(TcpClient client)
        {
            LogMessage($"Client connected: {client.Client.RemoteEndPoint}");
        }

        // Handle the client disconnection
        private void OnClientDisconnected()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                LogMessage("Client disconnected.");
                HostButton.IsEnabled = true;
                JoinButton.IsEnabled = true;
                DisconnectButton.IsEnabled = false;
                SendButton.IsEnabled = false;
            });
        }

        // Handle the Enter key press in the MessageTextBox
        private void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && SendButton.IsEnabled)
            {
                SendButton_Click(this, new RoutedEventArgs());
                e.Handled = true; // Prevent the default action of the Enter key
            }
        }
    }
}
