﻿using GGOverlay.Game;
using Microsoft.Win32;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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



        private void SubscribeToGameEvents()
        {
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



        private void EditRules_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.ShowEditRulesView(_game);
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
                //LocalPlayerTextBlock.Text = $"{_game._localPlayer.Name}: Drink Modifier = {_game._localPlayer.DrinkModifier}";

                // Create the local player box with distinct styling
                var localPlayerBox = CreatePlayerBox(_game._localPlayer, isLocal: true);
                LobbyPlayersPanel.Children.Add(localPlayerBox);
            }
            else
            {
                // Display the default box if the local player is not set
                //LocalPlayerTextBlock.Text = "Click Edit Player";

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
                Text = ConvertToFraction(player.DrinkModifier), // Display modifier as a fraction
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


        // Helper method to convert a double to a fraction string
        // Helper method to convert a double to a fraction string
        private string ConvertToFraction(double value)
        {
            // Check for the special case where the value is exactly 1
            if (Math.Abs(value - 1) < 1.0E-6)
            {
                return "1";
            }

            // Define tolerance for precision
            double tolerance = 1.0E-6;
            double numerator = value;
            double denominator = 1;
            double gcd;

            // Iteratively adjust numerator and denominator until the value is approximately the same as input
            while (Math.Abs(numerator % 1) > tolerance)
            {
                numerator *= 10;
                denominator *= 10;
            }

            // Find the greatest common divisor to simplify the fraction
            gcd = GCD((int)numerator, (int)denominator);

            // Return fraction string representation
            return $"{(int)numerator / gcd}/{(int)denominator / gcd}";
        }

        // Helper method to calculate the Greatest Common Divisor (GCD)
        private int GCD(int a, int b)
        {
            return b == 0 ? a : GCD(b, a % b);
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