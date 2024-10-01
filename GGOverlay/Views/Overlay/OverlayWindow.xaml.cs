// OverlayWindow.xaml.cs
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using GGOverlay.Game;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.Generic;
using System.Windows.Media.Animation;

namespace GGOverlay
{
    public partial class OverlayWindow : Window, INotifyPropertyChanged
    {
        private IGameInterface _game;
        private bool isInteractive = false;

        // Constants for hotkey registration
        private const int HOTKEY_ID = 9000;
        private const uint MOD_SHIFT = 0x0004;
        private const uint VK_BACKTICK = 0xC0; // VK_OEM_3 for '`' key


        // Timer for auto-hiding the punishment display
        private DispatcherTimer punishmentTimer;

        // Font Scale Multiplier
        private double fontScaleMultiplier = 1.0;

        // Import user32.dll functions
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

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

        // Variables for rule and player selection
        private Rule selectedRule;
        private Border selectedRuleBorder;
        private PlayerInfo selectedPlayer;
        private Border selectedPlayerBorder;

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

        public OverlayWindow(IGameInterface game)
        {
            InitializeComponent(); // Ensure this is called

            // Assign _game first to prevent null reference issues
            _game = game ?? throw new ArgumentNullException(nameof(game));

            // Set Window to cover the entire primary screen
            this.Width = SystemParameters.PrimaryScreenWidth;
            this.Height = SystemParameters.PrimaryScreenHeight;
            this.Left = 0;
            this.Top = 0;

            // Load settings from UserData
            LoadUserDataSettings();

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

            // Subscribe to the OnPunishmentTriggered event
            _game.OnGroupPunishmentTriggered += HandleGroupPunishmentTriggered;
            _game.OnIndividualPunishmentTriggered += HandleIndividualPunishmentTriggered;

            // Subscribe to size change event to adjust font sizes
            this.SizeChanged += OverlayWindow_SizeChanged;

            // Initialize ComboBoxes
            InitializeComboBoxes();


            // Initialize TimerTextBlock properties if needed
            TimerTextBlock.FontSize = 14 * fontScaleMultiplier;
            TimerTextBlock.Foreground = new SolidColorBrush(currentTextColor);
            TimerTextBlock.FontFamily = new FontFamily(currentFont);
            TimerTextBlock.Opacity = currentTextOpacity;

            // Set interactive mode
            SetInteractiveMode();



            // Update the timer display with the initial value
            UpdateTimerDisplay();
        }


        private void UpdateTimerDisplay()
        {
            // Convert _game._elapsedMinutes to H:MM format
            int totalMinutes = (int)Math.Round(_game._elapsedMinutes);
            int hours = totalMinutes / 60;
            int minutes = totalMinutes % 60;
            TimerTextBlock.Text = $"{hours}:{minutes:D2}";
        }

        private void OverlayWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var helper = new WindowInteropHelper(this);
            var hwnd = helper.Handle;

            HwndSource source = HwndSource.FromHwnd(hwnd);
            source.AddHook(HwndHook);

            bool isRegistered = RegisterHotKey(hwnd, HOTKEY_ID, MOD_SHIFT, VK_BACKTICK);
            if (!isRegistered)
            {
                Xceed.Wpf.Toolkit.MessageBox.Show("Failed to register hotkey Ctrl + ` for overlay toggle. Please ensure it's not already in use.", "Hotkey Registration Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Set the window to interactive mode after loading all UI elements
            SetInteractiveMode();
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
                UnifiedResizeThumb.Visibility = Visibility.Collapsed; // Hide the ResizeThumb
            }
            else
            {
                SetInteractiveMode();
                UnifiedResizeThumb.Visibility = Visibility.Visible; // Show the ResizeThumb
            }

            // Update game rules' IsHitTestVisible
            foreach (var child in GameRulesPanel.Children)
            {
                if (child is Border ruleBorder)
                {
                    ruleBorder.IsHitTestVisible = isInteractive;
                }
            }

            // Update player borders' IsHitTestVisible
            foreach (var border in _playerBorders)
            {
                border.IsHitTestVisible = isInteractive;
            }

            // Update punishment displays' IsHitTestVisible
            foreach (var child in PunishmentDisplayStackPanel.Children)
            {
                if (child is Border punishmentBorder)
                {
                    punishmentBorder.IsHitTestVisible = isInteractive;
                }
            }

            // Clear selections and hide buttons when switching modes
            if (!isInteractive)
            {
                DeselectAll();
                ConfirmButton.Visibility = Visibility.Collapsed;
                CancelButton.Visibility = Visibility.Collapsed;
            }
        }

        private void SetInteractiveMode()
        {
            // Make the window interactive
            isInteractive = true;

            foreach (var child in GameRulesPanel.Children)
            {
                if (child is Border ruleBorder)
                {
                    ruleBorder.IsHitTestVisible = isInteractive;
                }
            }

            // Update player borders' IsHitTestVisible
            foreach (var border in _playerBorders)
            {
                border.IsHitTestVisible = isInteractive;
            }

            // Update punishment displays' IsHitTestVisible
            foreach (var child in PunishmentDisplayStackPanel.Children)
            {
                if (child is Border punishmentBorder)
                {
                    punishmentBorder.IsHitTestVisible = isInteractive;
                }
            }

            if (TimerTextBlock != null)
            {
                TimerTextBlock.Visibility = Visibility.Visible;
            }

            // Update IsHitTestVisible on main elements
            MainCanvas.IsHitTestVisible = true;

            // Bring the window to the front and focus
            this.Topmost = true;
            this.Focusable = true;
            this.Activate();

            // Show Settings button
            SettingsButton.Visibility = Visibility.Visible;
            CloseOverlayButton.Visibility = Visibility.Visible;
            FinishDrinkButton.Visibility = Visibility.Visible;

            // Hide controls box initially
            InteractiveControlsBackground.Visibility = Visibility.Collapsed;
        }

        private void SetBackgroundMode()
        {
            // Make the window non-interactive
            isInteractive = false;

            // Update IsHitTestVisible on main elements
            MainCanvas.IsHitTestVisible = false;

            // Keep punishment popups interactive
            foreach (var child in PunishmentDisplayStackPanel.Children)
            {
                if (child is Border punishmentBorder)
                {
                    punishmentBorder.IsHitTestVisible = true;
                }
            }

            if (TimerTextBlock != null)
            {
                TimerTextBlock.Visibility = Visibility.Collapsed;
            }

            // Hide interactive controls and settings button
            InteractiveControlsBackground.Visibility = Visibility.Collapsed;
            SettingsButton.Visibility = Visibility.Collapsed;
            CloseOverlayButton.Visibility = Visibility.Collapsed;
            FinishDrinkButton.Visibility = Visibility.Collapsed;

            // Clear selections and hide buttons
            DeselectAll();
            ConfirmButton.Visibility = Visibility.Collapsed;
            CancelButton.Visibility = Visibility.Collapsed;
        }

        private void FinishDrinkButton_Click(object sender, RoutedEventArgs e)
        {
            if (_game != null)
            {
                _game.FinishDrink();
            }
        }

        private void OverlayWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            AdjustFontSizes(UnifiedBorder);
            AdjustPlayerBoxesWidth(); // Adjust player boxes' width on window size change
        }

        // Implement INotifyPropertyChanged for data binding
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
