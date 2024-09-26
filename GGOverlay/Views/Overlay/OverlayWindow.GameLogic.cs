// OverlayWindow.GameLogic.cs
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using GGOverlay.Game;
using System.Linq;
using System.Windows.Data;

namespace GGOverlay
{
    public partial class OverlayWindow
    {
        private void LoadUserDataSettings()
        {
            var userData = _game.UserData;

            // Apply overlay settings
            if (userData != null && userData.OverlaySettings != null)
            {
                // Set font color
                var fontColor = (Color)ColorConverter.ConvertFromString(userData.OverlaySettings.FontColor);
                SetTextColor(fontColor);

                // Set font scale multiplier
                fontScaleMultiplier = userData.OverlaySettings.FontScaleMultiplier;
                FontScaleSlider.Value = fontScaleMultiplier;

                // Set background color
                var backgroundColor = (Color)ColorConverter.ConvertFromString(userData.OverlaySettings.BackgroundColor);
                SetBackgroundColor(backgroundColor);

                // Set Text Opacity
                TextOpacitySlider.Value = userData.OverlaySettings.TextOpacity;
                SetTextOpacity(userData.OverlaySettings.TextOpacity);

                // Set Background Opacity
                BackgroundOpacitySlider.Value = userData.OverlaySettings.BackgroundOpacity;
                SetBackgroundOpacity(userData.OverlaySettings.BackgroundOpacity);

                // Set UnifiedBorder size
                UnifiedBorder.Width = userData.OverlaySettings.WindowWidth;
                UnifiedBorder.Height = userData.OverlaySettings.WindowHeight;

                // Set UnifiedBorder position correctly using WindowTop
                Canvas.SetLeft(UnifiedBorder, userData.OverlaySettings.WindowLeft);
                Canvas.SetTop(UnifiedBorder, userData.OverlaySettings.WindowTop);
            }
            else
            {
                // If no settings found, ensure default position and size
                UnifiedBorder.Width = 300;
                UnifiedBorder.Height = 400;
                Canvas.SetLeft(UnifiedBorder, 50);
                Canvas.SetTop(UnifiedBorder, 50);
            }
        }

        private void SaveUserDataSettings()
        {
            // Optional: Add null checks to prevent NullReferenceException
            if (_game?.UserData?.OverlaySettings == null)
                return;

            var userData = _game.UserData;

            // Save overlay settings
            // Set font color
            var fontColor = currentTextColor;
            userData.OverlaySettings.FontColor = fontColor.ToString();

            // Set font scale
            userData.OverlaySettings.FontScaleMultiplier = fontScaleMultiplier;

            // Set background color
            var backgroundColor = currentBackgroundColor;
            userData.OverlaySettings.BackgroundColor = backgroundColor.ToString();

            // Set Text Opacity
            userData.OverlaySettings.TextOpacity = TextOpacitySlider.Value;

            // Set Background Opacity
            userData.OverlaySettings.BackgroundOpacity = BackgroundOpacitySlider.Value;

            // Set UnifiedBorder size
            userData.OverlaySettings.WindowWidth = UnifiedBorder.Width;
            userData.OverlaySettings.WindowHeight = UnifiedBorder.Height;

            // Set UnifiedBorder position
            userData.OverlaySettings.WindowLeft = Canvas.GetLeft(UnifiedBorder);
            userData.OverlaySettings.WindowTop = Canvas.GetTop(UnifiedBorder);

            // Save to file
            userData.Save();
        }

        private void LoadGameRules()
        {
            // Load game rules into GameRulesPanel
            if (_game != null && _game._gameRules != null && _game._gameRules.Rules != null)
            {
                GameRulesPanel.Children.Clear();
                bool alternate = false;
                foreach (var rule in _game._gameRules.Rules)
                {
                    // Create a Border for the rule with consistent BorderThickness
                    Border ruleBorder = new Border
                    {
                        BorderThickness = new Thickness(2), // Consistent thickness
                        BorderBrush = Brushes.White,
                        CornerRadius = new CornerRadius(3),
                        Margin = new Thickness(2),
                        Padding = new Thickness(5),
                        Background = alternate ? new SolidColorBrush(Color.FromArgb(128, 50, 50, 50)) : new SolidColorBrush(Color.FromArgb(128, 70, 70, 70)),
                        Cursor = Cursors.Hand,
                        IsHitTestVisible = isInteractive
                    };

                    // Create a TextBlock for the rule
                    TextBlock ruleText = new TextBlock
                    {
                        Text = rule.RuleDescription,
                        Foreground = new SolidColorBrush(currentTextColor),
                        FontSize = 14 * fontScaleMultiplier, // Apply font scale
                        Margin = new Thickness(0),
                        TextWrapping = TextWrapping.Wrap,
                        FontFamily = new FontFamily(currentFont),
                        Opacity = currentTextOpacity // Set text opacity
                    };

                    ruleBorder.Child = ruleText;

                    // Add event handler for selection
                    ruleBorder.MouseLeftButtonDown += RuleBorder_MouseLeftButtonDown;

                    GameRulesPanel.Children.Add(ruleBorder);

                    alternate = !alternate;
                }
            }
        }




        private void RuleBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!isInteractive)
                return;

            Border clickedBorder = sender as Border;
            if (clickedBorder != null)
            {
                int index = GameRulesPanel.Children.IndexOf(clickedBorder);
                if (index >= 0 && index < _game._gameRules.Rules.Count)
                {
                    SelectRule(_game._gameRules.Rules[index], clickedBorder);
                }
            }
        }



        private void LoadLobbyMembers()
        {
            // Load lobby members into LobbyMembersPanel
            if (_game != null && _game._players != null)
            {
                LobbyMembersPanel.Children.Clear();
                _playerBorders.Clear();

                // Create a list to hold players, with local player first if exists
                List<PlayerInfo> players = new List<PlayerInfo>();

                // Assume _game has a LocalPlayer property
                PlayerInfo localPlayer = _game._localPlayer;
                if (localPlayer != null && _game._players.Contains(localPlayer))
                {
                    players.Add(localPlayer);
                    players.AddRange(_game._players.Where(p => p != localPlayer));
                }
                else
                {
                    players = _game._players.ToList();
                }

                for (int i = 0; i < players.Count; i++)
                {
                    var player = players[i];

                    // Assign color based on index
                    Color playerColor = _playerColors[i % _playerColors.Count];
                    Color colorWithOpacity = Color.FromArgb((byte)(255), playerColor.R, playerColor.G, playerColor.B);

                    // Create a Border for the player with consistent BorderThickness
                    Border playerBorder = new Border
                    {
                        BorderThickness = new Thickness(2), // Consistent thickness
                        BorderBrush = Brushes.Transparent, // Default border brush
                        CornerRadius = new CornerRadius(5),
                        Padding = new Thickness(5),
                        Background = new SolidColorBrush(colorWithOpacity),
                        Cursor = Cursors.Hand,
                        IsHitTestVisible = isInteractive
                    };

                    // Create a StackPanel inside the Border
                    StackPanel playerStack = new StackPanel
                    {
                        Orientation = Orientation.Vertical,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    // Player Name
                    TextBlock nameText = new TextBlock
                    {
                        Text = player.Name,
                        Foreground = new SolidColorBrush(currentTextColor),
                        FontSize = 14 * fontScaleMultiplier,
                        FontFamily = new FontFamily(currentFont),
                        Margin = new Thickness(0, 0, 0, 2),
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Center,
                        Opacity = currentTextOpacity // Set text opacity
                    };

                    // Drink Count Text
                    double drinks = player.DrinkCount / 20.0;
                    string drinksText = $"{drinks} Drinks";

                    TextBlock drinkText = new TextBlock
                    {
                        Text = drinksText,
                        Foreground = new SolidColorBrush(currentTextColor),
                        FontSize = 12 * fontScaleMultiplier,
                        FontFamily = new FontFamily(currentFont),
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Center,
                        Opacity = currentTextOpacity // Set text opacity
                    };

                    playerStack.Children.Add(nameText);
                    playerStack.Children.Add(drinkText);

                    playerBorder.Child = playerStack;

                    // Add event handler for selection
                    playerBorder.MouseLeftButtonDown += PlayerBorder_MouseLeftButtonDown;

                    // Associate the PlayerInfo object with the Border
                    playerBorder.Tag = player;

                    LobbyMembersPanel.Children.Add(playerBorder);
                    _playerBorders.Add(playerBorder);
                }

                AdjustPlayerBoxesWidth(); // Adjust player boxes' width and margins after loading
            }
        }





        private void PlayerBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!isInteractive)
                return;

            Border clickedBorder = sender as Border;
            if (clickedBorder != null)
            {
                // Retrieve the PlayerInfo from the Tag property
                PlayerInfo player = clickedBorder.Tag as PlayerInfo;
                if (player != null)
                {
                    SelectPlayer(player, clickedBorder);
                }
            }
        }


        private void SelectRule(Rule rule, Border border)
        {
            // Deselect previous rule
            if (selectedRuleBorder != null)
            {
                selectedRuleBorder.BorderBrush = Brushes.White; // Reset to default color
            }

            // Select new rule
            selectedRule = rule;
            selectedRuleBorder = border;

            selectedRuleBorder.BorderBrush = Brushes.Yellow; // Highlight selected rule

            // If the rule is a group punishment, deselect any selected player
            if (selectedRule.IsGroupPunishment)
            {
                DeselectPlayer();
            }

            UpdateConfirmButtonVisibility();
        }

        private void DeselectRule()
        {
            if (selectedRuleBorder != null)
            {
                selectedRuleBorder.BorderBrush = Brushes.White; // Reset to default color
                selectedRuleBorder = null;
            }
            selectedRule = null;
        }

        private void SelectPlayer(PlayerInfo player, Border border)
        {
            // Deselect previous player
            if (selectedPlayerBorder != null)
            {
                selectedPlayerBorder.BorderBrush = Brushes.Transparent; // Reset to default color
            }

            // If a group rule is selected, deselect it
            if (selectedRule != null && selectedRule.IsGroupPunishment)
            {
                DeselectRule();
            }

            // Select new player
            selectedPlayer = player;
            selectedPlayerBorder = border;

            selectedPlayerBorder.BorderBrush = Brushes.Yellow; // Highlight selected player

            UpdateConfirmButtonVisibility();
        }

        private void DeselectPlayer()
        {
            if (selectedPlayerBorder != null)
            {
                selectedPlayerBorder.BorderBrush = Brushes.Transparent; // Reset to default color
                selectedPlayerBorder = null;
            }
            selectedPlayer = null;
        }

        private void UpdateConfirmButtonVisibility()
        {
            if (selectedRule != null)
            {
                if (selectedRule.IsGroupPunishment)
                {
                    ConfirmButton.Visibility = Visibility.Visible;
                    CancelButton.Visibility = Visibility.Visible;
                }
                else
                {
                    // Individual punishment
                    if (selectedPlayer != null)
                    {
                        ConfirmButton.Visibility = Visibility.Visible;
                        CancelButton.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        ConfirmButton.Visibility = Visibility.Collapsed;
                        CancelButton.Visibility = Visibility.Visible;
                    }
                }
            }
            else
            {
                ConfirmButton.Visibility = Visibility.Collapsed;
                CancelButton.Visibility = Visibility.Collapsed;
            }
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedRule != null)
            {
                if (selectedRule.IsGroupPunishment)
                {
                    _game.TriggerGroupRule(selectedRule);
                }
                else if (selectedPlayer != null)
                {
                    _game.TriggerIndividualRule(selectedRule, selectedPlayer);
                }

                // After confirmation, reset selections
                DeselectAll();
                UpdateConfirmButtonVisibility();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DeselectAll();
            UpdateConfirmButtonVisibility();
        }

        private void DeselectAll()
        {
            DeselectRule();
            DeselectPlayer();
        }

        private void OnGameUIUpdate()
        {
            Dispatcher.Invoke(() =>
            {
                LoadGameRules();
                LoadLobbyMembers();
                AdjustFontSizes(UnifiedBorder); // Ensure font sizes are adjusted on update
                ApplyTextOpacity(); // Apply text opacity if needed
            });
        }

        private void HandleIndividualPunishmentTriggered(Rule rule, PlayerInfo player)
        {
            Dispatcher.Invoke(() =>
            {
                CreatePunishmentDisplay(rule, player);
            });
        }

        private void HandleGroupPunishmentTriggered(Rule rule)
        {
            Dispatcher.Invoke(() =>
            {
                CreatePunishmentDisplay(rule);
            });
        }


        private void CreatePunishmentDisplay(Rule rule, PlayerInfo player = null)
        {
            // Adjust the background color's opacity by modifying the alpha channel
            Color backgroundColorWithOpacity = Color.FromArgb(
                (byte)(currentBackgroundOpacity * 255),
                currentBackgroundColor.R,
                currentBackgroundColor.G,
                currentBackgroundColor.B
            );

            // Create a new Border for the punishment display
            Border punishmentBorder = new Border
            {
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(20),
                Background = new SolidColorBrush(backgroundColorWithOpacity), // Set background with adjusted opacity
                Margin = new Thickness(0, 0, 0, 10), // Add margin between punishment displays
                IsHitTestVisible = isInteractive
            };

            // Create a StackPanel to hold the content
            StackPanel punishmentStackPanel = new StackPanel();

            // Create TextBlock for punishment description
            TextBlock punishmentDescriptionText = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                FontSize = 14 * fontScaleMultiplier * 3, // Adjust font size for emphasis
                Foreground = new SolidColorBrush(currentTextColor),
                FontFamily = new FontFamily(currentFont),
                Opacity = currentTextOpacity // Set text opacity
            };

            // Get the formatted punishment description
            punishmentDescriptionText.Text = rule.GetPunishmentDescription(
                player?.Name, player?.DrinkModifier ?? _game._localPlayer.DrinkModifier);

            // Create Confirm Button
            Button punishmentConfirmButton = new Button
            {
                Content = "Confirm",
                Width = 100,
                Height = 40,
                Margin = new Thickness(0, 20, 0, 0),
                Visibility = Visibility.Collapsed
            };

            // Add Click event handler for the confirm button
            punishmentConfirmButton.Click += (s, e) =>
            {
                // Remove this punishment display from the stack panel
                PunishmentDisplayStackPanel.Children.Remove(punishmentBorder);
            };

            // Add elements to the StackPanel
            punishmentStackPanel.Children.Add(punishmentDescriptionText);
            punishmentStackPanel.Children.Add(punishmentConfirmButton);

            // Add StackPanel to Border
            punishmentBorder.Child = punishmentStackPanel;

            // Add punishmentBorder to PunishmentDisplayStackPanel
            PunishmentDisplayStackPanel.Children.Add(punishmentBorder);

            // Show the confirm button if necessary
            if (player == null || string.Equals(player.Name, _game._localPlayer.Name, StringComparison.OrdinalIgnoreCase))
            {
                punishmentConfirmButton.Visibility = Visibility.Visible;
            }
            else
            {
                // Start a timer to auto-hide after 5 seconds
                DispatcherTimer individualPunishmentTimer = new DispatcherTimer();
                individualPunishmentTimer.Interval = TimeSpan.FromSeconds(5);
                individualPunishmentTimer.Tick += (sender, args) =>
                {
                    // Remove this punishment display from the stack panel
                    PunishmentDisplayStackPanel.Children.Remove(punishmentBorder);
                    individualPunishmentTimer.Stop();
                };
                individualPunishmentTimer.Start();
            }
        }



    }
}
