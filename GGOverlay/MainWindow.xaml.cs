using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GGOverlay.Game;
using Microsoft.Win32;

using System;
using System.Windows;
using System.Windows.Input;


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
            YouSection.Visibility = Visibility.Collapsed;
            LobbySection.Visibility = Visibility.Collapsed;
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
            YouSection.Visibility = Visibility.Visible;
            LobbySection.Visibility = Visibility.Visible;
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
            // Update "You" section with local player information
            if (_game != null && _game._localPlayer != null)
            {
                LocalPlayerTextBlock.Text = $"{_game._localPlayer.Name}: Drink Modifier = {_game._localPlayer.DrinkModifier}";
            }
            else
            {
                LocalPlayerTextBlock.Text = "Click Edit Player";
            }

            // Update "Lobby" section with other players, excluding the local player
            if (_game != null && _game._players != null && _game._players.Any())
            {
                // Ensure _localPlayer is not displayed in the Lobby section by comparing properties explicitly
                var lobbyPlayersText = string.Join("\n", _game._players
                    .Where(p => p != null && !IsLocalPlayer(p)) // Use a method to robustly exclude _localPlayer
                    .Select(p => $"{p.Name}: Drink Modifier = {p.DrinkModifier}"));

                LobbyPlayersTextBlock.Text = string.IsNullOrEmpty(lobbyPlayersText) ? "No Players in Lobby" : lobbyPlayersText;
            }
            else
            {
                LobbyPlayersTextBlock.Text = "No Players in Lobby";
            }
        }

        // Helper method to check if the player is the local player
        private bool IsLocalPlayer(PlayerInfo player)
        {
            return _game != null && _game._localPlayer != null &&
                   player.Name == _game._localPlayer.Name &&
                   Math.Abs(player.DrinkModifier - _game._localPlayer.DrinkModifier) < 0.0001; // Use epsilon comparison for double values
        }

        private void UpdateUIElements()
        {
            UpdatePlayerInfoDisplay();
            UpdateGameRulesDisplay();

            // Show or hide sections based on whether a game is active
            if (_game != null)
            {
                YouSection.Visibility = Visibility.Visible;
                LobbySection.Visibility = Visibility.Visible;
            }
            else
            {
                YouSection.Visibility = Visibility.Collapsed;
                LobbySection.Visibility = Visibility.Collapsed;
            }
        }

        // Event handler to allow dragging the window
        private void TopBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Allows dragging the window around
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }


        private void EditPlayer_Click(object sender, RoutedEventArgs e)
        {
            // Check if a local player exists and pass current values to the dialog
            var currentName = _game._localPlayer?.Name ?? string.Empty;
            var currentModifier = _game._localPlayer?.DrinkModifier ?? 1.0;

            var dialog = new EditPlayerDialog(currentName, currentModifier);
            if (dialog.ShowDialog() == true)
            {
                _game.EditPlayer(dialog.PlayerName, dialog.DrinkModifier);
            }
        }

    }
}
