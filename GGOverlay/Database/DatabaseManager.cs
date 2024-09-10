using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace GGOverlay.Database
{
    public class DatabaseManager
    {
        private readonly string _connectionString;

        public DatabaseManager(string databasePath)
        {
            _connectionString = $"Data Source={databasePath};Version=3;";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                // Create the counters table if it does not exist
                string createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS Counters (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL UNIQUE,
                        Value INTEGER NOT NULL
                    );";
                using (var command = new SQLiteCommand(createTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }

                // Initialize the three counters if they don't exist
                InitializeCounter("Counter1");
                InitializeCounter("Counter2");
                InitializeCounter("Counter3");
            }
        }

        private void InitializeCounter(string counterName)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string insertQuery = "INSERT OR IGNORE INTO Counters (Name, Value) VALUES (@name, 0);";
                using (var command = new SQLiteCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@name", counterName);
                    command.ExecuteNonQuery();
                }
            }
        }

        public int GetCounterValue(string counterName)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string selectQuery = "SELECT Value FROM Counters WHERE Name = @name;";
                using (var command = new SQLiteCommand(selectQuery, connection))
                {
                    command.Parameters.AddWithValue("@name", counterName);
                    var result = command.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : 0;
                }
            }
        }

        public void UpdateCounterValue(string counterName, int newValue)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string updateQuery = "UPDATE Counters SET Value = @value WHERE Name = @name;";
                using (var command = new SQLiteCommand(updateQuery, connection))
                {
                    command.Parameters.AddWithValue("@value", newValue);
                    command.Parameters.AddWithValue("@name", counterName);
                    command.ExecuteNonQuery();
                }
            }
        }

        public Dictionary<string, int> GetAllCounterValues()
        {
            var counters = new Dictionary<string, int>();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string selectQuery = "SELECT Name, Value FROM Counters;";
                using (var command = new SQLiteCommand(selectQuery, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string name = reader.GetString(0);
                        int value = reader.GetInt32(1);
                        counters[name] = value;
                    }
                }
            }
            return counters;
        }
    }
}
