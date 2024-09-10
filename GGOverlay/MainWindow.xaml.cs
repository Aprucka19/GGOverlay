using System.Net.Sockets;
using System.Windows;
using GGOverlay.Networking;
using GGOverlay.Database;

namespace GGOverlay
{
    public partial class MainWindow : Window
    {
        private NetworkServer _networkServer;
        private NetworkClient _networkClient;
        private DatabaseManager _databaseManager;

        public MainWindow()
        {
            InitializeComponent();
            _databaseManager = new DatabaseManager("shared_data.db");
            _networkServer = new NetworkServer(_databaseManager);
            _networkClient = new NetworkClient();

            _networkServer.OnClientConnected += ClientConnected;
            _networkServer.OnLogMessage += Log; // Subscribe to log messages from the server
            _networkClient.OnDataReceived += DataReceived;

            LoadCounterValues();
        }

        private void HostButton_Click(object sender, RoutedEventArgs e)
        {
            _networkServer.StartServer();
            Log("Server started... Waiting for clients.");
        }

        private void JoinButton_Click(object sender, RoutedEventArgs e)
        {
            string ipAddress = IpAddressTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(ipAddress))
            {
                _networkClient.ConnectToServer(ipAddress);
                Log("Connecting to server...");
            }
            else
            {
                Log("Please enter a valid IP address.");
            }
        }

        private void Counter1PlusButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateCounter("Counter1", _databaseManager.GetCounterValue("Counter1") + 1);
        }

        private void Counter1MinusButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateCounter("Counter1", _databaseManager.GetCounterValue("Counter1") - 1);
        }

        private void Counter2PlusButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateCounter("Counter2", _databaseManager.GetCounterValue("Counter2") + 1);
        }

        private void Counter2MinusButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateCounter("Counter2", _databaseManager.GetCounterValue("Counter2") - 1);
        }

        private void Counter3PlusButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateCounter("Counter3", _databaseManager.GetCounterValue("Counter3") + 1);
        }

        private void Counter3MinusButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateCounter("Counter3", _databaseManager.GetCounterValue("Counter3") - 1);
        }

        private void UpdateCounter(string counterName, int newValue)
        {
            _networkServer.UpdateCounter(counterName, newValue);
            LoadCounterValues();
        }

        private void LoadCounterValues()
        {
            Counter1TextBox.Text = _databaseManager.GetCounterValue("Counter1").ToString();
            Counter2TextBox.Text = _databaseManager.GetCounterValue("Counter2").ToString();
            Counter3TextBox.Text = _databaseManager.GetCounterValue("Counter3").ToString();
        }

        private void Log(string message)
        {
            // Use the Dispatcher to ensure the UI updates happen on the UI thread
            if (LogTextBox.Dispatcher.CheckAccess())
            {
                // If already on the UI thread, update directly
                LogTextBox.AppendText($"{message}\n");
                LogTextBox.ScrollToEnd();
            }
            else
            {
                // If not on the UI thread, invoke the update on the UI thread
                LogTextBox.Dispatcher.Invoke(() =>
                {
                    LogTextBox.AppendText($"{message}\n");
                    LogTextBox.ScrollToEnd();
                });
            }
        }


        private void ClientConnected(TcpClient client)
        {
            Log("Client connected.");
        }

        private void DataReceived(string message)
        {
            Log($"Received: {message}");
        }
    }
}
