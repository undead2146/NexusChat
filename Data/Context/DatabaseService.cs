using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Core.Models;
using SQLite;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace NexusChat.Data.Context
{
    /// <summary>
    /// Service for managing database connections
    /// </summary>
    public class DatabaseService
    {
        private readonly SemaphoreSlim _initializationLock = new SemaphoreSlim(1, 1);
        private bool _isInitialized = false;
        
        private readonly string _databasePath;
        private SQLiteAsyncConnection _database;
        private SQLiteAsyncConnection _asyncConnection;
        private SQLiteConnection _syncConnection;
        private readonly object _initLock = new object();

        /// <summary>
        /// Gets the database connection, initialize if needed
        /// </summary>
        public SQLiteAsyncConnection Database 
        {
            get
            {
                if (_database == null)
                {
                    lock (_initLock)
                    {
                        if (_database == null)
                        {
                            _database = new SQLiteAsyncConnection(_databasePath, 
                                SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.SharedCache);
                        }
                    }
                }
                return _database;
            }
        }

        /// <summary>
        /// Database filename constant
        /// </summary>
        public const string DatabaseFilename = "nexus_chat.db3";
        
        /// <summary>
        /// Creates a new instance of DatabaseService
        /// </summary>
        public DatabaseService()
        {
            _databasePath = Path.Combine(FileSystem.AppDataDirectory, DatabaseFilename);
            Debug.WriteLine($"Database path: {_databasePath}");
            _database = new SQLiteAsyncConnection(_databasePath);
            
            // Initialize sync connection
            Connection = new SQLiteConnection(_databasePath);
        }
        
        /// <summary>
        /// Initialize SQLite database connection and create tables
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        public async Task Initialize(CancellationToken cancellationToken = default)
        {
            // If already initialized, return immediately 
            if (_isInitialized && _database != null)
                return;
            
            try
            {
                // Use lock to prevent multiple concurrent initializations
                await _initializationLock.WaitAsync(cancellationToken);
                
                // Check again in case another thread initialized while we were waiting
                if (_isInitialized && _database != null)
                    return;
                
                Debug.WriteLine("Initializing database");
                
                // Set up the connection and ensure it's open
                _database = new SQLiteAsyncConnection(_databasePath, 
                    SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.SharedCache);
                
                // Create tables if they don't exist
                await CreateTablesIfNeededAsync();
                
                _isInitialized = true;
                Debug.WriteLine("Database initialization completed successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing database: {ex}");
                throw;
            }
            finally
            {
                _initializationLock.Release();
            }
        }
        
        /// <summary>
        /// Initialize database safely with exception handling
        /// </summary>
        public async Task<bool> SafeInitializeAsync()
        {
            try
            {
                await Initialize();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SafeInitializeAsync error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Create database tables if they don't exist
        /// </summary>
        private async Task CreateTablesIfNeededAsync()
        {
            try
            {
                Debug.WriteLine("Creating database tables if needed");

                // Create tables if they don't exist
                await CreateOrUpdateTableAsync<Message>("Messages");
                await CreateOrUpdateTableAsync<User>("Users");
                await CreateOrUpdateTableAsync<Conversation>("Conversations");
                await CreateOrUpdateTableAsync<AIModel>("AIModels");
                
                Debug.WriteLine("Database schema verification completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating tables: {ex}");
                throw;
            }
        }
        
        /// <summary>
        /// Creates or updates a table schema to match the model
        /// </summary>
        /// <typeparam name="T">The model type</typeparam>
        /// <param name="tableName">The table name</param>
        private async Task CreateOrUpdateTableAsync<T>(string tableName) where T : new()
        {
            try
            {
                // Check if table exists
                var tableInfo = await _database.GetTableInfoAsync(tableName);
                if (tableInfo == null || tableInfo.Count == 0)
                {
                    Debug.WriteLine($"Creating {tableName} table");
                    await _database.CreateTableAsync<T>();
                    Debug.WriteLine($"{tableName} table created");
                    return;
                }
                
                Debug.WriteLine($"{tableName} table already exists, checking for schema updates");
                
                // Get existing columns
                var existingColumns = new HashSet<string>(
                    tableInfo.Select(c => c.Name), 
                    StringComparer.OrdinalIgnoreCase);
                
                // Get model properties that should be columns
                var modelType = typeof(T);
                var modelProperties = modelType.GetProperties()
                    .Where(p => !p.GetCustomAttributes(true)
                        .Any(a => a.GetType().Name == "IgnoreAttribute"))
                    .ToList();
                
                bool needsMigration = false;
                var missingColumns = new List<PropertyInfo>();
                
                // Find missing columns
                foreach (var prop in modelProperties)
                {
                    var columnAttr = prop.GetCustomAttribute<ColumnAttribute>();
                    var columnName = columnAttr?.Name ?? prop.Name;
                    
                    if (!existingColumns.Contains(columnName))
                    {
                        missingColumns.Add(prop);
                        needsMigration = true;
                    }
                }
                
                // If we have missing columns, alter the table
                if (needsMigration)
                {
                    Debug.WriteLine($"Adding {missingColumns.Count} missing columns to {tableName}");
                    
                    foreach (var prop in missingColumns)
                    {
                        var columnName = prop.GetCustomAttribute<ColumnAttribute>()?.Name ?? prop.Name;
                        var columnType = GetSqliteTypeName(prop.PropertyType);
                        
                        // Null constraint depends on if the type is nullable
                        var isNullable = IsNullableType(prop.PropertyType);
                        var nullConstraint = isNullable ? "" : " NOT NULL DEFAULT ''";
                        
                        var alterSql = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnType}{nullConstraint}";
                        Debug.WriteLine($"Executing: {alterSql}");
                        
                        await _database.ExecuteAsync(alterSql);
                    }
                    Debug.WriteLine($"Schema migration completed for {tableName}");
                }
                else
                {
                    Debug.WriteLine($"No schema changes needed for {tableName}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating schema for {tableName}: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Determines if a type is nullable
        /// </summary>
        private bool IsNullableType(Type type)
        {
            if (!type.IsValueType) return true; // Reference types are nullable
            return Nullable.GetUnderlyingType(type) != null; // Check for Nullable<T>
        }
        
        /// <summary>
        /// Maps .NET types to SQLite types
        /// </summary>
        private string GetSqliteTypeName(Type type)
        {
            // Handle nullable types
            type = Nullable.GetUnderlyingType(type) ?? type;
            
            if (type == typeof(int) || type == typeof(long) || type == typeof(bool) || 
                type == typeof(byte) || type == typeof(sbyte) || type == typeof(short))
                return "INTEGER";
                
            if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
                return "REAL";
                
            if (type == typeof(DateTime))
                return "TEXT"; // Store as ISO8601 strings
                
            if (type == typeof(Guid))
                return "TEXT"; // Store as strings
                
            if (type == typeof(byte[]))
                return "BLOB";
                
            return "TEXT"; // Default for strings and other types
        }
        
        /// <summary>
        /// Clear all data from the database for testing purposes
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        public async Task ClearAllData()
        {
            try
            {
                await Initialize();
                
                // Drop tables in reverse order of dependencies
                await _database.ExecuteAsync("DELETE FROM Messages");
                await _database.ExecuteAsync("DELETE FROM Conversations");
                await _database.ExecuteAsync("DELETE FROM AIModels");
                await _database.ExecuteAsync("DELETE FROM Users");
                
                Debug.WriteLine("All data cleared from database");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error clearing database: {ex.Message}");
                throw;
            }
        }


        /// <summary>
        /// Check if a table exists in the database
        /// </summary>
        /// <param name="tableName">Name of the table to check</param>
        /// <returns>True if table exists, false otherwise</returns>
        public async Task<bool> TableExistsAsync(string tableName)
        {
            try
            {
                await Initialize();
                var tableInfo = await _database.GetTableInfoAsync(tableName);
                return tableInfo != null && tableInfo.Count > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking table existence for {tableName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the SQLite async connection
        /// </summary>
        /// <returns>SQLiteAsyncConnection</returns>
        public SQLiteAsyncConnection GetAsyncConnection()
        {
            if (_asyncConnection == null)
            {
                // Create a new connection if one doesn't exist
                _asyncConnection = new SQLiteAsyncConnection(_databasePath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
                Debug.WriteLine($"Created new async database connection: {_databasePath}");
            }
            
            return _asyncConnection;
        }

        /// <summary>
        /// Gets the SQLite synchronous connection
        /// </summary>
        /// <returns>SQLiteConnection</returns>
        public SQLiteConnection GetSyncConnection()
        {
            if (_syncConnection == null)
            {
                // Create a new synchronous connection if one doesn't exist
                _syncConnection = new SQLiteConnection(_databasePath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
                Debug.WriteLine($"Created new sync database connection: {_databasePath}");
            }
            
            return _syncConnection;
        }
        
        /// <summary>
        /// Closes and disposes database connections
        /// </summary>
        public void CloseConnections()
        {
            try
            {
                _syncConnection?.Close();
                _syncConnection?.Dispose();
                _syncConnection = null;
                
                // For async connection, we can only dispose it
                _asyncConnection?.CloseAsync().Wait();
                _asyncConnection = null;
                
                Debug.WriteLine("Database connections closed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error closing database connections: {ex.Message}");
            }
        }

        /// <summary>
        /// Ensures the database is initialized
        /// </summary>
        public async Task EnsureInitializedAsync()
        {
            if (_isInitialized)
                return;

            lock (_initLock)
            {
                if (_isInitialized)
                    return;

                if (_database == null)
                {
                    // Create database connection
                    _database = new SQLiteAsyncConnection(_databasePath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
                }
            }

            // Create tables if they don't exist - outside the lock to avoid deadlocks during async operations
            await CreateTablesIfNeededAsync();

            _isInitialized = true;
        }

        /// <summary>
        /// Fast synchronous initialization without deadlocks
        /// </summary>
        public void InitializeSync()
        {
            if (_isInitialized)
                return;

            lock (_initLock)
            {
                if (_isInitialized)
                    return;

                try
                {
                    Debug.WriteLine("DatabaseService: Fast sync initialization");
                    
                    // Create connection immediately without async operations
                    _database = new SQLiteAsyncConnection(_databasePath, 
                        SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.SharedCache);
                    
                    // Mark as initialized - table creation can happen in background
                    _isInitialized = true;
                    
                    // Create tables in background without blocking
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await CreateTablesIfNeededAsync();
                            Debug.WriteLine("DatabaseService: Background table creation completed");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"DatabaseService: Background table creation error: {ex.Message}");
                        }
                    });
                    
                    Debug.WriteLine("DatabaseService: Sync initialization completed immediately");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"DatabaseService: Error in sync initialization: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// Gets the SQLite synchronous connection
        /// </summary>
        public SQLiteConnection Connection { get; private set; }

        /// <summary>
        /// Gets the SQLite async connection
        /// </summary>
        /// <returns>SQLiteAsyncConnection</returns>
        public SQLiteAsyncConnection GetConnection()
        {
            return _database;  // Assuming _database is the SQLiteAsyncConnection instance
        }

        /// <summary>
        /// Gets the SQLite connection asynchronously
        /// </summary>
        public async Task<SQLiteAsyncConnection> GetConnectionAsync()
        {
            await Initialize();
            return _database;
        }
    }
}
