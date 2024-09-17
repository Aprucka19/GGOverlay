using System;
using System.Windows;
using System.Windows.Input;
using GGOverlay.Game;

namespace GGOverlay
{
    public partial class MainWindow : Window
    {
        private IGameInterface _game;
        private bool _subscribed;

        public static readonly RoutedCommand ToggleLogsCommand = new RoutedCommand();

        public MainWindow()
        {
            _subscribed = false;
            InitializeComponent();
            ShowLaunchView();
        }

        public void ShowLaunchView()
        {
            this.Title = "GGOverlay - Connection Window";
            ContentArea.Content = new LaunchView(this);
        }

        public void ShowLobbyView(IGameInterface game)
        {
            this.Title = "GGOverlay - Lobby";
            ContentArea.Content = new LobbyView(this, game);

            // Store the game reference
            _game = game;


            if (!_subscribed)
            {
                _subscribed = true;
                // Subscribe to the game's OnLog event
                _game.OnLog += LogMessage;

                // Optionally, handle the game's OnDisconnect event
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
            // Unsubscribe from the game's OnLog event when the game disconnects
            if (_game != null)
            {
                _game.OnLog -= LogMessage;
                _game.OnDisconnect -= Game_OnDisconnect;
                _game = null;
                _subscribed = false;
            }
        }
    }
}
