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
        private Counters _counters;

        public MainWindow()
        {
            InitializeComponent();
            _databaseManager = new DatabaseManager("shared_data.db");
            _networkServer = new NetworkServer(_databaseManager);
            _networkClient = new NetworkClient(_databaseManager);
            _counters = new Counters(_databaseManager); // Initialize the Counters class

            _networkServer.OnLogMessage += Log;
            _networkClient.OnDataReceived += DataReceived;

            // Ensure counters exist in the database, creating them if necessary
            _counters.InitializeCounters(); // Ensure this method is public in Counters.cs

            // Load initial state from the database
            LoadCounterValues();

            // Subscribe to database change events to update the UI
            _databaseManager.OnDatabaseChanged += UpdateUIFromDatabaseChange;
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
            int currentValue = _counters.GetCounterValue("Counter1");
            _counters.UpdateCounterValue("Counter1", currentValue + 1);
        }

        private void Counter1MinusButton_Click(object sender, RoutedEventArgs e)
        {
            int currentValue = _counters.GetCounterValue("Counter1");
            _counters.UpdateCounterValue("Counter1", currentValue - 1);
        }

        private void Counter2PlusButton_Click(object sender, RoutedEventArgs e)
        {
            int currentValue = _counters.GetCounterValue("Counter2");
            _counters.UpdateCounterValue("Counter2", currentValue + 1);
        }

        private void Counter2MinusButton_Click(object sender, RoutedEventArgs e)
        {
            int currentValue = _counters.GetCounterValue("Counter2");
            _counters.UpdateCounterValue("Counter2", currentValue - 1);
        }

        private void Counter3PlusButton_Click(object sender, RoutedEventArgs e)
        {
            int currentValue = _counters.GetCounterValue("Counter3");
            _counters.UpdateCounterValue("Counter3", currentValue + 1);
        }

        private void Counter3MinusButton_Click(object sender, RoutedEventArgs e)
        {
            int currentValue = _counters.GetCounterValue("Counter3");
            _counters.UpdateCounterValue("Counter3", currentValue - 1);
        }

        // Update UI elements when database changes occur
        private void UpdateUIFromDatabaseChange(string changeMessage)
        {
            Application.Current.Dispatcher.Invoke(() => LoadCounterValues());
        }

        // Load counter values from the database into the UI
        private void LoadCounterValues()
        {
            Counter1TextBox.Text = _counters.GetCounterValue("Counter1").ToString();
            Counter2TextBox.Text = _counters.GetCounterValue("Counter2").ToString();
            Counter3TextBox.Text = _counters.GetCounterValue("Counter3").ToString();
        }

        private void Log(string message)
        {
            if (LogTextBox.Dispatcher.CheckAccess())
            {
                LogTextBox.AppendText($"{message}\n");
                LogTextBox.ScrollToEnd();
            }
            else
            {
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
