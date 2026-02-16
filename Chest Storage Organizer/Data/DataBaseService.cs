using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Data.Sqlite;
using Chest_Storage_Organizer.Models;

namespace Chest_Storage_Organizer.Data
{
    public class DataBaseService
    {
        private readonly string _connectionString = "Data Source=minecraft_inventory.db";

        public void Initialize()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS Items (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ItemName TEXT,
                Quantity INTEGER,
                Source TEXT
            );
            ";

            command.ExecuteNonQuery();
        }

        public void InsertItem(InventoryItem item)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
            INSERT INTO Items (ItemName, Quantity, Source)
            VALUES ($name, $quantity, $source);
            ";

            command.Parameters.AddWithValue("$name", item.ItemName);
            command.Parameters.AddWithValue("$quantity", item.Quantity);
            command.Parameters.AddWithValue("$source", item.Source);

            command.ExecuteNonQuery();
        }

    }
}
