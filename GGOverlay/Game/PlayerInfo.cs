using System;
using System.Collections.Generic;
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

        // Method to save a list of PlayerInfo objects to a JSON file
        public static void SaveToFile(string filePath, List<PlayerInfo> players)
        {
            try
            {
                // Serialize the list of players to JSON format using Newtonsoft.Json
                string json = JsonConvert.SerializeObject(players, Formatting.Indented);

                // Write the JSON string to the specified file path
                File.WriteAllText(filePath, json);
                OnLog?.Invoke("Player information saved successfully.");
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Error saving player information to file: {ex.Message}");
            }
        }

        // Method to load a list of PlayerInfo objects from a JSON file
        public static List<PlayerInfo> LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    OnLog?.Invoke("The specified file does not exist.");
                    return new List<PlayerInfo>();
                }

                // Read the JSON string from the specified file path
                string json = File.ReadAllText(filePath);

                // Deserialize the JSON string into a list of PlayerInfo objects using Newtonsoft.Json
                List<PlayerInfo> players = JsonConvert.DeserializeObject<List<PlayerInfo>>(json) ?? new List<PlayerInfo>();
                OnLog?.Invoke("Player information loaded successfully.");
                return players;
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Error loading player information from file: {ex.Message}");
                return new List<PlayerInfo>();
            }
        }

        // Override ToString to provide a readable representation of the PlayerInfo object
        public override string ToString()
        {
            return $"{Name}: Drink Modifier = {DrinkModifier}";
        }
    }
}
