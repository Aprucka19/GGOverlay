using GGOverlay.Game;
using Microsoft.Win32;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GGOverlay
{
    public partial class LobbyView : UserControl
    {
        private MainWindow _mainWindow;
        private IGameInterface _game;

        public LobbyView(MainWindow mainWindow, IGameInterface game)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            _game = game;

            SubscribeToGameEvents();
            UpdateUIElements();
        }

        private void SubscribeToGameEvents()
        {
            _game.OnLog += LogMessage;
            _game.UIUpdate += UpdateUIElements;
            _game.OnDisconnect += Disconnect;
        }

        private void Disconnect()
        {
            _game.Stop();
            _game = null;
            _mainWindow.ShowLaunchView();
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            Disconnect();
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
        }

        private void EditPlayer_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.ShowEditPlayerView(_game);
        }
    }
}
