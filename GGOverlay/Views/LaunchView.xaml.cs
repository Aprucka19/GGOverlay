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
        }

        private async void HostButton_Click(object sender, RoutedEventArgs e)
        {
            _game = new GameMaster(); // Create an instance of GameMaster
            //SubscribeToGameEvents();

            try
            {
                _mainWindow.ShowLobbyView(_game);
                await _game.Start(25565); // Start hosting the game
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error hosting game: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void JoinButton_Click(object sender, RoutedEventArgs e)
        {
            _game = new GameClient(); // Create an instance of GameClient
            //SubscribeToGameEvents();

            string ipAddress = IpTextBox.Text;

            try
            {
                _mainWindow.ShowLobbyView(_game);
                await _game.Start(25565, ipAddress); // Start joining the game
                
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error joining game: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //private void SubscribeToGameEvents()
        //{
        //    _game.OnLog += LogMessage;
        //    _game.UIUpdate += UpdateUIElements;
        //    _game.OnDisconnect += _mainWindow.ShowLaunchView;
        //}

        //private void LogMessage(string message)
        //{
        //    // Implement logging if necessary
        //}

        //private void UpdateUIElements()
        //{
        //    // Implement UI updates if necessary
        //}
    }
}
