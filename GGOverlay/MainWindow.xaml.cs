// MainWindow.xaml.cs
using GGOverlay.Data;
using GGOverlay.Networking;
using GGOverlay.Utilities;
using Microsoft.Win32;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace GGOverlay
{
    public partial class MainWindow : Window
    {
        private GameData _gameData;
        private Profile _localProfile;
        private NetworkServer _server;
        private NetworkClient _client;

        public MainWindow()
        {
            InitializeComponent();
            Logger.OnLogMessage += Log; // Subscribe to centralized logging
        }

        // Create a profile when the user clicks Create Profile
        private void CreateProfileButton_Click(object sender, RoutedEventArgs e)
        {
            _localProfile = CreateProfile();
            if (_localProfile != null)
            {
                Logger.Log($"Profile created: {_localProfile.Name}");
                UpdateLocalProfileUI();
            }
        }

        private Profile CreateProfile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                BitmapImage originalImage = new BitmapImage(new Uri(openFileDialog.FileName));
                BitmapImage resizedImage = ResizeImage(originalImage, 128, 128); // Resize the image to 128x128 pixels
                string name = PromptForName();
                decimal drinkScale = PromptForDrinkScale();
                string imageBase64 = ConvertBitmapImageToBase64(resizedImage); // Convert resized image to base64 for profile

                return new Profile(name, drinkScale, imageBase64);
            }
            return null;
        }

        // Method to resize a BitmapImage to the specified width and height
        private BitmapImage ResizeImage(BitmapImage originalImage, int width, int height)
        {
            // Create a DrawingVisual to render the image
            var drawingVisual = new System.Windows.Media.DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.DrawImage(originalImage, new System.Windows.Rect(0, 0, width, height));
            }

            // Render the DrawingVisual into a RenderTargetBitmap
            var resizedBitmap = new RenderTargetBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            resizedBitmap.Render(drawingVisual);

            // Convert RenderTargetBitmap back to BitmapImage
            var bitmapImage = new BitmapImage();
            using (MemoryStream stream = new MemoryStream())
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(resizedBitmap));
                encoder.Save(stream);
                stream.Position = 0;

                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
            }

            return bitmapImage;
        }



        // Host a server and create GameData
        private async void HostButton_Click(object sender, RoutedEventArgs e)
        {
            if (_server == null && _localProfile != null)
            {
                _server = new NetworkServer();
                _gameData = new GameData(_server); // Pass the server to GameData
                _gameData.OnDataUpdated += UpdateGameDataUI;

                await _gameData.AddProfile(_localProfile);
                await _server.StartAsync();
                Logger.Log("Server hosted successfully with initial game data.");
            }
            else
            {
                Logger.Log("Create a profile before hosting or server is already running.");
            }
        }

        // Join as a client and synchronize GameData
        private async void JoinButton_Click(object sender, RoutedEventArgs e)
        {
            if (_client == null && _localProfile != null)
            {
                string ipAddress = IpAddressTextBox.Text.Trim();
                if (!string.IsNullOrEmpty(ipAddress))
                {
                    _client = new NetworkClient();
                    _gameData = new GameData(client: _client); // Pass the client to GameData
                    _gameData.OnDataUpdated += UpdateGameDataUI;

                    await _client.ConnectAsync(ipAddress);
                    await _gameData.AddProfile(_localProfile);
                    Logger.Log("Connected to the server, awaiting game data.");
                }
                else
                {
                    Logger.Log("Please enter a valid IP address.");
                }
            }
            else
            {
                Logger.Log("Create a profile before joining or already connected as a client.");
            }
        }

        // Increment the counter when the Plus button is clicked
        private async void PlusButton_Click(object sender, RoutedEventArgs e)
        {
            if (_gameData != null)
            {
                await _gameData.UpdateCounter(_gameData.Counter.Value + 1);
            }
        }

        // Decrement the counter when the Minus button is clicked
        private async void MinusButton_Click(object sender, RoutedEventArgs e)
        {
            if (_gameData != null)
            {
                await _gameData.UpdateCounter(_gameData.Counter.Value - 1);
            }
        }

        // Update the local profile UI
        private void UpdateLocalProfileUI()
        {
            if (_localProfile != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    LocalProfileName.Text = _localProfile.Name;
                    LocalProfileScaler.Text = $"Scaler: {_localProfile.DrinkScale}";
                    LocalProfileImage.Source = ConvertBase64ToBitmapImage(_localProfile.ImageBase64); // Convert base64 to BitmapImage for display
                });
            }
        }

        // Convert a base64 string to a BitmapImage for UI display
        private BitmapImage ConvertBase64ToBitmapImage(string base64)
        {
            if (string.IsNullOrEmpty(base64)) return null;

            byte[] imageBytes = Convert.FromBase64String(base64);
            BitmapImage bitmapImage = new BitmapImage();

            using (MemoryStream ms = new MemoryStream(imageBytes))
            {
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = ms;
                bitmapImage.EndInit();
            }

            return bitmapImage;
        }

        // Convert BitmapImage to a base64 string
        private string ConvertBitmapImageToBase64(BitmapImage bitmapImage)
        {
            if (bitmapImage == null) return string.Empty;

            using (var ms = new MemoryStream())
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
                encoder.Save(ms);
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        // Update the entire GameData UI
        private void UpdateGameDataUI()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CounterTextBox.Text = _gameData.Counter.Value.ToString();
                UpdateProfilesUI();
            });
        }

        // Update the connected profiles UI
        private void UpdateProfilesUI()
        {
            ProfilesPanel.Children.Clear();
            foreach (var profile in _gameData.Profiles)
            {
                var profilePanel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Margin = new Thickness(10)
                };

                var profileImage = new Image
                {
                    Width = 80,
                    Height = 80,
                    Source = ConvertBase64ToBitmapImage(profile.ImageBase64), // Convert base64 to BitmapImage
                    Margin = new Thickness(0, 0, 0, 5)
                };

                var profileName = new TextBlock
                {
                    Text = profile.Name,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                var profileScaler = new TextBlock
                {
                    Text = $"Scaler: {profile.DrinkScale}",
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                profilePanel.Children.Add(profileImage);
                profilePanel.Children.Add(profileName);
                profilePanel.Children.Add(profileScaler);

                ProfilesPanel.Children.Add(profilePanel);
            }
        }

        // Logging utility to update UI
        private void Log(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                LogTextBox.AppendText($"{message}\n");
                LogTextBox.ScrollToEnd();
            });
        }

        private string PromptForName()
        {
            return Microsoft.VisualBasic.Interaction.InputBox("Enter Profile Name:", "Profile Name", "Player");
        }

        private decimal PromptForDrinkScale()
        {
            string input = Microsoft.VisualBasic.Interaction.InputBox("Enter Drink Scale:", "Drink Scale", "1.0");
            return decimal.TryParse(input, out decimal result) ? result : 1.0m;
        }
    }
}
