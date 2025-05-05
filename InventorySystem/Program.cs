//ITCS 3112 Final Project
//Ritvik Manem
//#Student ID: 801289730
using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace InventorySystem
{
    // Represents a single inventory item
    public class InventoryItem
    {
        public required string Name { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalValue => Quantity * UnitPrice;
    }

    // Manages the collection of inventory items using SQLite database
    public class InventoryManager
    {
        private const string ConnectionString = "Data Source=inventory.db";

        public InventoryManager()
        {
            // Ensure database and table exist
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            var tableCmd = connection.CreateCommand();
            tableCmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Items (
                    Name TEXT PRIMARY KEY,
                    Quantity INTEGER NOT NULL,
                    UnitPrice REAL NOT NULL
                );";
            tableCmd.ExecuteNonQuery();
        }

        public void AddItem(string name, int qty, decimal price)
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            var existsCmd = connection.CreateCommand();
            existsCmd.CommandText = "SELECT COUNT(1) FROM Items WHERE Name = $name";
            existsCmd.Parameters.AddWithValue("$name", name);
            var exists = Convert.ToInt32(existsCmd.ExecuteScalar());
            if (exists > 0)
            {
                Console.WriteLine("An item with that name already exists.");
                return;
            }

            var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = @"
                INSERT INTO Items (Name, Quantity, UnitPrice)
                VALUES ($name, $qty, $price);";
            insertCmd.Parameters.AddWithValue("$name", name);
            insertCmd.Parameters.AddWithValue("$qty", qty);
            insertCmd.Parameters.AddWithValue("$price", price);
            insertCmd.ExecuteNonQuery();

            Console.WriteLine("Item added successfully.");
        }

        public List<InventoryItem> GetAllItems()
        {
            var items = new List<InventoryItem>();
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = "SELECT Name, Quantity, UnitPrice FROM Items";
            using var reader = selectCmd.ExecuteReader();
            while (reader.Read())
            {
                items.Add(new InventoryItem
                {
                    Name = reader.GetString(0),
                    Quantity = reader.GetInt32(1),
                    UnitPrice = (decimal)reader.GetDouble(2)
                });
            }
            return items;
        }

        public void ViewInventory()
        {
            var items = GetAllItems();
            if (items.Count == 0)
            {
                Console.WriteLine("Inventory is empty.");
                return;
            }

            Console.WriteLine("\nCurrent Inventory:");
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine("{0,-20} {1,5}   {2,10}   {3,12}", "Name", "Qty", "Price", "Total");
            Console.WriteLine("--------------------------------------------------");
            foreach (var item in items)
            {
                Console.WriteLine("{0,-20} {1,5}   {2,10:C2}   {3,12:C2}", 
                    item.Name, item.Quantity, item.UnitPrice, item.TotalValue);
            }
            Console.WriteLine("--------------------------------------------------");
        }

        public void UpdateItem(string name)
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = "SELECT Quantity, UnitPrice FROM Items WHERE Name = $name";
            selectCmd.Parameters.AddWithValue("$name", name);
            using var reader = selectCmd.ExecuteReader();
            if (!reader.Read())
            {
                Console.WriteLine("Item not found.");
                return;
            }

            int currentQty = reader.GetInt32(0);
            decimal currentPrice = (decimal)reader.GetDouble(1);
            reader.Close();

            Console.WriteLine("Enter new name (or press Enter to keep current):");
            string newName = (Console.ReadLine() ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(newName)) newName = name;

            Console.WriteLine("Enter new quantity (or press Enter to keep current):");
            string qtyInput = (Console.ReadLine() ?? string.Empty).Trim();
            int newQty = int.TryParse(qtyInput, out var q) ? q : currentQty;

            Console.WriteLine("Enter new unit price (or press Enter to keep current):");
            string priceInput = (Console.ReadLine() ?? string.Empty).Trim();
            decimal newPrice = decimal.TryParse(priceInput, out var p) ? p : currentPrice;

            var updateCmd = connection.CreateCommand();
            updateCmd.CommandText = @"
                UPDATE Items
                SET Name = $newName, Quantity = $qty, UnitPrice = $price
                WHERE Name = $oldName;";
            updateCmd.Parameters.AddWithValue("$newName", newName);
            updateCmd.Parameters.AddWithValue("$qty", newQty);
            updateCmd.Parameters.AddWithValue("$price", newPrice);
            updateCmd.Parameters.AddWithValue("$oldName", name);
            updateCmd.ExecuteNonQuery();

            Console.WriteLine("Item updated successfully.");
        }

        public void RemoveItem(string name)
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            var deleteCmd = connection.CreateCommand();
            deleteCmd.CommandText = "DELETE FROM Items WHERE Name = $name";
            deleteCmd.Parameters.AddWithValue("$name", name);
            int rows = deleteCmd.ExecuteNonQuery();
            if (rows == 0)
            {
                Console.WriteLine("Item not found.");
                return;
            }
            Console.WriteLine("Item removed successfully.");
        }

        public void DisplayTotalInventoryValue()
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            var sumCmd = connection.CreateCommand();
            sumCmd.CommandText = "SELECT SUM(Quantity * UnitPrice) FROM Items";
            var result = sumCmd.ExecuteScalar();
            decimal total = result != DBNull.Value ? Convert.ToDecimal(result) : 0;
            Console.WriteLine($"\nTotal Inventory Value: {total:C}");
        }
    }

    class Program
    {
        static void Main()
        {
            var manager = new InventoryManager();
            bool exit = false;

            while (!exit)
            {
                Console.WriteLine("\nInventory Management System (SQLite)");
                Console.WriteLine("1. Add Item");
                Console.WriteLine("2. View Inventory");
                Console.WriteLine("3. Update Item");
                Console.WriteLine("4. Remove Item");
                Console.WriteLine("5. Calculate Total Value");
                Console.WriteLine("6. Exit");
                Console.Write("Select an option: ");

                string choice = (Console.ReadLine() ?? string.Empty).Trim();
                switch (choice)
                {
                    case "1":
                        Console.Write("Enter item name: ");
                        string name = Console.ReadLine()?.Trim() ?? string.Empty;
                        Console.Write("Enter quantity: ");
                        if (!int.TryParse(Console.ReadLine(), out int qty))
                        {
                            Console.WriteLine("Invalid quantity.");
                            break;
                        }
                        Console.Write("Enter unit price: ");
                        if (!decimal.TryParse(Console.ReadLine(), out decimal price))
                        {
                            Console.WriteLine("Invalid price.");
                            break;
                        }
                        manager.AddItem(name, qty, price);
                        break;

                    case "2":
                        manager.ViewInventory();
                        break;

                    case "3":
                        Console.Write("Enter the name of the item to update: ");
                        manager.UpdateItem(Console.ReadLine()?.Trim() ?? string.Empty);
                        break;

                    case "4":
                        Console.Write("Enter the name of the item to remove: ");
                        manager.RemoveItem(Console.ReadLine()?.Trim() ?? string.Empty);
                        break;

                    case "5":
                        manager.DisplayTotalInventoryValue();
                        break;

                    case "6":
                        exit = true;
                        break;

                    default:
                        Console.WriteLine("Invalid option. Please select 1-6.");
                        break;
                }
            }

            Console.WriteLine("Goodbye!");
        }
    }
}
