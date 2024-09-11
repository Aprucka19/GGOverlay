// Data/Profile.cs
using System;
using System.IO;

namespace GGOverlay.Data
{
    public class Profile
    {
        public string Name { get; set; }
        public decimal DrinkScale { get; set; }
        public string ImageBase64 { get; set; } // Store image as a base64 string for thread safety

        public Profile(string name, decimal drinkScale, string imageBase64)
        {
            Name = name;
            DrinkScale = drinkScale;
            ImageBase64 = ValidateBase64String(imageBase64);
        }

        public Profile()
        {
            Name = string.Empty;
            DrinkScale = 0;
            ImageBase64 = string.Empty;
        }

        // Validate and ensure that the base64 string is correctly formatted
        private string ValidateBase64String(string base64)
        {
            if (string.IsNullOrWhiteSpace(base64))
                return string.Empty;

            try
            {
                // Try converting back to bytes to check if it's a valid base64 string
                Convert.FromBase64String(base64);
                return base64; // If no exception, it's valid
            }
            catch (FormatException)
            {
                Console.WriteLine("Invalid base64 string detected.");
                return string.Empty; // Return empty if invalid
            }
        }
    }
}
