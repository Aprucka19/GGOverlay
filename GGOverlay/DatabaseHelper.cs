using Microsoft.Data.Sqlite;
using System;

public static class DatabaseHelper
{
    private const string DbFileName = "CounterDatabase.db";

    public static void InitializeDatabase()
    {
        using (var connection = new SqliteConnection($"Data Source={DbFileName}"))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText =
            @"
                CREATE TABLE IF NOT EXISTS Counter(
                    ID INTEGER PRIMARY KEY,
                    Value INTEGER NOT NULL
                );
                
                INSERT INTO Counter (Value) 
                SELECT 0 WHERE NOT EXISTS (SELECT 1 FROM Counter WHERE ID = 1);
            ";
            command.ExecuteNonQuery();
        }
    }

    public static int GetCounterValue()
    {
        using (var connection = new SqliteConnection($"Data Source={DbFileName}"))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT Value FROM Counter WHERE ID = 1;";
            return Convert.ToInt32(command.ExecuteScalar());
        }
    }

    public static void UpdateCounter(int newValue)
    {
        using (var connection = new SqliteConnection($"Data Source={DbFileName}"))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "UPDATE Counter SET Value = @newValue WHERE ID = 1;";
            command.Parameters.AddWithValue("@newValue", newValue);
            command.ExecuteNonQuery();
        }
    }
}
