using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using GGOverlay.Game;
using Microsoft.Win32;

namespace GGOverlay
{
    public partial class MainWindow : Window
    {
        private IGameInterface _game; // Unified interface for both hosting and joining

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void HostButton_Click(object sender, RoutedEventArgs e)
        {
            _game = new GameMaster(); // Create an instance of GameMaster
            SubscribeToGameEvents();
            
            try
            {
                ShowLobbyUI();
                UpdateUIElements();
                SetRules.Visibility = Visibility.Visible; // Show the Set Rules button for the host
                await _game.Start(25565); // Start hosting the game
            }
            catch (Exception ex)
            {
                LogMessage($"Error hosting game: {ex.Message}");
                ResetUIState();
            }
        }

        private async void JoinButton_Click(object sender, RoutedEventArgs e)
        {
            _game = new GameClient(); // Create an instance of GameClient
            SubscribeToGameEvents();
            _game.OnDisconnect += Disconnect;
            

            string ipAddress = IpTextBox.Text;

            try
            {
                await _game.Start(25565, ipAddress); // Start joining the game
                ShowLobbyUI();
              
            }
            catch (Exception ex)
            {
                LogMessage($"Error joining game: {ex.Message}");
                ResetUIState();
            }
        }

        private void SubscribeToGameEvents()
        {
            _game.OnLog += LogMessage;
            _game.UIUpdate += UpdateUIElements;
        }

        private void ResetUIState()
        {
            HostButton.Visibility = Visibility.Visible;
            JoinButton.Visibility = Visibility.Visible;
            IpTextBox.Visibility = Visibility.Visible;
            PlayerInfoSection.Visibility = Visibility.Collapsed;
            EditPlayerButton.Visibility = Visibility.Collapsed;
            GameRulesSection.Visibility = Visibility.Collapsed;
            SetRules.Visibility = Visibility.Collapsed;
            DisconnectButton.Visibility = Visibility.Collapsed;
        }

        private void ShowLobbyUI()
        {
            // Hide connection-related elements
            HostButton.Visibility = Visibility.Collapsed;
            JoinButton.Visibility = Visibility.Collapsed;
            IpTextBox.Visibility = Visibility.Collapsed;

            // Show gameplay-related elements
            DisconnectButton.Visibility = Visibility.Visible;
            PlayerInfoSection.Visibility = Visibility.Visible;
            EditPlayerButton.Visibility = Visibility.Visible;
            GameRulesSection.Visibility = Visibility.Visible;
            DisconnectButton.IsEnabled = true;
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            
            Disconnect();
        }

        private void Disconnect()
        {
            ResetUIState();
            _game?.Stop();
            _game = null;
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

        private async void SetRules_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select Game Rules File",
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filepath = openFileDialog.FileName;
                try
                {
                    await _game.SetGameRules(filepath);
                    LogMessage("Game rules set from file.");
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

        private void ToggleLogs_Click(object sender, RoutedEventArgs e)
        {
            if (LogScrollViewer.Visibility == Visibility.Visible)
            {
                LogScrollViewer.Visibility = Visibility.Collapsed;
                ToggleLogs.Content = "Show Logs";
            }
            else
            {
                LogScrollViewer.Visibility = Visibility.Visible;
                ToggleLogs.Content = "Hide Logs";
            }
        }

        private void UpdateGameRulesDisplay()
        {
            if (_game != null && _game._gameRules.Rules.Any())
            {
                var rulesText = string.Join("\n", _game._gameRules.Rules.Select(r =>
                    $"{(r.IsGroupPunishment ? "Group" : "Individual")} Punishment: {r.RuleDescription} - {r.PunishmentDescription} ({r.PunishmentQuantity})"));

                GameRulesTextBlock.Text = rulesText;
            }
            else
            {
                GameRulesTextBlock.Text = "No Game Rules Loaded";
            }
        }

        private void UpdatePlayerInfoDisplay()
        {
            if (_game != null && _game._players != null && _game._players.Any())
            {
                // Filter out any null player objects before creating the display text
                var playersText = string.Join("\n", _game._players
                    .Where(p => p != null) // Exclude null players
                    .Select(p => $"{p.Name}: Drink Modifier = {p.DrinkModifier}"));

                PlayerInfoTextBlock.Text = string.IsNullOrEmpty(playersText) ? "No Player Info Loaded" : playersText;
            }
            else
            {
                PlayerInfoTextBlock.Text = "No Player Info Loaded";
            }
        }


        private void UpdateUIElements()
        {
            UpdatePlayerInfoDisplay();
            UpdateGameRulesDisplay();
        }

        private void EditPlayer_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new EditPlayerDialog();
            if (dialog.ShowDialog() == true)
            {
                _game.EditPlayer(dialog.PlayerName, dialog.DrinkModifier);
            }
        }
    }
}
