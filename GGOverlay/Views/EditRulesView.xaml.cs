using GGOverlay.Game;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GGOverlay
{
    public partial class EditRulesView : UserControl
    {
        private MainWindow _mainWindow;
        private IGameInterface _game;
        private string _currentFileName = "Unnamed Rule Set";

        private ObservableCollection<Rule> _currentRules;
        private List<Rule> _originalRules;
        private string _originalSourceFilePath;
        private string _currentSourceFilePath;


        public EditRulesView(MainWindow mainWindow, IGameInterface game)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            _game = game;

            // Make a deep copy of the original rules
            _originalRules = _game._gameRules.Rules.Select(r => r.Clone()).ToList();

            // Initialize the current rules collection
            _currentRules = new ObservableCollection<Rule>(_originalRules.Select(r => r.Clone()));

            // Bind the ItemsControl to the current rules
            RulesItemsControl.ItemsSource = _currentRules;

            // Hide the example section by default
            ExampleSection.Visibility = Visibility.Collapsed;

            // Initialize _currentFileName and button state based on SourceFilePath
            if (!string.IsNullOrEmpty(_game._gameRules.SourceFilePath))
            {
                _currentFileName = System.IO.Path.GetFileName(_game._gameRules.SourceFilePath);
                CreateNewRulesButton.IsEnabled = true;
            }
            else
            {
                _currentFileName = "Unnamed Rule Set";
                CreateNewRulesButton.IsEnabled = false;
            }
            FileNameTextBlock.Text = _currentFileName;

            // Store the original SourceFilePath
            _originalSourceFilePath = _game._gameRules.SourceFilePath;

            // Initialize the current SourceFilePath
            _currentSourceFilePath = _originalSourceFilePath;
        }


        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Revert the rules to the original state
            _currentRules = new ObservableCollection<Rule>(_originalRules.Select(r => r.Clone()));
            RulesItemsControl.ItemsSource = _currentRules;

            // Revert the current SourceFilePath
            _currentSourceFilePath = _originalSourceFilePath;

            // Update the UI
            _currentFileName = string.IsNullOrEmpty(_originalSourceFilePath) ? "Unnamed Rule Set" : System.IO.Path.GetFileName(_originalSourceFilePath);
            FileNameTextBlock.Text = _currentFileName;

            // Navigate back to the Lobby view
            _mainWindow.ShowLobbyView(_game);
        }



        private void AddRule_Click(object sender, RoutedEventArgs e)
        {
            _currentRules.Add(new Rule());
        }

        private void DeleteRule_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Rule rule)
            {
                _currentRules.Remove(rule);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Update the game's rules
            _game._gameRules.Rules = _currentRules.Select(r => r.Clone()).ToList();
            _game._gameRules.SourceFilePath = _currentSourceFilePath;

            // Define the user-specific GameRulesets directory
            string userRulesDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "GGOverlay",
                "GameRulesets"
            );

            // Define the application-specific DefaultRulesets directory
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string defaultRulesDirectory = Path.Combine(appDirectory, "DefaultRulesets");

            // Determine the default save directory
            string defaultSaveDirectory;

            // Use the user directory if it exists
            if (Directory.Exists(userRulesDirectory))
            {
                defaultSaveDirectory = userRulesDirectory;
            }
            // Otherwise, use the application directory if it exists
            else if (Directory.Exists(defaultRulesDirectory))
            {
                defaultSaveDirectory = defaultRulesDirectory;
            }
            else
            {
                // Handle the case where neither directory exists
                MessageBox.Show("No valid directory found to save the ruleset.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Save rules to file if loaded from a file or prompt to save
            if (!string.IsNullOrEmpty(_game._gameRules.SourceFilePath))
            {
                try
                {
                    _game._gameRules.SaveToFile(_game._gameRules.SourceFilePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save the ruleset: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            else
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Title = "Save Game Rules",
                    Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                    InitialDirectory = defaultSaveDirectory,
                    FileName = _currentFileName
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    _currentFileName = Path.GetFileName(saveFileDialog.FileName);
                    FileNameTextBlock.Text = _currentFileName;

                    try
                    {
                        _game._gameRules.SaveToFile(saveFileDialog.FileName);
                        _game._gameRules.SourceFilePath = saveFileDialog.FileName;
                        _currentSourceFilePath = saveFileDialog.FileName;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to save the ruleset: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("Rules not saved. Please save your rules.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            // Update the original rules after saving
            _originalRules = _currentRules.Select(r => r.Clone()).ToList();
            _originalSourceFilePath = _currentSourceFilePath;

            // Update clients if hosting
            if (_game is GameMaster gameMaster)
            {
                gameMaster.BroadcastGameRules();
            }

            // Navigate back to the Lobby view
            _mainWindow.ShowLobbyView(_game);
        }



        private void LoadRules_Click(object sender, RoutedEventArgs e)
        {
            // Define the user-specific GameRulesets directory
            string userRulesDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "GGOverlay",
                "GameRulesets"
            );

            // Define the application-specific DefaultRulesets directory
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string defaultRulesDirectory = Path.Combine(appDirectory, "DefaultRulesets");

            // Determine the initial directory for the OpenFileDialog
            string initialDirectory;

            // Use the user directory if it exists and contains files
            if (Directory.Exists(userRulesDirectory) && Directory.EnumerateFiles(userRulesDirectory, "*.json").Any())
            {
                initialDirectory = userRulesDirectory;
            }
            // Otherwise, use the application directory if it exists
            else if (Directory.Exists(defaultRulesDirectory))
            {
                initialDirectory = defaultRulesDirectory;
            }
            else
            {
                // Handle the case where neither directory exists
                MessageBox.Show("No rulesets were found in the default locations.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select Game Rules File",
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                InitialDirectory = initialDirectory
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _currentFileName = Path.GetFileName(openFileDialog.FileName);
                FileNameTextBlock.Text = _currentFileName;

                // Load rules from file into a temporary GameRules object
                var tempGameRules = new GameRules();
                tempGameRules.LoadFromFile(openFileDialog.FileName);

                // Update the current rules
                _currentRules = new ObservableCollection<Rule>(tempGameRules.Rules.Select(r => r.Clone()));
                RulesItemsControl.ItemsSource = _currentRules;

                // Update the original rules
                _originalRules = _currentRules.Select(r => r.Clone()).ToList();

                // Store the new SourceFilePath temporarily
                _currentSourceFilePath = openFileDialog.FileName;

                // Enable the Create New Rules button
                CreateNewRulesButton.IsEnabled = true;
            }
        }



        private void ToggleExampleSection_Click(object sender, RoutedEventArgs e)
        {
            if (ExampleSection.Visibility == Visibility.Visible)
            {
                ExampleSection.Visibility = Visibility.Collapsed;
            }
            else
            {
                ExampleSection.Visibility = Visibility.Visible;
            }
        }

        private void CreateNewRules_Click(object sender, RoutedEventArgs e)
        {
            // Check if there are unsaved changes
            if (IsRulesModified())
            {
                MessageBoxResult result = MessageBox.Show("Do you want to save changes to the existing rules?", "Save Changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    SaveButton_Click(sender, e);
                    // If user cancels the save dialog, we should check if they actually saved
                    if (IsRulesModified())
                    {
                        // User did not save, so return without clearing
                        return;
                    }
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    // Do nothing, return
                    return;
                }
            }

            // Reset to empty rules
            _currentRules.Clear();
            RulesItemsControl.ItemsSource = _currentRules;
            _originalRules.Clear();

            // Reset the current SourceFilePath
            _currentSourceFilePath = null;

            _currentFileName = "Unnamed Rule Set";
            FileNameTextBlock.Text = _currentFileName;

            // Disable the Create New Rules button since no file is loaded
            CreateNewRulesButton.IsEnabled = false;

            // Stay in EditRulesView with an empty rule set
            // No navigation is needed
        }


        private bool IsRulesModified()
        {
            // Compare _currentRules and _originalRules
            if (_currentRules.Count != _originalRules.Count)
                return true;

            for (int i = 0; i < _currentRules.Count; i++)
            {
                if (!_currentRules[i].Equals(_originalRules[i]))
                    return true;
            }

            return false;
        }
    }
}
