using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Newtonsoft.Json;

namespace GGOverlay.Game
{
    public class GameRules
    {
        // List of Rule objects
        public List<Rule> Rules { get; set; }

        // Path to the source file (if loaded from a file)
        public string SourceFilePath { get; set; }

        // Logging event callback
        public event Action<string> OnLog;

        // Constructor initializes the list of rules
        public GameRules()
        {
            Rules = new List<Rule>();
        }

        public void LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    OnLog?.Invoke("The specified file does not exist.");
                    return;
                }

                string json = File.ReadAllText(filePath);
                Rules = JsonConvert.DeserializeObject<List<Rule>>(json) ?? new List<Rule>();
                SourceFilePath = filePath;
                OnLog?.Invoke("Rules loaded successfully.");
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Error loading rules from file: {ex.Message}");
            }
        }

        public void SaveToFile(string filePath)
        {
            try
            {
                string json = JsonConvert.SerializeObject(Rules, Formatting.Indented);
                File.WriteAllText(filePath, json);
                SourceFilePath = filePath;
                OnLog?.Invoke("Rules saved successfully.");
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Error saving rules to file: {ex.Message}");
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

    public class Rule : INotifyPropertyChanged
    {
        private bool _isGroupPunishment;
        private string _ruleDescription;
        private string _punishmentDescription;
        private int _punishmentQuantity;

        public bool IsGroupPunishment
        {
            get => _isGroupPunishment;
            set
            {
                if (_isGroupPunishment != value)
                {
                    _isGroupPunishment = value;
                    OnPropertyChanged(nameof(IsGroupPunishment));
                }
            }
        }

        public string RuleDescription
        {
            get => _ruleDescription;
            set
            {
                if (_ruleDescription != value)
                {
                    _ruleDescription = value;
                    OnPropertyChanged(nameof(RuleDescription));
                }
            }
        }

        public string PunishmentDescription
        {
            get => _punishmentDescription;
            set
            {
                if (_punishmentDescription != value)
                {
                    _punishmentDescription = value;
                    OnPropertyChanged(nameof(PunishmentDescription));
                }
            }
        }

        public int PunishmentQuantity
        {
            get => _punishmentQuantity;
            set
            {
                if (_punishmentQuantity != value)
                {
                    _punishmentQuantity = value;
                    OnPropertyChanged(nameof(PunishmentQuantity));
                }
            }
        }

        public Rule()
        {
        }

        public Rule(bool isGroupPunishment, string ruleDescription, string punishmentDescription, int punishmentQuantity)
        {
            IsGroupPunishment = isGroupPunishment;
            RuleDescription = ruleDescription;
            PunishmentDescription = punishmentDescription;
            PunishmentQuantity = punishmentQuantity;
        }

        public Rule Clone()
        {
            return new Rule(IsGroupPunishment, RuleDescription, PunishmentDescription, PunishmentQuantity);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public override bool Equals(object obj)
        {
            if (obj is Rule otherRule)
            {
                return this.IsGroupPunishment == otherRule.IsGroupPunishment &&
                       this.RuleDescription == otherRule.RuleDescription &&
                       this.PunishmentDescription == otherRule.PunishmentDescription &&
                       this.PunishmentQuantity == otherRule.PunishmentQuantity;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(IsGroupPunishment, RuleDescription, PunishmentDescription, PunishmentQuantity);
        }

        // Method to get the formatted punishment description
        public string GetPunishmentDescription(string name = "{Player}", double DrinkModifier = 1)
        {
            // Replace the placeholders with provided name and formatted drink description
            string playerName = string.IsNullOrEmpty(name) ? "{Player}" : name;

            // Calculate the adjusted punishment quantity using Math.Round with MidpointRounding.AwayFromZero
            int adjustedPunishmentQuantity = (int)Math.Round(PunishmentQuantity * DrinkModifier, MidpointRounding.AwayFromZero);

            // Use the adjusted punishment quantity in the formatted drink description
            string drinkDescription = FormatDrinkDescription(adjustedPunishmentQuantity);

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
