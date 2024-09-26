using GGOverlay.Game;
using System;
using System.Windows;
using System.Windows.Controls;

namespace GGOverlay
{
    public partial class LaunchView : UserControl
    {
        private MainWindow _mainWindow;
        private IGameInterface _game;

        public LaunchView(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;

            // Set default port value
            PortTextBox.Text = "25565";
        }

        private async void HostButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear previous error messages
            ErrorMessageTextBlock.Text = "";

            // Retrieve and validate port
            if (!int.TryParse(PortTextBox.Text.Trim(), out int port))
            {
                ErrorMessageTextBlock.Text = "Please enter a valid port number.";
                return;
            }

            _game = new GameMaster(); // Create an instance of GameMaster

            // Show loading indicator and connecting text
            LoadingIndicator.Visibility = Visibility.Visible;
            ConnectingTextBlock.Visibility = Visibility.Visible;

            // Disable buttons
            HostButton.IsEnabled = false;
            JoinButton.IsEnabled = false;

            try
            {
                _mainWindow.ShowLobbyView(_game);
                await _game.Start(port); // Start hosting the game
            }
            catch (Exception ex)
            {
                // Display error message
                ErrorMessageTextBlock.Text = $"Error hosting game: {ex.Message}";
            }
            finally
            {
                // Hide loading indicator and connecting text
                LoadingIndicator.Visibility = Visibility.Collapsed;
                ConnectingTextBlock.Visibility = Visibility.Collapsed;

                // Re-enable buttons
                HostButton.IsEnabled = true;
                JoinButton.IsEnabled = true;
            }
        }

        private async void JoinButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear previous error messages
            ErrorMessageTextBlock.Text = "";

            // Retrieve and validate port and IP
            string ipAddress = IpTextBox.Text.Trim();
            if (string.IsNullOrEmpty(ipAddress))
            {
                ErrorMessageTextBlock.Text = "Please enter a valid IP address.";
                return;
            }

            if (!int.TryParse(PortTextBox.Text.Trim(), out int port))
            {
                ErrorMessageTextBlock.Text = "Please enter a valid port number.";
                return;
            }

            _game = new GameClient(); // Create an instance of GameClient

            // Show loading indicator and connecting text
            LoadingIndicator.Visibility = Visibility.Visible;
            ConnectingTextBlock.Visibility = Visibility.Visible;

            // Disable buttons
            HostButton.IsEnabled = false;
            JoinButton.IsEnabled = false;

            try
            {
                await _game.Start(port, ipAddress); // Start joining the game
                _mainWindow.ShowLobbyView(_game);
                if (_game._localPlayer != null)
                {
                    _game.EditPlayer(_game._localPlayer.Name, _game._localPlayer.DrinkModifier);
                }
            }
            catch (Exception ex)
            {
                // Display error message
                ErrorMessageTextBlock.Text = $"Error joining game: {ex.Message}";
                // Remain on the launch screen
            }
            finally
            {
                // Hide loading indicator and connecting text
                LoadingIndicator.Visibility = Visibility.Collapsed;
                ConnectingTextBlock.Visibility = Visibility.Collapsed;

                // Re-enable buttons
                HostButton.IsEnabled = true;
                JoinButton.IsEnabled = true;
            }
        }
    }
}
