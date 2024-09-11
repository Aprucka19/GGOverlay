// MainWindow.xaml.cs
using GGOverlay.Data;
using GGOverlay.Networking;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace GGOverlay
{
    public partial class MainWindow : Window
    {
        private NetworkServer _server;
        private NetworkClient _client;
        private Counter _counter;

        public MainWindow()
        {
            InitializeComponent();
            _counter = new Counter();
            _counter.OnValueChanged += BroadcastCounterValueAsync; // Attach event handler to counter changes
            UpdateCounterDisplay(_counter.Value); // Initialize the UI with the initial counter value
        }

        private async void HostButton_Click(object sender, RoutedEventArgs e)
        {
            if (_server == null) // Only create the server when the button is clicked
            {
                _server = new NetworkServer();
                _server.OnLog += Log;
                _server.OnMessageReceived += HandleMessageReceived;
                await _server.StartAsync();
            }
            else
            {
                Log("Server is already running.");
            }
        }

        private async void JoinButton_Click(object sender, RoutedEventArgs e)
        {
            if (_client == null) // Only create the client when the button is clicked
            {
                string ipAddress = IpAddressTextBox.Text.Trim();
                if (!string.IsNullOrEmpty(ipAddress))
                {
                    _client = new NetworkClient();
                    _client.OnLog += Log;
                    _client.OnMessageReceived += HandleMessageReceived;
                    await _client.ConnectAsync(ipAddress);
                }
                else
                {
                    Log("Please enter a valid IP address.");
                }
            }
            else
            {
                Log("Already connected as a client.");
            }
        }

        private void PlusButton_Click(object sender, RoutedEventArgs e)
        {
            _counter.Increment();
        }

        private void MinusButton_Click(object sender, RoutedEventArgs e)
        {
            _counter.Decrement();
        }

        private async Task BroadcastCounterValueAsync(int newValue)
        {
            string message = $"COUNTER:{newValue}";

            if (_server != null) // Broadcast to all clients if hosting
            {
                await _server.BroadcastMessageAsync(message);
            }
            else if (_client != null) // Send the updated value to the server if connected as a client
            {
                await _client.SendMessageAsync(message);
            }
        }

        private void HandleMessageReceived(string message)
        {
            Log($"Received: {message}");
            if (message.StartsWith("COUNTER:") && int.TryParse(message.Substring(8), out int newValue))
            {
                _counter.SetValue(newValue); // Update the counter value
                UpdateCounterDisplay(newValue); // Update the UI with the new value
            }
        }

        // Update the counter display on the UI thread
        private void UpdateCounterDisplay(int value)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CounterTextBox.Text = value.ToString();
            });
        }

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
