using GGOverlay.Game;
using Microsoft.Win32;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.IO;
using System.Windows.Input;

namespace GGOverlay
{
    public partial class MainWindow : Window
    {
        private IGameInterface _game;
        private bool _subscribed;
        private LobbyView lobbyView;


        public static readonly RoutedCommand ToggleLogsCommand = new RoutedCommand();

        public MainWindow()
        {
            _subscribed = false;
            InitializeComponent();
            CopyDefaultRulesetsToUserFolder();
            ShowLaunchView();
        }

        public void ShowLaunchView()
        {
            this.Title = "GGOverlay - Connection Window";
            ContentArea.Content = new LaunchView(this);
        }

        public void ShowLobbyView(IGameInterface game)
        {
            if (lobbyView == null)
            {
                lobbyView = new LobbyView(this, game);
            }
            else
            {
                // Optionally, update the game interface if necessary
                lobbyView.UpdateGameInterface(game);
            }

            ContentArea.Content = lobbyView;

            if (!_subscribed)
            {
                _subscribed = true;
                _game = game;
                _game.OnLog += LogMessage;
                _game.OnDisconnect += Game_OnDisconnect;
            }
        }


        public void ShowEditRulesView(IGameInterface game)
        {
            this.Title = "GGOverlay - Edit Rules";
            ContentArea.Content = new EditRulesView(this, game);
        }

        public void ShowEditPlayerView(IGameInterface game)
        {
            this.Title = "GGOverlay - Edit Player";
            ContentArea.Content = new EditPlayerView(this, game);
        }

        private void TopBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Allows dragging the window around
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                MaximizeButton.Content = "\u2610"; // Empty square
            }
            else
            {
                this.WindowState = WindowState.Maximized;
                MaximizeButton.Content = "\u2752"; // Overlapping squares
            }
        }
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void LogMessage(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                LogTextBox.AppendText($"{message}\n");
                LogTextBox.ScrollToEnd();
            });
        }


        private void ToggleLogsCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ToggleLogsVisibility();
        }

        private void ToggleLogsVisibility()
        {
            if (LogGrid.Visibility == Visibility.Visible)
            {
                LogGrid.Visibility = Visibility.Collapsed;
            }
            else
            {
                LogGrid.Visibility = Visibility.Visible;
            }
        }



        private void Game_OnDisconnect()
        {
            if (lobbyView != null)
            {
                lobbyView.CloseOverlayWindowIfOpen(); // Close the OverlayWindow if it's open
                ContentArea.Content = null; // Remove LobbyView from the UI
                lobbyView = null; // Remove the reference to allow garbage collection
            }

            if (_game != null)
            {
                _game.OnLog -= LogMessage;
                _game.OnDisconnect -= Game_OnDisconnect;
                _game = null;
                _subscribed = false;
            }

            ShowLaunchView(); // Navigate back to the LaunchView
        }



        private void CopyDefaultRulesetsToUserFolder()
        {
            // Define the user-specific GameRulesets directory
            string userRulesDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "GGOverlay",
                "GameRulesets"
            );

            // Check if the directory exists
            if (!Directory.Exists(userRulesDirectory))
            {
                // Create the directory
                Directory.CreateDirectory(userRulesDirectory);
            }

            // Copy default rulesets from application directory
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string defaultRulesDirectory = Path.Combine(appDirectory, "DefaultRulesets");

            if (Directory.Exists(defaultRulesDirectory))
            {
                foreach (string filePath in Directory.GetFiles(defaultRulesDirectory, "*.json"))
                {
                    string fileName = Path.GetFileName(filePath);
                    string destFilePath = Path.Combine(userRulesDirectory, fileName);

                    try
                    {
                        // Copy and overwrite any existing files with the same name
                        File.Copy(filePath, destFilePath, overwrite: true);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to copy default ruleset '{fileName}': {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_game != null)
            {
                _game.OnLog -= LogMessage;
                _game.OnDisconnect -= Game_OnDisconnect;
                _game = null;
                _subscribed = false;
            }

            base.OnClosing(e);

        }
    }
}
