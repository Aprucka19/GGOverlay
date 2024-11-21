// OverlaySettings.cs
using GGOverlay.Game;
using Newtonsoft.Json;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace GGOverlay
{
    public class OverlaySettings
    {
        // UI control settings
        public string FontColor { get; set; } = "#FFFFFF"; // Default white
        public double FontScaleMultiplier { get; set; } = 1.0;
        public string BackgroundColor { get; set; } = "#000000"; // Default black
        public double WindowWidth { get; set; } = 300; // Default width matching LoadUserDataSettings
        public double WindowHeight { get; set; } = 400; // Default height matching LoadUserDataSettings
        public double WindowLeft { get; set; } = 50; // Default left
        public double WindowTop { get; set; } = 50; // Default top

        // Added properties for opacity settings
        public double TextOpacity { get; set; } = 1.0;
        public double BackgroundOpacity { get; set; } = 1.0;
        // Add other settings as needed

        public string FontName { get; set; } = "Segoe UI"; // Default font
    }

    public class UserData
    {
        public PlayerInfo LocalPlayer { get; set; }
        public OverlaySettings OverlaySettings { get; set; } = new OverlaySettings();

        [JsonIgnore]
        private string UserDataPath { get; set; }

        public UserData()
        {
            // Initialize the path
            string userRulesDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "GGOverlay"
            );
            if (!Directory.Exists(userRulesDirectory))
            {
                Directory.CreateDirectory(userRulesDirectory);
            }

            UserDataPath = Path.Combine(userRulesDirectory, "UserData.json");
        }

        public void Save()
        {
            try
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(UserDataPath, json);
            }
            catch (Exception ex)
            {
                // Handle exception, possibly log it
                // For simplicity, show a message box
                MessageBox.Show($"Failed to save user data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static UserData Load()
        {
            string userRulesDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "GGOverlay"
            );
            string userDataPath = Path.Combine(userRulesDirectory, "UserData.json");

            if (File.Exists(userDataPath))
            {
                try
                {
                    string json = File.ReadAllText(userDataPath);
                    UserData data = JsonConvert.DeserializeObject<UserData>(json) ?? new UserData();
                    if (data.LocalPlayer != null)
                    {
                        data.LocalPlayer.DrinkCount = 0;
                    }
                    return data;
                }
                catch (Exception ex)
                {
                    // Handle exception, possibly log it
                    MessageBox.Show($"Failed to load user data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return new UserData();
                }
            }
            else
            {
                return new UserData();
            }
        }
    }
}
