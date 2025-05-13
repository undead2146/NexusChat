using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NexusChat.Core.Models;
using SQLite;

namespace NexusChat.Helpers
{
    /// <summary>
    /// Helper for database operations related to AI models
    /// </summary>
    public static class ModelDatabaseHelper {
        /// <summary>
        /// Ensures that a table has all required columns for a type
        /// </summary>
        public static async Task EnsureColumnsExist<T>(SQLiteAsyncConnection db, string tableName) where T : new() {
            try {
                Debug.WriteLine($"Checking if {tableName} table has all required columns");

                // Get the table info
                var tableInfo = await db.GetTableInfoAsync(tableName);
                if (tableInfo == null || tableInfo.Count == 0) {
                    Debug.WriteLine($"Table {tableName} doesn't exist, creating it");
                    await db.CreateTableAsync<T>();
                    return;
                }

                // Get the column names from the table info
                var columnNames = tableInfo.Select(c => c.Name).ToList();
                Debug.WriteLine($"Found columns: {string.Join(", ", columnNames)}");

                // Get the property info of the type
                var properties = typeof(T).GetProperties();

                // Check if all properties are present as columns
                foreach (var property in properties) {
                    // Skip if it's an ignore property
                    var ignoreAttr = property.GetCustomAttributes(typeof(IgnoreAttribute), true);
                    if (ignoreAttr.Length > 0)
                        continue;

                    // Get the column name (might be different from property name)
                    var columnAttr = property.GetCustomAttributes(typeof(ColumnAttribute), true);
                    string columnName = columnAttr.Length > 0
                        ? ((ColumnAttribute)columnAttr[0]).Name
                        : property.Name;

                    // Check if the column exists
                    if (!columnNames.Contains(columnName, StringComparer.OrdinalIgnoreCase)) {
                        Debug.WriteLine($"Column {columnName} is missing, adding it");

                        // Determine the SQLite type based on the property type
                        string sqliteType = GetSqliteType(property.PropertyType);

                        // Create the column
                        string alterSql = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {sqliteType}";
                        Debug.WriteLine($"Executing SQL: {alterSql}");

                        try {
                            await db.ExecuteAsync(alterSql);
                            Debug.WriteLine($"Added column {columnName} to {tableName}");
                        }
                        catch (Exception ex) {
                            Debug.WriteLine($"Error adding column {columnName}: {ex.Message}");
                        }
                    }
                }

                Debug.WriteLine($"Column check completed for {tableName}");
            }
            catch (Exception ex) {
                Debug.WriteLine($"Error ensuring columns exist: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the SQLite type for a .NET type
        /// </summary>
        private static string GetSqliteType(Type type) {
            if (type == typeof(int) || type == typeof(int?) ||
                type == typeof(long) || type == typeof(long?) ||
                type == typeof(bool) || type == typeof(bool?))
                return "INTEGER";

            if (type == typeof(float) || type == typeof(float?) ||
                type == typeof(double) || type == typeof(double?) ||
                type == typeof(decimal) || type == typeof(decimal?))
                return "REAL";

            if (type == typeof(byte[]))
                return "BLOB";

            if (type == typeof(DateTime) || type == typeof(DateTime?))
                return "TEXT";

            return "TEXT";
        }
    }
}
