// OverlayWindow.UIHandlers.cs
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Windows.Media.Animation;
using System.IO;
using System.Linq;
using MessageBox = System.Windows.MessageBox;
using System.Windows.Interop;

namespace GGOverlay
{
    public partial class OverlayWindow
    {
        #region Dragging Logic

        // Variables for dragging
        private bool isDragging = false;
        private Point clickPosition;
        private Border draggedSection;

        private void Section_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!isInteractive)
                return;

            draggedSection = sender as Border;
            if (draggedSection != null)
            {
                isDragging = true;
                clickPosition = e.GetPosition(MainCanvas);
                draggedSection.CaptureMouse();
            }
        }

        // Variables for dragging the controls panel
        private bool isControlsDragging = false;
        private Point controlsClickPosition;
        private Border draggedControls;

        private void InteractiveControlsBackground_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!isInteractive)
                return;

            draggedControls = sender as Border;
            if (draggedControls != null)
            {
                isControlsDragging = true;
                controlsClickPosition = e.GetPosition(MainCanvas);
                draggedControls.CaptureMouse();
            }
        }

        private void InteractiveControlsBackground_MouseMove(object sender, MouseEventArgs e)
        {
            if (isControlsDragging && draggedControls != null)
            {
                Point currentPosition = e.GetPosition(MainCanvas);
                double offsetX = currentPosition.X - controlsClickPosition.X;
                double offsetY = currentPosition.Y - controlsClickPosition.Y;

                double newLeft = Canvas.GetLeft(draggedControls) + offsetX;
                double newTop = Canvas.GetTop(draggedControls) + offsetY;

                // Ensure the controls panel stays within the window bounds
                newLeft = Math.Max(0, Math.Min(newLeft, MainCanvas.ActualWidth - draggedControls.ActualWidth));
                newTop = Math.Max(0, Math.Min(newTop, MainCanvas.ActualHeight - draggedControls.ActualHeight));

                Canvas.SetLeft(draggedControls, newLeft);
                Canvas.SetTop(draggedControls, newTop);

                controlsClickPosition = currentPosition;
            }
        }

        private void InteractiveControlsBackground_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isControlsDragging && draggedControls != null)
            {
                isControlsDragging = false;
                draggedControls.ReleaseMouseCapture();
                draggedControls = null;
            }
        }

        private void Section_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && draggedSection != null)
            {
                Point currentPosition = e.GetPosition(MainCanvas);
                double offsetX = currentPosition.X - clickPosition.X;
                double offsetY = currentPosition.Y - clickPosition.Y;

                double newLeft = Canvas.GetLeft(draggedSection) + offsetX;
                double newTop = Canvas.GetTop(draggedSection) + offsetY;

                // Ensure the section stays within the window bounds
                newLeft = Math.Max(0, Math.Min(newLeft, MainCanvas.ActualWidth - draggedSection.Width));
                newTop = Math.Max(0, Math.Min(newTop, MainCanvas.ActualHeight - draggedSection.Height));

                Canvas.SetLeft(draggedSection, newLeft);
                Canvas.SetTop(draggedSection, newTop);

                clickPosition = currentPosition;
            }
        }

        private void Section_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isDragging && draggedSection != null)
            {
                isDragging = false;
                draggedSection.ReleaseMouseCapture();
                draggedSection = null;

                // Save the new position to UserData only when closing
                // Removed SaveUserDataSettings() from here
            }
        }

        #endregion

        #region Resizing Logic

        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (!isInteractive)
                return;

            Thumb thumb = sender as Thumb;
            if (thumb == null)
                return;

            double newWidth = UnifiedBorder.Width + e.HorizontalChange;
            double newHeight = UnifiedBorder.Height + e.VerticalChange;

            // Set minimum and maximum sizes
            newWidth = Math.Max(100, newWidth); // Minimum width
            newHeight = Math.Max(100, newHeight); // Minimum height

            UnifiedBorder.Width = newWidth;
            UnifiedBorder.Height = newHeight;

            // Adjust font sizes and wrapping accordingly
            AdjustFontSizes(UnifiedBorder);
            AdjustPlayerBoxesWidth(); // Adjust player boxes' width after resizing

            // Save the new size to UserData only when closing
            // Removed SaveUserDataSettings() from here
        }

        // Event handler for when the Font Scale slider value changes
        private void FontScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!isInteractive)
                return;

            // Update the font scale multiplier based on the slider's value
            fontScaleMultiplier = e.NewValue;
            AdjustFontSizes(UnifiedBorder);
        }

        private void AdjustFontSizes(Border border)
        {
            double baseWidth = 300;  // Original width
            double baseHeight = 400; // Original height

            double currentWidth = border.Width;
            double currentHeight = border.Height;

            double widthRatio = currentWidth / baseWidth;
            double heightRatio = currentHeight / baseHeight;

            // Calculate the average scaling factor and apply the font scale multiplier
            double scale = ((widthRatio + heightRatio) / 2) * fontScaleMultiplier;

            // Clamp the scale to prevent excessive font sizes
            scale = Math.Min(scale, 8.0); // Maximum 8x scaling

            foreach (var textBlock in FindVisualChildren<TextBlock>(border))
            {
                double newFontSize = 14 * scale;  // Base font size multiplied by scale
                newFontSize = Math.Max(2, Math.Min(newFontSize, 300)); // Clamp between 2 and 300

                // Set the font size directly without animation
                textBlock.FontSize = newFontSize;
            }

            double buttonSize = 30 * scale; // Base size multiplied by scale
            buttonSize = Math.Max(16, Math.Min(buttonSize, 100)); // Clamp between 16 and 100

            SettingsButton.Width = buttonSize;
            SettingsButton.Height = buttonSize;
            SettingsButton.FontSize = buttonSize * 0.5; // Adjust font size accordingly

            CloseOverlayButton.Width = buttonSize;
            CloseOverlayButton.Height = buttonSize;
            CloseOverlayButton.FontSize = buttonSize * 0.5;

            ConfirmButton.Width = buttonSize;
            ConfirmButton.Height = buttonSize;
            ConfirmButton.FontSize = buttonSize * 0.5; // Adjust font size accordingly

            CancelButton.Width = buttonSize;
            CancelButton.Height = buttonSize;
            CancelButton.FontSize = buttonSize * 0.5;

            FinishDrinkButton.Width = buttonSize;
            FinishDrinkButton.Height = buttonSize;
            FinishDrinkButton.FontSize = buttonSize * 0.5;

            if (FinishDrinkButton.Content is TextBlock finishDrinkTextBlock)
            {
                finishDrinkTextBlock.FontSize = buttonSize * 0.5;
            }

            // Adjust font size of TimerTextBlock
            if (TimerTextBlock != null)
            {
                double newFontSize = 14 * scale;
                newFontSize = Math.Max(2, Math.Min(newFontSize, 300));
                TimerTextBlock.FontSize = newFontSize;
            }
        }

        private IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        #endregion

        #region Opacity Logic

        // Added current opacity variables
        private double currentBackgroundOpacity = 1.0;
        private double currentTextOpacity = 1.0;

        private void BackgroundOpacitySlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isBackgroundSliderDragging = true;
            sliderTimer.Stop(); // Stop the timer while dragging
        }

        private void BackgroundOpacitySlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isBackgroundSliderDragging = false;
            sliderTimer.Start(); // Start the timer when dragging stops
            SaveUserDataSettings();
        }

        private void TextOpacitySlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isTextSliderDragging = true;
            sliderTimer.Stop(); // Stop the timer while dragging
        }

        private void TextOpacitySlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isTextSliderDragging = false;
            sliderTimer.Start(); // Start the timer when dragging stops
            SaveUserDataSettings();
        }

        private void BackgroundOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!isInteractive)
                return;

            if (isBackgroundSliderDragging)
            {
                // Adjust the background opacity according to the slider value
                SetBackgroundOpacity(e.NewValue);
            }
        }

        private void TextOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!isInteractive)
                return;

            if (isTextSliderDragging)
            {
                // Adjust the text opacity according to the slider value
                SetTextOpacity(e.NewValue);
            }
        }

        private void SliderTimer_Tick(object sender, EventArgs e)
        {
            if (!isBackgroundSliderDragging && !isTextSliderDragging)
            {
                // Ensure opacity is set based on the slider value
                ApplyTextOpacity();

                sliderTimer.Stop();
            }
        }

        private void SetBackgroundOpacity(double opacity)
        {
            currentBackgroundOpacity = opacity;

            // Modify the background color's alpha channel based on opacity
            var currentBrush = UnifiedBorder.Background as SolidColorBrush;
            if (currentBrush != null)
            {
                Color color = currentBrush.Color;
                color.A = (byte)(opacity * 255);
                currentBrush.Color = color;
            }

            // Update punishment displays' background opacity
            foreach (var child in PunishmentDisplayStackPanel.Children)
            {
                if (child is Border punishmentBorder)
                {
                    var brush = punishmentBorder.Background as SolidColorBrush;
                    if (brush != null)
                    {
                        Color color = brush.Color;
                        color.A = (byte)(opacity * 255);
                        brush.Color = color;
                    }
                }
            }
        }

        private void SetTextOpacity(double opacity)
        {
            currentTextOpacity = opacity;

            // Iterate through all TextBlocks and set their opacity
            foreach (var textBlock in FindVisualChildren<TextBlock>(UnifiedBorder))
            {
                textBlock.Opacity = opacity;
            }
        }

        private void ApplyTextOpacity()
        {
            double opacity = TextOpacitySlider.Value;
            foreach (var textBlock in FindVisualChildren<TextBlock>(UnifiedBorder))
            {
                textBlock.Opacity = opacity;
            }
            if (TimerTextBlock != null)
            {
                TimerTextBlock.Opacity = opacity;
            }
        }

        #endregion

        #region ColorPicker Logic

        private void SetBackgroundColor(Color color)
        {
            currentBackgroundColor = color;
            var brush = new SolidColorBrush(color);
            UnifiedBorder.Background = brush;

            // Removed updating player boxes' background colors to keep them at 100% opacity
        }

        private void SetTextColor(Color color)
        {
            currentTextColor = color;
            foreach (var textBlock in FindVisualChildren<TextBlock>(UnifiedBorder))
            {
                textBlock.Foreground = new SolidColorBrush(color);
            }

            // Update TimerTextBlock's color
            if (TimerTextBlock != null)
            {
                TimerTextBlock.Foreground = new SolidColorBrush(color);
            }
        }

        private void BackgroundColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (e.NewValue.HasValue && isInteractive)
            {
                SetBackgroundColor(e.NewValue.Value);
                // SaveUserDataSettings(); // Removed from here
            }
        }

        private void TextColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (e.NewValue.HasValue && isInteractive)
            {
                SetTextColor(e.NewValue.Value);
                // SaveUserDataSettings(); // Removed from here
            }
        }

        #endregion

        #region ComboBox Logic

        private void InitializeComboBoxes()
        {
            // Initialize FontComboBox with fonts displayed in their own font type
            FontComboBox.Items.Clear();

            var fonts = new List<string>
            {
                // Common system fonts
                "Segoe UI",
                "Arial",
                "Calibri",
                "Verdana",
                "Times New Roman",
                "Georgia",
                "Trebuchet MS",
                "Comic Sans MS",
                "Courier New",
                "Lucida Console",
                "Palatino Linotype",
                "Tahoma",
                "Gill Sans MT",
                "Century Gothic",
                "Franklin Gothic Medium",
                "Impact",
                "Futura",
                "Garamond",
                "Cambria",
                "Rockwell",

                // Cursive and script-like fonts available on Windows 11
                "Brush Script MT",
                "Segoe Script",
                "Lucida Handwriting",
                "Segoe Print",
                "Kristen ITC"
            };

            foreach (var font in fonts)
            {
                ComboBoxItem item = new ComboBoxItem
                {
                    Content = font,
                    FontFamily = new FontFamily(font)
                };
                if (font == "Segoe UI")
                {
                    item.IsSelected = true;
                }
                FontComboBox.Items.Add(item);
            }

            FontScaleSlider.Value = 1.0;
        }

        private void FontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = FontComboBox.SelectedItem as ComboBoxItem;
            if (selected != null)
            {
                string fontName = selected.Content.ToString();
                SetFontFamily(fontName);
                // SaveUserDataSettings(); // Removed from here
            }
        }

        private void SetFontFamily(string fontName)
        {
            currentFont = fontName;
            foreach (var textBlock in FindVisualChildren<TextBlock>(UnifiedBorder))
            {
                textBlock.FontFamily = new FontFamily(fontName);
            }

            // Update TimerTextBlock's font family
            if (TimerTextBlock != null)
            {
                TimerTextBlock.FontFamily = new FontFamily(fontName);
            }
        }

        #endregion

        #region Reset Settings Logic

        private void ResetSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            ResetSettings();
        }

        private void ResetSettings()
        {
            if (!isInteractive)
                return;

            var result = MessageBox.Show("Are you sure you want to reset all settings to default?", "Confirm Reset", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;

            // Reset Sliders
            BackgroundOpacitySlider.Value = 1.0;
            TextOpacitySlider.Value = 1.0;
            FontScaleSlider.Value = 1.0;

            // Reset ColorPickers
            BackgroundColorPicker.SelectedColor = Colors.Black;
            TextColorPicker.SelectedColor = Colors.White;

            // Reset Font ComboBox
            FontComboBox.SelectedIndex = 0; // "Segoe UI"

            // Reset UnifiedBorder's size and position
            UnifiedBorder.Width = 300;
            UnifiedBorder.Height = 400;
            Canvas.SetLeft(UnifiedBorder, 50);
            Canvas.SetTop(UnifiedBorder, 50);

            // Optionally, reset current variables
            fontScaleMultiplier = 1.0;
            currentBackgroundColor = Colors.Black;
            currentTextColor = Colors.White;
            currentFont = "Segoe UI";

            // Save the reset settings
            SaveUserDataSettings();

            // Reload lobby members to apply new settings
            LoadLobbyMembers();
        }

        #endregion

        #region Close Overlay Logic

        private void OverlayWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveUserDataSettings();
            CloseOverlay();
        }

        private void CloseOverlayButton_Click(object sender, RoutedEventArgs e)
        {
            CloseOverlay();
        }

        private void CloseOverlay()
        {
            // Set to background mode
            SetBackgroundMode();

            // Hide the overlay window
            this.Hide();

            // Unregister the hotkey when the overlay is closed
            var helper = new WindowInteropHelper(this);
            UnregisterHotKey(helper.Handle, HOTKEY_ID);

            // Save settings
            SaveUserDataSettings();

            // Bring Lobby window back into focus
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null)
            {
                mainWindow.WindowState = System.Windows.WindowState.Normal; // Restore if minimized
                mainWindow.Activate();
            }
            else
            {
                // If mainWindow is not set correctly, consider alternative methods to bring the lobby window to focus
                // For example, if you have a reference to the lobby window, use that instead
            }
        }

        #endregion

        #region Load and Save Settings Logic

        private string GetOverlayLayoutsPath()
        {
            string overlayLayoutsPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "GGOverlay",
                "OverlayLayouts"
            );

            if (!Directory.Exists(overlayLayoutsPath))
            {
                Directory.CreateDirectory(overlayLayoutsPath);
            }

            return overlayLayoutsPath;
        }

        private void LoadSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            LoadSettings();
        }

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
        }

        private void LoadSettings()
        {
            try
            {
                string overlayLayoutsPath = GetOverlayLayoutsPath();

                // Configure OpenFileDialog
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    InitialDirectory = overlayLayoutsPath,
                    Filter = "JSON Files (*.json)|*.json",
                    Title = "Select Overlay Settings"
                };

                bool? result = openFileDialog.ShowDialog();

                if (result == true)
                {
                    string selectedFile = openFileDialog.FileName;
                    OverlaySettings loadedSettings = JsonConvert.DeserializeObject<OverlaySettings>(File.ReadAllText(selectedFile));

                    if (loadedSettings != null)
                    {
                        ApplyOverlaySettings(loadedSettings);
                        Xceed.Wpf.Toolkit.MessageBox.Show("Settings loaded successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        Xceed.Wpf.Toolkit.MessageBox.Show("Failed to deserialize the selected settings file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                Xceed.Wpf.Toolkit.MessageBox.Show($"An error occurred while loading settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveSettings()
        {
            try
            {
                string overlayLayoutsPath = GetOverlayLayoutsPath();

                // Configure SaveFileDialog
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    InitialDirectory = overlayLayoutsPath,
                    Filter = "JSON Files (*.json)|*.json",
                    Title = "Save Overlay Settings",
                    AddExtension = true,
                    DefaultExt = "json",
                    FileName = "NewOverlaySettings.json"
                };

                bool? result = saveFileDialog.ShowDialog();

                if (result == true)
                {
                    string selectedFile = saveFileDialog.FileName;
                    OverlaySettings currentSettings = GetCurrentOverlaySettings();

                    string json = JsonConvert.SerializeObject(currentSettings, Formatting.Indented);
                    File.WriteAllText(selectedFile, json);

                    Xceed.Wpf.Toolkit.MessageBox.Show("Settings saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Xceed.Wpf.Toolkit.MessageBox.Show($"An error occurred while saving settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private OverlaySettings GetCurrentOverlaySettings()
        {
            return new OverlaySettings
            {
                FontColor = currentTextColor.ToString(),
                FontScaleMultiplier = fontScaleMultiplier,
                BackgroundColor = currentBackgroundColor.ToString(),
                WindowWidth = UnifiedBorder.Width,
                WindowHeight = UnifiedBorder.Height,
                WindowLeft = Canvas.GetLeft(UnifiedBorder),
                WindowTop = Canvas.GetTop(UnifiedBorder),
                TextOpacity = TextOpacitySlider.Value,
                BackgroundOpacity = BackgroundOpacitySlider.Value
                // Add other settings as needed
            };
        }

        private void ApplyOverlaySettings(OverlaySettings settings)
        {
            // Apply font color
            Color fontColor = (Color)ColorConverter.ConvertFromString(settings.FontColor);
            SetTextColor(fontColor);

            // Apply font scale multiplier
            fontScaleMultiplier = settings.FontScaleMultiplier;
            FontScaleSlider.Value = fontScaleMultiplier;

            // Apply background color
            Color backgroundColor = (Color)ColorConverter.ConvertFromString(settings.BackgroundColor);
            SetBackgroundColor(backgroundColor);

            // Apply Text Opacity
            TextOpacitySlider.Value = settings.TextOpacity;
            SetTextOpacity(settings.TextOpacity);

            // Apply Background Opacity
            BackgroundOpacitySlider.Value = settings.BackgroundOpacity;
            SetBackgroundOpacity(settings.BackgroundOpacity);

            // Apply UnifiedBorder size
            UnifiedBorder.Width = settings.WindowWidth;
            UnifiedBorder.Height = settings.WindowHeight;

            // Apply UnifiedBorder position
            Canvas.SetLeft(UnifiedBorder, settings.WindowLeft);
            Canvas.SetTop(UnifiedBorder, settings.WindowTop);

            AdjustFontSizes(UnifiedBorder);
            AdjustPlayerBoxesWidth(); // Adjust player boxes' width after applying settings
        }

        private void AdjustPlayerBoxesWidth()
        {
            if (LobbyMembersPanel == null)
                return;

            int playerCount = _game._players.Count;

            if (playerCount == 0)
                return;

            // Define a maximum number of columns (2 for ~50% each)
            int maxColumns = 2;

            // Determine the actual number of columns based on the number of players
            int columns = Math.Min(playerCount, maxColumns);

            // Calculate boxMargin based on UnifiedBorder width
            double boxMargin = UnifiedBorder.Width * 0.025; // 2.5% left and right

            // Calculate the width for each player box as ~45% of UnifiedBorder width
            double itemWidth = (UnifiedBorder.Width * 0.50) - 2 * boxMargin; // 50% - 2 * 2.5% = 45% of width

            // Ensure the itemWidth is not negative
            itemWidth = Math.Max(itemWidth, 50); // Minimum width of 50 pixels

            // Set the ItemWidth of the WrapPanel
            LobbyMembersPanel.ItemWidth = itemWidth;

            // Update the margins of each playerBorder
            foreach (var border in _playerBorders)
            {
                // Preserve the top and bottom margins
                border.Margin = new Thickness(boxMargin, boxMargin, boxMargin, boxMargin);
            }
        }

        #endregion

        #region Settings Button Logic

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (InteractiveControlsBackground.Visibility == Visibility.Visible)
            {
                InteractiveControlsBackground.Visibility = Visibility.Collapsed;
            }
            else
            {
                InteractiveControlsBackground.Visibility = Visibility.Visible;

                // Set initial position if needed
                if (double.IsNaN(Canvas.GetLeft(InteractiveControlsBackground)) && double.IsNaN(Canvas.GetTop(InteractiveControlsBackground)))
                {
                    // Position it near the settings button
                    var position = SettingsButton.TranslatePoint(new Point(0, 0), MainCanvas);
                    Canvas.SetLeft(InteractiveControlsBackground, position.X);
                    Canvas.SetTop(InteractiveControlsBackground, position.Y + SettingsButton.ActualHeight + 5);
                }
            }
        }



        #endregion

        // Rest of your code...
    }
}
