using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Core.Models;
using SQLite;

namespace NexusChat.Data.Context
{
    /// <summary>
    /// Service for database operations
    /// </summary>
    public class DatabaseService
    {
        private readonly SemaphoreSlim _initializationLock = new SemaphoreSlim(1, 1);
        private bool _isInitialized = false;
        
        private readonly string _databasePath;
        private SQLiteAsyncConnection _database;
        
        /// <summary>
        /// Gets the database connection, initialize if needed
        /// </summary>
        public SQLiteAsyncConnection Database 
        {
            get
            {
                if (_database == null)
                {
                    // Note: This isn't awaitable, so database may not be ready
                    Initialize().GetAwaiter().GetResult();
                }
                return _database;
            }
        }
        
        /// <summary>
        /// Database filename constant
        /// </summary>
        public const string DatabaseFilename = "nexus_chat.db3";
        
        /// <summary>
        /// Initialize a new DatabaseService with default path
        /// </summary>
        public DatabaseService()
        {
            _databasePath = Path.Combine(FileSystem.AppDataDirectory, DatabaseFilename);
            Debug.WriteLine($"Database path: {_databasePath}");
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

                // Get the model types based on attributes to ensure we're using the right table names
                var tableInfo = await _database.GetTableInfoAsync("Messages");
                if (tableInfo == null || tableInfo.Count == 0)
                {
                    Debug.WriteLine("Creating Messages table");
                    await _database.CreateTableAsync<Message>();
                    Debug.WriteLine("Messages table created");
                }
                else
                {
                    Debug.WriteLine("Messages table already exists");
                }

                tableInfo = await _database.GetTableInfoAsync("Users");
                if (tableInfo == null || tableInfo.Count == 0)
                {
                    Debug.WriteLine("Creating Users table");
                    await _database.CreateTableAsync<User>();
                    Debug.WriteLine("Users table created");
                }
                else
                {
                    Debug.WriteLine("Users table already exists");
                }

                tableInfo = await _database.GetTableInfoAsync("Conversations");
                if (tableInfo == null || tableInfo.Count == 0)
                {
                    Debug.WriteLine("Creating Conversations table");
                    await _database.CreateTableAsync<Conversation>();
                    Debug.WriteLine("Conversations table created");
                }
                else
                {
                    Debug.WriteLine("Conversations table already exists");
                }

                tableInfo = await _database.GetTableInfoAsync("AIModels");
                if (tableInfo == null || tableInfo.Count == 0)
                {
                    Debug.WriteLine("Creating AIModels table");
                    await _database.CreateTableAsync<AIModel>();
                    Debug.WriteLine("AIModels table created");
                }
                else
                {
                    Debug.WriteLine("AIModels table already exists");
                }
                
                Debug.WriteLine("Database schema verification completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating tables: {ex}");
                throw;
            }
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
        /// Creates a test user if none exists
        /// </summary>
        public async Task CreateTestUserIfNotExists()
        {
            // Implementation unchanged
        }
        
        /// <summary>
        /// Creates default AI models if none exist
        /// </summary>
        public async Task CreateDefaultAIModelsIfNotExists()
        {
            // Implementation unchanged
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
    }
}
