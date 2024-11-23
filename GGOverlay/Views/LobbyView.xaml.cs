using GGOverlay.Game;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xceed.Wpf.AvalonDock.Controls;

namespace GGOverlay
{
    public partial class LobbyView : UserControl
    {
        private MainWindow _mainWindow;
        private IGameInterface _game;
        private OverlayWindow overlayWindow; // Reference to the overlay window

        public LobbyView(MainWindow mainWindow, IGameInterface game)
        {
            InitializeComponent();
            _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
            _game = game ?? throw new ArgumentNullException(nameof(game));

            // Set visibility of buttons based on whether hosting or joining
            if (_game is GameMaster)
            {
                EditRulesButton.Visibility = Visibility.Visible;
            }
            else
            {
                EditRulesButton.Visibility = Visibility.Collapsed;
            }

            SubscribeToGameEvents();
            UpdateUIElements();
        }


        private void LaunchOverlay_Click(object sender, RoutedEventArgs e)
        {
            // Minimize the Lobby window
            _mainWindow.WindowState = WindowState.Minimized;

            if (overlayWindow != null)
            {
                // Check if the OverlayWindow is still loaded and not closed
                if (overlayWindow.IsLoaded)
                {
                    // Overlay is already open, bring it to foreground and set interactive mode
                    overlayWindow.Dispatcher.Invoke(() =>
                    {
                        if (overlayWindow.WindowState == WindowState.Minimized)
                        {
                            overlayWindow.WindowState = WindowState.Normal;
                        }
                        UpdateGameInterface(_game);

                        overlayWindow.Activate(); // Bring to foreground
                        overlayWindow.SetInteractiveMode(); // Set to interactive mode
                    });

                    Debug.WriteLine("OverlayWindow is already open. Brought to foreground.");
                    return;
                }
                else
                {
                    // OverlayWindow reference exists but it's not loaded anymore
                    Debug.WriteLine("OverlayWindow reference exists but the window is not loaded. Resetting reference.");
                    overlayWindow = null;
                }
            }

            // Create and show a new OverlayWindow instance
            overlayWindow = new OverlayWindow(_game);

            // Subscribe to the Closed event using a named event handler
            overlayWindow.Closed += OverlayWindow_Closed;

            try
            {
                overlayWindow.Show();
                Debug.WriteLine("OverlayWindow created and shown.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to show OverlayWindow: {ex.Message}");
                MessageBox.Show($"Failed to open the overlay: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                overlayWindow = null;
                return;
            }

            if (_game is GameMaster gameMaster)
            {
                gameMaster.StartTimer();
            }
        }


        // Existing event handler in LobbyView.xaml.cs
        private void OverlayWindow_Closed(object sender, EventArgs e)
        {
            Debug.WriteLine("OverlayWindow has been closed.");
            overlayWindow = null;

            // Bring the main lobby window back to focus
            if (_mainWindow != null)
            {
                // Restore the window if it's minimized
                if (_mainWindow.WindowState == WindowState.Minimized)
                {
                    _mainWindow.WindowState = WindowState.Normal;
                }

                // Activate the window to bring it to the foreground
                _mainWindow.Activate();
            }
            else
            {
                Debug.WriteLine("MainWindow reference is null. Cannot activate lobby window.");
            }
        }






        private void SubscribeToGameEvents()
        {
            _game.UIUpdate += UpdateUIElements;
        }

        private void Disconnect()
        {
            _game.Stop();

        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            Disconnect();
        }

        public void CloseOverlayWindowIfOpen()
        {
            if (overlayWindow != null && overlayWindow.IsLoaded)
            {
                overlayWindow.Close(); // This will trigger the Closed event
                overlayWindow = null;
                Debug.WriteLine("OverlayWindow closed by MainWindow.");
            }
        }


        private void EditRules_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.ShowEditRulesView(_game);
        }


        public void UpdateGameInterface(IGameInterface game)
        {
            _game = game;
            UpdateUIElements();
            // Update UI elements or internal state as needed
        }


        private void UpdateGameRulesDisplay()
        {
            if (_game != null && _game._gameRules.Rules.Any())
            {
                GameRulesSection.Children.Clear(); // Clear existing rules

                // Iterate through each rule and format it with alternating colors
                for (int i = 0; i < _game._gameRules.Rules.Count; i++)
                {
                    var rule = _game._gameRules.Rules[i];
                    var ruleDescription = $"{i + 1}. {rule.RuleDescription}";
                    var punishmentDescription = rule.GetPunishmentDescription(); // Fill in placeholders

                    // Create a Border to hold the StackPanel
                    var ruleBorder = new Border
                    {
                        Margin = new Thickness(5),
                        Background = new SolidColorBrush(i % 2 == 0 ? Color.FromRgb(68, 68, 68) : Color.FromRgb(85, 85, 85)), // Alternating colors
                        Padding = new Thickness(10)
                    };

                    // Create a StackPanel for each rule and punishment
                    var ruleStackPanel = new StackPanel
                    {
                        Orientation = Orientation.Vertical
                    };

                    // Create TextBlock for the rule description
                    var ruleTextBlock = new TextBlock
                    {
                        Text = ruleDescription,
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = Brushes.White,
                        FontFamily = new FontFamily("Comic Sans"),
                        FontSize = 14
                    };

                    // Create TextBlock for the punishment description
                    var punishmentTextBlock = new TextBlock
                    {
                        Text = punishmentDescription,
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = Brushes.White,
                        FontFamily = new FontFamily("Comic Sans"),
                        FontSize = 14,
                        Margin = new Thickness(0, 5, 0, 0)
                    };

                    // Add TextBlocks to the StackPanel
                    ruleStackPanel.Children.Add(ruleTextBlock);
                    ruleStackPanel.Children.Add(punishmentTextBlock);

                    // Add the StackPanel to the Border
                    ruleBorder.Child = ruleStackPanel;

                    // Add the Border to the GameRulesSection
                    GameRulesSection.Children.Add(ruleBorder);
                }
            }
            else
            {
                GameRulesSection.Children.Clear();
                var noRulesTextBlock = new TextBlock
                {
                    Text = "No Game Rules Loaded",
                    Foreground = Brushes.White,
                    FontFamily = new FontFamily("Comic Sans"),
                    FontSize = 16,
                    Margin = new Thickness(5),
                    Padding = new Thickness(10),
                    Background = new SolidColorBrush(Color.FromRgb(68, 68, 68))
                };
                GameRulesSection.Children.Add(noRulesTextBlock);
            }
        }




        private void UpdatePlayerInfoDisplay()
        {
            // Clear the current player display
            LobbyPlayersPanel.Children.Clear();

            // Always display a box for the local player
            if (_game != null && _game._localPlayer != null)
            {
                // Create the local player box with distinct styling
                var localPlayerBox = CreatePlayerBox(_game._localPlayer, isLocal: true);
                LobbyPlayersPanel.Children.Add(localPlayerBox);
            }
            else
            {
                // Display the default box if the local player is not set
                var defaultLocalPlayerBox = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(90, 90, 90)),
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(5),
                    Padding = new Thickness(10),
                    CornerRadius = new CornerRadius(5),
                    Width = 150,
                    Height = 80
                };

                var defaultTextBlock = new TextBlock
                {
                    Text = "Click Edit Player",
                    Foreground = Brushes.White,
                    FontFamily = new FontFamily("Segoe Script"),
                    FontSize = 16,
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                defaultLocalPlayerBox.Child = defaultTextBlock;
                LobbyPlayersPanel.Children.Add(defaultLocalPlayerBox);
            }

            // Update "Lobby" section with other players, excluding the local player
            if (_game != null && _game._players != null && _game._players.Any())
            {
                foreach (var player in _game._players)
                {
                    // Skip the local player to avoid duplication
                    if (player != null && !IsLocalPlayer(player))
                    {
                        var playerBox = CreatePlayerBox(player, isLocal: false);
                        LobbyPlayersPanel.Children.Add(playerBox);
                    }
                }
            }
            else
            {
                // If no other players are present, do nothing additional; the local player box will still be shown
            }
        }

        private Border CreatePlayerBox(PlayerInfo player, bool isLocal)
        {
            // Set background colors based on whether the player is local or not
            var backgroundColor = isLocal ? Color.FromRgb(85, 85, 85) : Color.FromRgb(68, 68, 68);
            var border = new Border
            {
                Background = new SolidColorBrush(backgroundColor),
                BorderBrush = new SolidColorBrush(Color.FromRgb(90, 90, 90)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(5),
                Padding = new Thickness(10),
                CornerRadius = new CornerRadius(5),
                Width = 150, // Fixed width for consistent layout
                Height = 80  // Fixed height for consistent layout
            };

            // Display player name and drink modifier in a stack
            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            var nameTextBlock = new TextBlock
            {
                Text = player.Name,
                Foreground = Brushes.White,
                FontFamily = new FontFamily("Segoe Script"),
                FontSize = 16,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Convert drink modifier to a fraction format
            var modifierTextBlock = new TextBlock
            {
                Text = player.ReturnFraction(), // Display modifier as a fraction
                Foreground = Brushes.White,
                FontFamily = new FontFamily("Segoe Script"),
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            stackPanel.Children.Add(nameTextBlock);
            stackPanel.Children.Add(modifierTextBlock);
            border.Child = stackPanel;

            return border;
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
            UpdateDrinkingPaceDisplay();
        }

        private void UpdateDrinkingPaceDisplay()
        {
            if (_game != null && _game._gameRules != null && _game._gameRules.Pace > 0)
            {
                // Pace is set, make the TextBlock visible
                DrinkingPaceTextBlock.Visibility = Visibility.Visible;

                // Get the formatted drink description
                int paceQuantity = _game._gameRules.PaceQuantity;

                string drinkDescription = Rule.FormatDrinkDescription(paceQuantity);

                int pace = _game._gameRules.Pace;

                DrinkingPaceTextBlock.Text = $"Drinking Pace: {drinkDescription} every {pace} minutes.";
            }
            else
            {
                // Pace is not set, hide the TextBlock
                DrinkingPaceTextBlock.Visibility = Visibility.Collapsed;
            }
        }

        private void EditPlayer_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.ShowEditPlayerView(_game);
        }
    }
}
