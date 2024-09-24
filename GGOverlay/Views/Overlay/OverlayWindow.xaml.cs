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
        private const uint MOD_CONTROL = 0x0002;
        private const uint VK_BACKTICK = 0xC0; // VK_OEM_3 for '`' key

        // Constants for extended window styles
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;

        // Timer for auto-hiding the punishment display
        private DispatcherTimer punishmentTimer;

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

            // Subscribe to the OnPunishmentTriggered event
            _game.OnGroupPunishmentTriggered += HandleGroupPunishmentTriggered;
            _game.OnIndividualPunishmentTriggered += HandleIndividualPunishmentTriggered;

            // Subscribe to size change event to adjust font sizes
            this.SizeChanged += OverlayWindow_SizeChanged;

            // Remove the initial interactive mode setting from here
            ToggleMode();
            if (!isInteractive)
            {
                ToggleMode();
            }

            // Initialize ComboBoxes
            InitializeComboBoxes();

            // Load settings from UserData
            LoadUserDataSettings();
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
                InteractiveControlsBackground.Visibility = Visibility.Collapsed;
                UnifiedResizeThumb.Visibility = Visibility.Collapsed; // Hide the ResizeThumb
            }
            else
            {
                SetInteractiveMode();
                InteractiveControlsBackground.Visibility = Visibility.Visible;
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

            // Clear selections and hide buttons
            DeselectAll();
            ConfirmButton.Visibility = Visibility.Collapsed;
            CancelButton.Visibility = Visibility.Collapsed;
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
