using System;
using System.IO;
using Newtonsoft.Json;

namespace GGOverlay.Game
{
    public class PlayerInfo
    {
        // Player's name
        public string Name { get; set; }

        // Drink modifier value
        public double DrinkModifier { get; set; }

        // Logging event callback
        public static event Action<string> OnLog;

        // Constructor to initialize the player info with default values
        public PlayerInfo()
        {
        }

        // Constructor to initialize the player info with given values
        public PlayerInfo(string name, double drinkModifier)
        {
            Name = name;
            DrinkModifier = drinkModifier;
        }

        // Method to save a single PlayerInfo object to a JSON file
        public static void SaveToFile(string filePath, PlayerInfo player)
        {
            try
            {
                // Serialize the player object to JSON format using Newtonsoft.Json
                string json = JsonConvert.SerializeObject(player, Formatting.Indented);

                // Write the JSON string to the specified file path
                File.WriteAllText(filePath, json);
                OnLog?.Invoke("Player information saved successfully.");
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Error saving player information to file: {ex.Message}");
            }
        }

        // Method to load a single PlayerInfo object from a JSON file
        public static PlayerInfo LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    OnLog?.Invoke("The specified file does not exist.");
                    return new PlayerInfo(); // Return a default player object
                }

                // Read the JSON string from the specified file path
                string json = File.ReadAllText(filePath);

                // Deserialize the JSON string into a PlayerInfo object using Newtonsoft.Json
                PlayerInfo player = JsonConvert.DeserializeObject<PlayerInfo>(json) ?? new PlayerInfo();
                OnLog?.Invoke("Player information loaded successfully.");
                return player;
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Error loading player information from file: {ex.Message}");
                return new PlayerInfo(); // Return a default player object in case of error
            }
        }

        // Override ToString to provide a readable representation of the PlayerInfo object
        public override string ToString()
        {
            return $"{Name}: Drink Modifier = {DrinkModifier}";
        }
    }
}
