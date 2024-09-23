﻿using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using GGOverlay.Game;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Linq;
using System.Collections.Generic;
using Xceed.Wpf.Toolkit;
using MessageBox = System.Windows.MessageBox;

namespace GGOverlay
{
    public partial class OverlayWindow : Window
    {
        private IGameInterface _game;
        private bool isInteractive = false;

        // Constants for hotkey registration
        private const int HOTKEY_ID = 9000;
        private const uint MOD_CONTROL = 0x0002;
        private const uint VK_BACKTICK = 0xC0; // VK_OEM_3 for '`' key

        // Constants for extended window styles
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;


        // Font Scale Multiplier
        private double fontScaleMultiplier = 1.0;

        // Import user32.dll functions
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        // Variables for dragging
        private bool isDragging = false;
        private Point clickPosition;
        private Border draggedSection;

        // Variables for Sliders
        private bool isBackgroundSliderDragging = false;
        private bool isTextSliderDragging = false;
        private DispatcherTimer sliderTimer;

        // Current Colors and Font
        private Color currentBackgroundColor = Colors.Black;
        private Color currentTextColor = Colors.White;
        private string currentFont = "Segoe UI";

        // Variables for player boxes
        private List<Border> _playerBorders = new List<Border>();
        private int _colorIndex = 0;

        public OverlayWindow(IGameInterface game)
        {
            // Assign _game first to prevent null reference issues
            _game = game ?? throw new ArgumentNullException(nameof(game));

            InitializeComponent();

            // Set Window to cover the entire primary screen
            this.Width = SystemParameters.PrimaryScreenWidth;
            this.Height = SystemParameters.PrimaryScreenHeight;
            this.Left = 0;
            this.Top = 0;

            // Load game rules and lobby members
            LoadGameRules();
            LoadLobbyMembers();

            // Initialize the slider timer
            sliderTimer = new DispatcherTimer();
            sliderTimer.Interval = TimeSpan.FromMilliseconds(500);
            sliderTimer.Tick += SliderTimer_Tick;

            // Register the hotkey
            Loaded += OverlayWindow_Loaded;
            Closing += OverlayWindow_Closing;

            // Subscribe to game events
            _game.UIUpdate += OnGameUIUpdate;
            _game.OnDisconnect += CloseOverlay;

            // Subscribe to size change event to adjust font sizes
            this.SizeChanged += OverlayWindow_SizeChanged;

            // Set initial mode to interactive
            SetInteractiveMode();

            // Initialize ComboBoxes
            InitializeComboBoxes();

            // Load settings from UserData
            LoadUserDataSettings();
        }

        #region OverlayLayouts Folder Management

        private string GetOverlayLayoutsPath()
        {
            string overlayLayoutsPath = Path.Combine(
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

        #endregion

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

                // Set UnifiedBorder size
                UnifiedBorder.Width = userData.OverlaySettings.WindowWidth;
                UnifiedBorder.Height = userData.OverlaySettings.WindowHeight;

                // Set UnifiedBorder position correctly using WindowTop
                Canvas.SetLeft(UnifiedBorder, userData.OverlaySettings.WindowLeft);
                Canvas.SetTop(UnifiedBorder, userData.OverlaySettings.WindowTop); // Fixed line
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

            // Set UnifiedBorder size
            userData.OverlaySettings.WindowWidth = UnifiedBorder.Width;
            userData.OverlaySettings.WindowHeight = UnifiedBorder.Height;

            // Set UnifiedBorder position
            userData.OverlaySettings.WindowLeft = Canvas.GetLeft(UnifiedBorder);
            userData.OverlaySettings.WindowTop = Canvas.GetTop(UnifiedBorder);

            // Save to file
            userData.Save();
        }

        private void OverlayWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var helper = new WindowInteropHelper(this);
            var hwnd = helper.Handle;

            HwndSource source = HwndSource.FromHwnd(hwnd);
            source.AddHook(HwndHook);

            bool isRegistered = RegisterHotKey(hwnd, HOTKEY_ID, MOD_CONTROL, VK_BACKTICK);
            if (!isRegistered)
            {
                Xceed.Wpf.Toolkit.MessageBox.Show("Failed to register hotkey Ctrl + ` for overlay toggle. Please ensure it's not already in use.", "Hotkey Registration Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;

            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (id == HOTKEY_ID)
                {
                    // Toggle mode
                    ToggleMode();
                    handled = true;
                }
            }

            return IntPtr.Zero;
        }

        private void ToggleMode()
        {
            if (isInteractive)
            {
                SetBackgroundMode();
                InteractiveControlsBackground.Visibility = Visibility.Collapsed;
            }
            else
            {
                SetInteractiveMode();
                InteractiveControlsBackground.Visibility = Visibility.Visible;
            }

            // Update game rules' IsHitTestVisible
            foreach (var child in GameRulesPanel.Children)
            {
                if (child is Border ruleBorder)
                {
                    ruleBorder.IsHitTestVisible = isInteractive;
                }
            }
        }

        private void SetInteractiveMode()
        {
            // Make the window interactive
            this.IsHitTestVisible = true;
            this.Focusable = true;
            this.Topmost = true;
            isInteractive = true;

            var hwnd = new WindowInteropHelper(this).Handle;
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle & ~WS_EX_TRANSPARENT);

            // Show interactive controls
            InteractiveControlsBackground.Visibility = Visibility.Visible;

            // Bring the window to the front and focus
            this.Activate();
        }

        private void SetBackgroundMode()
        {
            // Make the window non-interactive
            this.IsHitTestVisible = false;
            this.Focusable = false;
            this.Topmost = true;
            isInteractive = false;

            var hwnd = new WindowInteropHelper(this).Handle;
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TRANSPARENT);

            // Hide interactive controls
            InteractiveControlsBackground.Visibility = Visibility.Collapsed;
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
                    // Create a Border for the rule
                    Border ruleBorder = new Border
                    {
                        BorderThickness = new Thickness(1),
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
                        FontFamily = new FontFamily(currentFont)
                    };

                    ruleBorder.Child = ruleText;

                    GameRulesPanel.Children.Add(ruleBorder);

                    alternate = !alternate;
                }
            }
        }


        // Define a consistent color list for player boxes
        private readonly List<Color> _playerColors = new List<Color>
        {
            Color.FromRgb(139, 0, 0),       // Dark Red
            Color.FromRgb(0, 100, 0),       // Dark Green
            Color.FromRgb(0, 0, 139),       // Dark Blue
            Color.FromRgb(184, 134, 11),    // Dark Goldenrod (Dark Yellow)
            Color.FromRgb(75, 0, 130),      // Indigo (Dark Purple)
            Color.FromRgb(255, 140, 0),     // Dark Orange
            Color.FromRgb(0, 139, 139),     // Dark Cyan
            Color.FromRgb(139, 0, 139)      // Dark Magenta
        };


        private void LoadLobbyMembers()
        {
            // Load lobby members into LobbyMembersPanel
            if (_game != null && _game._players != null)
            {
                LobbyMembersPanel.Children.Clear();
                _playerBorders.Clear();

                double backgroundOpacity = BackgroundOpacitySlider?.Value ?? 1.0;

                // Create a list to hold players, with local player first if exists
                List<PlayerInfo> players = new List<PlayerInfo>();

                // Assume _game has a LocalPlayer property. Adjust accordingly based on your IGameInterface implementation.
                PlayerInfo localPlayer = _game._localPlayer; // Replace with the actual property/method to get the local player
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
                    Color colorWithOpacity = Color.FromArgb((byte)(backgroundOpacity * 255), playerColor.R, playerColor.G, playerColor.B);

                    // Create a Border for the player without setting margin here
                    Border playerBorder = new Border
                    {
                        BorderThickness = new Thickness(0), // Remove border
                        CornerRadius = new CornerRadius(5),
                        // Margin will be set in AdjustPlayerBoxesWidth
                        Padding = new Thickness(5),
                        Background = new SolidColorBrush(colorWithOpacity)
                        // Width is managed by WrapPanel's ItemWidth
                    };

                    // Create a StackPanel inside the Border
                    StackPanel playerStack = new StackPanel
                    {
                        Orientation = Orientation.Vertical,
                        HorizontalAlignment = HorizontalAlignment.Center, // Center horizontally
                        VerticalAlignment = VerticalAlignment.Center    // Center vertically
                    };

                    // Player Name
                    TextBlock nameText = new TextBlock
                    {
                        Text = player.Name,
                        Foreground = new SolidColorBrush(currentTextColor),
                        FontSize = 14 * fontScaleMultiplier, // Apply font scale
                        FontFamily = new FontFamily(currentFont),
                        Margin = new Thickness(0, 0, 0, 2),
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Center // Center text horizontally
                    };

                    // Drink Modifier as Fraction
                    TextBlock drinkText = new TextBlock
                    {
                        Text = player.ReturnFraction(), // Display as fraction
                        Foreground = new SolidColorBrush(currentTextColor),
                        FontSize = 12 * fontScaleMultiplier, // Apply font scale
                        FontFamily = new FontFamily(currentFont),
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Center // Center text horizontally
                    };

                    playerStack.Children.Add(nameText);
                    playerStack.Children.Add(drinkText);

                    playerBorder.Child = playerStack;

                    LobbyMembersPanel.Children.Add(playerBorder);
                    _playerBorders.Add(playerBorder);
                }

                AdjustPlayerBoxesWidth(); // Adjust player boxes' width and margins after loading
            }
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

        #region Dragging Logic

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

        // This event handler will adjust the font size and player boxes' width every time the window size changes
        private void OverlayWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            AdjustFontSizes(UnifiedBorder);
            AdjustPlayerBoxesWidth(); // Adjust player boxes' width on window size change
        }

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
            // AdjustPlayerBoxesWidth(); // Not necessary here
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

                // Animate the font size change for a smooth transition
                DoubleAnimation fontSizeAnimation = new DoubleAnimation
                {
                    To = newFontSize,
                    Duration = TimeSpan.FromMilliseconds(200)
                };
                textBlock.BeginAnimation(TextBlock.FontSizeProperty, fontSizeAnimation);
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

        #region Slider Logic

        private void BackgroundOpacitySlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isBackgroundSliderDragging = true;
            sliderTimer.Stop(); // Stop the timer while dragging
        }

        private void BackgroundOpacitySlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isBackgroundSliderDragging = false;
            sliderTimer.Start(); // Start the timer when dragging stops
            // SaveUserDataSettings(); // Removed from here
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
            // SaveUserDataSettings(); // Removed from here
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
            // Modify the background color's alpha channel based on opacity
            var currentBrush = UnifiedBorder.Background as SolidColorBrush;
            if (currentBrush != null)
            {
                Color color = currentBrush.Color;
                color.A = (byte)(opacity * 255);
                currentBrush.Color = color;
            }

            // Update player boxes' background opacity
            foreach (var border in _playerBorders)
            {
                if (border.Background is SolidColorBrush playerBrush)
                {
                    Color color = playerBrush.Color;
                    color.A = (byte)(opacity * 255);
                    playerBrush.Color = color;
                }
            }
        }

        private void SetTextOpacity(double opacity)
        {
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
        }

        #endregion

        #region ColorPicker Logic

        private void SetBackgroundColor(Color color)
        {
            currentBackgroundColor = color;
            var brush = new SolidColorBrush(color);
            UnifiedBorder.Background = brush;

            // Update player boxes' background colors to maintain their distinct colors with new opacity
            double opacity = BackgroundOpacitySlider?.Value ?? 1.0;
            foreach (var border in _playerBorders)
            {
                if (border.Background is SolidColorBrush playerBrush)
                {
                    Color baseColor = playerBrush.Color;
                    // Assuming baseColor already has the intended color, just update alpha
                    playerBrush.Color = Color.FromArgb((byte)(opacity * 255), baseColor.R, baseColor.G, baseColor.B);
                }
            }
        }

        private void SetTextColor(Color color)
        {
            currentTextColor = color;
            foreach (var textBlock in FindVisualChildren<TextBlock>(UnifiedBorder))
            {
                textBlock.Foreground = new SolidColorBrush(color);
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



            var fonts = new List<string> { "Segoe UI", "Arial", "Calibri", "Verdana", "Times New Roman" };
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
                WindowTop = Canvas.GetTop(UnifiedBorder)
                // Add other settings as needed
            };
        }

        private void AdjustPlayerBoxesWidth()
        {
            if (LobbyMembersPanel == null)
                return;

            int playerCount = _game._players.Count;

            if (playerCount == 0)
                return;

            // Define a maximum number of columns (2 for ~40% each)
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

            // Apply UnifiedBorder size
            UnifiedBorder.Width = settings.WindowWidth;
            UnifiedBorder.Height = settings.WindowHeight;

            // Apply UnifiedBorder position
            Canvas.SetLeft(UnifiedBorder, settings.WindowLeft);
            Canvas.SetTop(UnifiedBorder, settings.WindowTop);

            AdjustFontSizes(UnifiedBorder);
            AdjustPlayerBoxesWidth(); // Adjust player boxes' width after applying settings
        }

        #endregion
    }
}
