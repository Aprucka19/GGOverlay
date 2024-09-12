using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace GGOverlay.Game
{
    public class GameRules
    {
        // List of Rule objects
        public List<Rule> Rules { get; private set; }

        // Logging event callback
        public event Action<string> OnLog;

        // Constructor initializes the list of rules
        public GameRules()
        {
            Rules = new List<Rule>();
        }

        // Method to save rules to a JSON file
        public void SaveToFile(string filePath)
        {
            try
            {
                // Serialize the list of rules to JSON format using Newtonsoft.Json
                string json = JsonConvert.SerializeObject(Rules, Formatting.Indented);

                // Write the JSON string to the specified file path
                File.WriteAllText(filePath, json);
                OnLog?.Invoke("Rules saved successfully.");
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Error saving rules to file: {ex.Message}");
            }
        }

        // Method to load rules from a JSON file
        public void LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    OnLog?.Invoke("The specified file does not exist.");
                    return;
                }

                // Read the JSON string from the specified file path
                string json = File.ReadAllText(filePath);

                // Deserialize the JSON string into a list of rules using Newtonsoft.Json
                Rules = JsonConvert.DeserializeObject<List<Rule>>(json) ?? new List<Rule>();
                OnLog?.Invoke("Rules loaded successfully.");
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Error loading rules from file: {ex.Message}");
            }
        }
    }

    public class Rule
    {
        // Boolean to indicate if the punishment is for a group or individual
        public bool IsGroupPunishment { get; set; }

        // Description of the Rule
        public string RuleDescription { get; set; }

        // Description of the punishment
        public string PunishmentDescription { get; set; }

        // Quantity value for the punishment
        public double PunishmentQuantity { get; set; }

        // Constructor to initialize the rule with given values
        public Rule(bool isGroupPunishment, string ruleDescription, string punishmentDescription, double punishmentQuantity)
        {
            IsGroupPunishment = isGroupPunishment;
            RuleDescription = ruleDescription;
            PunishmentDescription = punishmentDescription;
            PunishmentQuantity = punishmentQuantity;
        }

        // Method to display rule details
        public override string ToString()
        {
            string punishmentType = IsGroupPunishment ? "Group" : "Individual";
            return $"{punishmentType} Punishment: {PunishmentDescription}, Quantity: {PunishmentQuantity}";
        }
    }
}
