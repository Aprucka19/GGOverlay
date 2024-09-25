using GGOverlay.Game;
using System;
using System.Windows;
using System.Windows.Controls;

namespace GGOverlay
{
    public partial class EditPlayerView : UserControl
    {
        private MainWindow _mainWindow;
        private IGameInterface _game;
        private Button _selectedButton;
        private int _drinkQuantity;

        public EditPlayerView(MainWindow mainWindow, IGameInterface game)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            _game = game;

            // Initialize with current player info
            var currentName = _game._localPlayer?.Name ?? string.Empty;
            var currentModifier = _game._localPlayer?.DrinkModifier ?? 1.0;
            _drinkQuantity = _game._localPlayer?.DrinkCount ?? 0;

            NameTextBox.Text = currentName;
            SetInitialModifierButton(currentModifier);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // Save player data
            var playerName = NameTextBox.Text;
            var modifier = GetSelectedModifier();

            if (string.IsNullOrWhiteSpace(playerName))
            {
                MessageBox.Show("Please enter a name.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (modifier == null)
            {
                MessageBox.Show("Please select a drink modifier.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _game.EditPlayer(playerName, modifier.Value, _drinkQuantity);
            

            _mainWindow.ShowLobbyView(_game); // Return to Lobby View
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.ShowLobbyView(_game); // Return to Lobby View
        }

        private void ModifierButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                if (_selectedButton != null)
                {
                    _selectedButton.ClearValue(Button.BackgroundProperty);
                    _selectedButton.ClearValue(Button.ForegroundProperty);
                    _selectedButton.Tag = null;
                }

                _selectedButton = button;
                _selectedButton.Background = System.Windows.Media.Brushes.LightBlue;
                _selectedButton.Foreground = System.Windows.Media.Brushes.Black;
                _selectedButton.Tag = "Selected";
            }
        }

        private void SetInitialModifierButton(double initialModifier)
        {
            foreach (var child in ModifierButtonsPanel.Children)
            {
                if (child is Button button && ConvertButtonContentToModifier(button.Content.ToString()) == initialModifier)
                {
                    _selectedButton = button;
                    _selectedButton.Background = System.Windows.Media.Brushes.LightBlue;
                    _selectedButton.Foreground = System.Windows.Media.Brushes.Black;
                    _selectedButton.Tag = "Selected";
                    break;
                }
            }
        }

        private double? GetSelectedModifier()
        {
            if (_selectedButton != null)
            {
                return ConvertButtonContentToModifier(_selectedButton.Content.ToString());
            }
            return null;
        }

        private double ConvertButtonContentToModifier(string content)
        {
            return content switch
            {
                "1/2" => 0.5,
                "5/8" => 0.625,
                "3/4" => 0.75,
                "7/8" => 0.875,
                "1" => 1.0,
                "9/8" => 1.125,
                "5/4" => 1.25,
                "3/2" => 1.5,
                _ => 0.0
            };
        }
    }
}
