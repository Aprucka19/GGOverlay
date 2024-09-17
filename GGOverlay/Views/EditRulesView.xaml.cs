using GGOverlay.Game;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GGOverlay
{
    public partial class EditRulesView : UserControl
    {
        private MainWindow _mainWindow;
        private IGameInterface _game;

        private ObservableCollection<Rule> _currentRules;
        private List<Rule> _originalRules;

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
            _game._gameRules.Rules = _currentRules.ToList();

            // Save rules to file if loaded from a file or prompt to save
            if (!string.IsNullOrEmpty(_game._gameRules.SourceFilePath))
            {
                _game._gameRules.SaveToFile(_game._gameRules.SourceFilePath);
            }
            else
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Title = "Save Game Rules",
                    Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    _game._gameRules.SaveToFile(saveFileDialog.FileName);
                    _game._gameRules.SourceFilePath = saveFileDialog.FileName;
                }
                else
                {
                    MessageBox.Show("Rules not saved. Please save your rules.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            // Update clients if hosting
            if (_game is GameMaster gameMaster)
            {
                gameMaster.BroadcastGameRules();
            }

            _mainWindow.ShowLobbyView(_game);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Revert the rules to the original state
            _game._gameRules.Rules = _originalRules.Select(r => r.Clone()).ToList();

            _mainWindow.ShowLobbyView(_game);
        }

        private void LoadRules_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select Game Rules File",
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _game._gameRules.LoadFromFile(openFileDialog.FileName);
                _game._gameRules.SourceFilePath = openFileDialog.FileName;

                // Update the current rules
                _currentRules = new ObservableCollection<Rule>(_game._gameRules.Rules.Select(r => r.Clone()));
                RulesItemsControl.ItemsSource = _currentRules;

                // Update clients if hosting
                if (_game is GameMaster gameMaster)
                {
                    gameMaster.BroadcastGameRules();
                }
            }
        }
    }
}
