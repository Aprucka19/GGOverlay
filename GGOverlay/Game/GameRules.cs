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
        // Method to serialize the rules into a string
        public string Send()
        {
            try
            {
                // Serialize the rules list to a JSON string
                string serializedRules = JsonConvert.SerializeObject(Rules, Formatting.Indented);
                OnLog?.Invoke("Rules serialized successfully.");
                return serializedRules;
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Error serializing rules: {ex.Message}");
                return string.Empty;
            }
        }

        // Method to deserialize the string into rules and set them
        public void Receive(string serializedRules)
        {
            try
            {
                // Deserialize the string into a list of rules
                Rules = JsonConvert.DeserializeObject<List<Rule>>(serializedRules) ?? new List<Rule>();
                OnLog?.Invoke("Rules deserialized successfully.");
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Error deserializing rules: {ex.Message}");
            }
        }


        // Method to display a specific rule or all rules
        public string DisplayRules(int? ruleNumber = null)
        {
            // Check if ruleNumber is provided and within valid range
            if (ruleNumber.HasValue)
            {
                int index = ruleNumber.Value - 1; // Convert to zero-based index
                if (index >= 0 && index < Rules.Count)
                {
                    return FormatRule(Rules[index]);
                }
                else
                {
                    return "Invalid rule number.";
                }
            }
            else
            {
                // Display all rules
                List<string> formattedRules = new List<string>();
                for (int i = 0; i < Rules.Count; i++)
                {
                    formattedRules.Add($"{i + 1}. {FormatRule(Rules[i])}");
                }
                return string.Join(Environment.NewLine, formattedRules);
            }
        }

        // Helper method to format a rule with placeholders replaced
        private string FormatRule(Rule rule)
        {
            return rule.PunishmentDescription.Replace("{0}", "Player").Replace("{1}", rule.PunishmentQuantity.ToString());
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
        public int PunishmentQuantity { get; set; }

        // Constructor to initialize the rule with given values
        public Rule(bool isGroupPunishment, string ruleDescription, string punishmentDescription, int punishmentQuantity)
        {
            IsGroupPunishment = isGroupPunishment;
            RuleDescription = ruleDescription;
            PunishmentDescription = punishmentDescription;
            PunishmentQuantity = punishmentQuantity;
        }

        // Method to get the formatted punishment description
        public string GetPunishmentDescription(string name = "{Player}")
        {
            // Replace the placeholders with provided name and formatted drink description
            string playerName = string.IsNullOrEmpty(name) ? "{Player}" : name;
            string drinkDescription = FormatDrinkDescription(PunishmentQuantity);

            return PunishmentDescription.Replace("{0}", playerName).Replace("{1}", drinkDescription);
        }

        private string FormatDrinkDescription(int quantity)
        {
            string drinkText;

            if (quantity == 1)
            {
                drinkText = "1 sip";
            }
            else if (quantity > 1 && quantity < 20)
            {
                drinkText = $"{quantity} sips";
            }
            else if (quantity == 20)
            {
                drinkText = "a full drink";
            }
            else
            {
                // Calculate full drinks and remaining sips
                int fullDrinks = quantity / 20;
                int sips = quantity % 20;

                string fullDrinksText = fullDrinks == 1 ? "1 full drink" : $"{fullDrinks} full drinks";
                string sipsText = sips == 1 ? "1 sip" : $"{sips} sips";

                if (sips == 0)
                {
                    drinkText = fullDrinksText;
                }
                else
                {
                    drinkText = $"{fullDrinksText} and {sipsText}";
                }
            }

            return drinkText;
        }


        // Method to display rule details
        public override string ToString()
        {
            string punishmentType = IsGroupPunishment ? "Group" : "Individual";
            return $"{punishmentType} Punishment: {PunishmentDescription}, Quantity: {PunishmentQuantity}";
        }
    }


}
