using System;
using System.Collections.Generic;
using System.Data.SQLite;
using Newtonsoft.Json;

namespace GGOverlay.Database
{
    public class DatabaseManager
    {
        private readonly string _connectionString;

        // Event to notify changes in a generic format
        public event Action<string> OnDatabaseChanged;

        // Flag to suppress broadcasting of changes
        private bool _suppressBroadcast = false;

        public DatabaseManager(string databasePath)
        {
            _connectionString = $"Data Source={databasePath};Version=3;";
        }

        // Generic method to execute non-query SQL commands (INSERT, UPDATE, DELETE)
        public void ExecuteNonQuery(string query, Dictionary<string, object> parameters, bool suppressBroadcast = false)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(query, connection))
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.AddWithValue(param.Key, param.Value);
                    }
                    command.ExecuteNonQuery();
                    OnDatabaseChanged?.Invoke(command.ToString());
                }
            }

            // Set the suppression flag based on input
            _suppressBroadcast = suppressBroadcast;

            // Broadcast changes to clients or host if not suppressed
            if (!_suppressBroadcast)
            {
                BroadcastChange(query, parameters);
            }

            // Reset the suppression flag
            _suppressBroadcast = false;
        }

        // Method to serialize and broadcast changes to connected clients or the host
        private void BroadcastChange(string query, Dictionary<string, object> parameters)
        {
            var changeMessage = new
            {
                Query = query,
                Parameters = parameters
            };

            string serializedChange = JsonConvert.SerializeObject(changeMessage);

        }

        // Retrieves all data from the entire database, used for syncing initial states
        public Dictionary<string, List<Dictionary<string, object>>> GetAllData()
        {
            var allData = new Dictionary<string, List<Dictionary<string, object>>>();

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                // Retrieve all table names
                var tableNames = GetAllTableNames(connection);

                // Iterate through each table and fetch its data
                foreach (var tableName in tableNames)
                {
                    var tableData = new List<Dictionary<string, object>>();
                    string selectQuery = $"SELECT * FROM {tableName};";

                    using (var command = new SQLiteCommand(selectQuery, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var row = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row[reader.GetName(i)] = reader.GetValue(i);
                            }
                            tableData.Add(row);
                        }
                    }

                    // Add the table data to the main dictionary
                    allData[tableName] = tableData;
                }
            }

            return allData;
        }

        // Helper method to get all table names from the database
        private List<string> GetAllTableNames(SQLiteConnection connection)
        {
            var tableNames = new List<string>();

            string query = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';";
            using (var command = new SQLiteCommand(query, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    tableNames.Add(reader.GetString(0));
                }
            }

            return tableNames;
        }

        public void ClearDatabase()
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var tableNames = GetAllTableNames(connection);

                // Clear each table in the database
                foreach (var tableName in tableNames)
                {
                    using (var command = new SQLiteCommand($"DELETE FROM {tableName};", connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        public void CreateTableIfNotExists(string tableName)
        {
            // Example for a generic table creation, adjust based on expected table structure
            string createTableQuery = $@"
        CREATE TABLE IF NOT EXISTS {tableName} (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT,
            Value INTEGER
        );";
            ExecuteNonQuery(createTableQuery, new Dictionary<string, object>(), suppressBroadcast: true);
        }


    }
}
