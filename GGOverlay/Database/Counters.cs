using System.Collections.Generic;

namespace GGOverlay.Database
{
    public class Counters
    {
        private readonly DatabaseManager _databaseManager;

        public Counters(DatabaseManager databaseManager)
        {
            _databaseManager = databaseManager;
            InitializeCounters();
        }

        // Initializes the Counters table and default counters
        public void InitializeCounters()
        {
            // Create the Counters table if it does not exist
            _databaseManager.ExecuteNonQuery(
                @"CREATE TABLE IF NOT EXISTS Counters (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL UNIQUE,
                    Value INTEGER NOT NULL
                );",
                new Dictionary<string, object>()
            );

            // Ensure default counters exist
            AddCounter("Counter1");
            AddCounter("Counter2");
            AddCounter("Counter3");
        }

        // Add a counter if it does not exist
        public void AddCounter(string counterName)
        {
            _databaseManager.ExecuteNonQuery(
                "INSERT OR IGNORE INTO Counters (Name, Value) VALUES (@name, @value);",
                new Dictionary<string, object> { { "@name", counterName }, { "@value", 0 } }
            );
        }

        // Get the value of a specific counter
        public int GetCounterValue(string counterName)
        {
            // Retrieve data specifically from the Counters table
            var result = _databaseManager.GetAllData();

            // Check if the Counters table exists in the result
            if (result.TryGetValue("Counters", out var tableData))
            {
                foreach (var row in tableData)
                {
                    if (row["Name"].ToString() == counterName)
                    {
                        return int.Parse(row["Value"].ToString());
                    }
                }
            }
            return 0; // Return 0 if not found
        }

        // Update the value of a specific counter
        public void UpdateCounterValue(string counterName, int newValue)
        {
            _databaseManager.ExecuteNonQuery(
                "UPDATE Counters SET Value = @value WHERE Name = @name;",
                new Dictionary<string, object> { { "@name", counterName }, { "@value", newValue } }
            );
        }

        // Remove a counter from the database
        public void RemoveCounter(string counterName)
        {
            _databaseManager.ExecuteNonQuery(
                "DELETE FROM Counters WHERE Name = @name;",
                new Dictionary<string, object> { { "@name", counterName } }
            );
        }

        // Get all counter values
        public Dictionary<string, int> GetAllCounterValues()
        {
            var counters = new Dictionary<string, int>();
            var result = _databaseManager.GetAllData();

            // Check if the Counters table exists in the result
            if (result.TryGetValue("Counters", out var tableData))
            {
                foreach (var row in tableData)
                {
                    counters[row["Name"].ToString()] = int.Parse(row["Value"].ToString());
                }
            }

            return counters;
        }
    }
}
