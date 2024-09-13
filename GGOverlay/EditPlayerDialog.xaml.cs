using System;
using System.Windows;

namespace GGOverlay
{
    public partial class EditPlayerDialog : Window
    {
        public string PlayerName { get; private set; }
        public double DrinkModifier { get; private set; }

        public EditPlayerDialog()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            PlayerName = NameTextBox.Text;

            // Try to parse the drink modifier input
            if (double.TryParse(ModifierTextBox.Text, out double modifier))
            {
                DrinkModifier = modifier;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please enter a valid numeric value for the drink modifier.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
