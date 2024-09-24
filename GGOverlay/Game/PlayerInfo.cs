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

        // New: Drink count in sips
        public int DrinkCount { get; set; } = 0;

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
                player.DrinkCount = 0;
                return player;
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Error loading player information from file: {ex.Message}");
                return new PlayerInfo(); // Return a default player object in case of error
            }
        }

        // Method to serialize the player info into a string
        public string Send()
        {
            try
            {
                // Serialize the player info to a JSON string
                string serializedPlayer = JsonConvert.SerializeObject(this, Formatting.Indented);
                OnLog?.Invoke("Player information serialized successfully.");
                return serializedPlayer;
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Error serializing player information: {ex.Message}");
                return string.Empty;
            }
        }

        // Method to deserialize the string into player info and set the properties
        public void Receive(string serializedPlayer)
        {
            try
            {
                // Deserialize the string into a PlayerInfo object
                PlayerInfo player = JsonConvert.DeserializeObject<PlayerInfo>(serializedPlayer) ?? new PlayerInfo();
                Name = player.Name;
                DrinkModifier = player.DrinkModifier;
                OnLog?.Invoke("Player information deserialized successfully.");
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Error deserializing player information: {ex.Message}");
            }
        }

        // Override ToString to provide a readable representation of the PlayerInfo object
        public override string ToString()
        {
            return $"{Name}: Drink Modifier = {DrinkModifier}";
        }

        /// <summary>
        /// Converts the DrinkModifier double value to a simplified fraction string.
        /// </summary>
        /// <returns>Fraction string representation of DrinkModifier.</returns>
        public string ReturnFraction()
        {
            double value = DrinkModifier;

            // Handle negative values if necessary
            bool isNegative = value < 0;
            value = Math.Abs(value);

            // Check for the special case where the value is exactly 1
            if (Math.Abs(value - 1) < 1.0E-6)
            {
                return isNegative ? "-1" : "1";
            }

            // Define tolerance for precision
            double tolerance = 1.0E-6;
            double numerator = value;
            double denominator = 1;

            // Iteratively adjust numerator and denominator until the value is approximately the same as input
            while (Math.Abs(numerator % 1) > tolerance)
            {
                numerator *= 10;
                denominator *= 10;

                // Prevent infinite loop in case of recurring decimals
                if (denominator > 1000000)
                {
                    break;
                }
            }

            // Find the greatest common divisor to simplify the fraction
            int gcd = GCD((int)Math.Round(numerator), (int)Math.Round(denominator));

            // Simplify numerator and denominator
            int simplifiedNumerator = (int)Math.Round(numerator) / gcd;
            int simplifiedDenominator = (int)Math.Round(denominator) / gcd;

            // Construct the fraction string
            string fraction = $"{simplifiedNumerator}/{simplifiedDenominator}";

            // Add negative sign back if necessary
            if (isNegative)
            {
                fraction = "-" + fraction;
            }

            return fraction;
        }

        /// <summary>
        /// Calculates the Greatest Common Divisor (GCD) of two integers using the Euclidean algorithm.
        /// </summary>
        /// <param name="a">First integer.</param>
        /// <param name="b">Second integer.</param>
        /// <returns>GCD of a and b.</returns>
        private int GCD(int a, int b)
        {
            return b == 0 ? a : GCD(b, a % b);
        }
    }
}
