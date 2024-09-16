using System;
using System.Windows;
using System.Windows.Controls;

namespace GGOverlay
{
    public partial class EditPlayerDialog : Window
    {
        public string PlayerName { get; private set; }
        public double DrinkModifier { get; private set; }

        private Button _selectedButton; // To keep track of the currently selected button

        public EditPlayerDialog(string initialName = "", double initialModifier = 1.0)
        {
            InitializeComponent();

            // Set initial values
            NameTextBox.Text = initialName;
            SetInitialModifierButton(initialModifier);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            PlayerName = NameTextBox.Text;

            // Check if a modifier button is selected
            if (_selectedButton != null)
            {
                // Set the DrinkModifier based on the selected button's content
                DrinkModifier = ConvertButtonContentToModifier(_selectedButton.Content.ToString());
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please select a drink modifier.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // Handle clicks on the predefined modifier buttons
        private void ModifierButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                // Unhighlight the previously selected button
                if (_selectedButton != null)
                {
                    _selectedButton.ClearValue(Button.BackgroundProperty);
                    _selectedButton.ClearValue(Button.ForegroundProperty);
                    _selectedButton.Tag = null; // Clear selection tag
                }

                // Highlight the selected button
                _selectedButton = button;
                _selectedButton.Background = System.Windows.Media.Brushes.LightBlue;
                _selectedButton.Foreground = System.Windows.Media.Brushes.Black;
                _selectedButton.Tag = "Selected"; // Set selection tag for style trigger
            }
        }

        // Set the initial modifier button based on the current modifier value
        private void SetInitialModifierButton(double initialModifier)
        {
            // Find the button that corresponds to the initial modifier value
            foreach (var child in ModifierButtonsPanel.Children)
            {
                if (child is Button button && ConvertButtonContentToModifier(button.Content.ToString()) == initialModifier)
                {
                    _selectedButton = button;
                    _selectedButton.Background = System.Windows.Media.Brushes.LightBlue; // Highlight the initial button
                    _selectedButton.Foreground = System.Windows.Media.Brushes.Black; // Set the text color to black
                    _selectedButton.Tag = "Selected"; // Set the selection tag for style trigger
                    break;
                }
            }
        }


        // Convert the button content to the corresponding double value for the modifier
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
