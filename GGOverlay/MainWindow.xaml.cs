using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GGOverlay.Game;
using Microsoft.Win32;

namespace GGOverlay
{
    public partial class MainWindow : Window
    {
        private GameMaster _gameMaster;
        private GameClient _gameClient;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void HostButton_Click(object sender, RoutedEventArgs e)
        {
            _gameMaster = new GameMaster();
            _gameMaster.OnLog += LogMessage;
            _gameMaster.UIUpdate += UpdateGameRulesDisplay;

            HostButton.IsEnabled = false;
            JoinButton.IsEnabled = false;
            DisconnectButton.IsEnabled = true;
            SetRules.Visibility = Visibility.Visible;

            try
            {
                await _gameMaster.HostGame(25565); // Arbitrary port, can be adjusted
            }
            catch (Exception ex)
            {
                LogMessage($"Error hosting game: {ex.Message}");
                ResetUIState();
            }
        }


        private async void SetRules_Click(object sender, RoutedEventArgs e)
        {
            // Open a file dialog to allow the user to select the rules file
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select Game Rules File",
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*"
            };

            // Show the file dialog and check if the user selected a file
            if (openFileDialog.ShowDialog() == true)
            {
                string filepath = openFileDialog.FileName;
                try
                {
                    // Set the game rules from the selected file
                    await _gameMaster.SetGameRules(filepath);
                    LogMessage("Game rules set from file.");

                    // Update the Game Rules display
                    UpdateGameRulesDisplay();
                }
                catch (Exception ex)
                {
                    LogMessage($"Error setting game rules: {ex.Message}");
                }
            }
            else
            {
                LogMessage("No file selected.");
            }
        }

        private async void JoinButton_Click(object sender, RoutedEventArgs e)
        {
            _gameClient = new GameClient();
            _gameClient.OnLog += LogMessage;
            _gameClient.OnDisconnectedGameClient += OnClientDisconnected;
            _gameClient.UIUpdate += UpdateGameRulesDisplay;

            HostButton.IsEnabled = false;
            JoinButton.IsEnabled = false;
            DisconnectButton.IsEnabled = true;

            string ipAddress = IpTextBox.Text;

            bool success = await _gameClient.JoinGame(ipAddress, 25565);
            if (!success)
            {
                ResetUIState();
            }
        }

        // Method to reset the UI state to allow the user to try connecting again
        private void ResetUIState()
        {
            HostButton.IsEnabled = true;
            JoinButton.IsEnabled = true;
            DisconnectButton.IsEnabled = false;
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            Disconnect();

            ResetUIState();
        }

        private void Disconnect()
        {
            _gameMaster?.StopServer();
            _gameClient?.Disconnect();
            LogMessage("Disconnected.");
        }

        private void LogMessage(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                LogTextBox.AppendText($"{message}\n");
                LogScrollViewer.ScrollToEnd();
            });
        }

        // Handle client disconnection
        private void OnClientDisconnected()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                LogMessage("Server Closed.");
                ResetUIState();
            });
        }

        private void ToggleLogs_Click(object sender, RoutedEventArgs e)
        {
            // Toggle the visibility of the LogScrollViewer
            if (LogScrollViewer.Visibility == Visibility.Visible)
            {
                LogScrollViewer.Visibility = Visibility.Collapsed;
                ToggleLogs.Content = "Show Logs"; // Update button text to indicate the next action
            }
            else
            {
                LogScrollViewer.Visibility = Visibility.Visible;
                ToggleLogs.Content = "Hide Logs"; // Update button text to indicate the next action
            }
        }


        // Update the Game Rules Display TextBlock
        private void UpdateGameRulesDisplay()
        {
            if (_gameMaster != null && _gameMaster._gameRules.Rules.Any())
            {
                var rulesText = string.Join("\n", _gameMaster._gameRules.Rules.Select(r =>
                    $"{(r.IsGroupPunishment ? "Group" : "Individual")} Punishment: {r.RuleDescription} - {r.PunishmentDescription} ({r.PunishmentQuantity})"));

                GameRulesTextBlock.Text = rulesText;
            }
            else if (_gameClient != null && _gameClient._gameRules.Rules.Any())
            {
                var rulesText = string.Join("\n", _gameClient._gameRules.Rules.Select(r =>
                    $"{(r.IsGroupPunishment ? "Group" : "Individual")} Punishment: {r.RuleDescription} - {r.PunishmentDescription} ({r.PunishmentQuantity})"));

                GameRulesTextBlock.Text = rulesText;
            }
            else
            {
                GameRulesTextBlock.Text = "No Game Rules Loaded";
            }
        }
    }
}
