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
        private const string DatabaseFilename = "nexuschat.db3";
        private static readonly SQLiteAsyncConnection _database;
        private static bool _isInitialized = false;
        private static readonly SemaphoreSlim _initializationLock = new SemaphoreSlim(1, 1);

        static DatabaseService()
        {
            try
            {
                // Get the database path
                var basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var dbPath = Path.Combine(basePath, DatabaseFilename);
                
                Debug.WriteLine($"Database path: {dbPath}");
                
                // Create the connection with write ahead logging for better concurrency
                _database = new SQLiteAsyncConnection(dbPath, SQLiteOpenFlags.Create | 
                                                             SQLiteOpenFlags.ReadWrite | 
                                                             SQLiteOpenFlags.SharedCache);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating database connection: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets the database connection
        /// </summary>
        public SQLiteAsyncConnection Database => _database;

        /// <summary>
        /// Initializes the database by creating tables
        /// </summary>
        public async Task Initialize(CancellationToken cancellationToken = default)
        {
            if (_isInitialized)
                return;
            
            await _initializationLock.WaitAsync(cancellationToken);
            
            try
            {
                if (_isInitialized)
                    return;
                
                // Create tables for all our models
                await _database.CreateTableAsync<User>(CreateFlags.None);
                await _database.CreateTableAsync<Conversation>(CreateFlags.None);
                await _database.CreateTableAsync<Message>(CreateFlags.None);
                await _database.CreateTableAsync<AIModel>(CreateFlags.None);
                
                // Create test data if needed
                await CreateTestUserIfNotExists();
                await CreateDefaultAIModelsIfNotExists();
                
                _isInitialized = true;
                Debug.WriteLine("Database initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing database: {ex.Message}");
                throw;
            }
            finally
            {
                _initializationLock.Release();
            }
        }
        
        /// <summary>
        /// Ensures the database is initialized safely
        /// </summary>
        public async Task SafeInitializeAsync()
        {
            try
            {
                await Initialize();
                Debug.WriteLine("Database initialized safely");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to initialize database: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Creates a test user for development purposes
        /// </summary>
        private async Task CreateTestUserIfNotExists()
        {
            try
            {
                // Check if any user exists
                var userCount = await _database.Table<User>().CountAsync();
                if (userCount == 0)
                {
                    var testUser = User.CreateTestUser();
                    await _database.InsertAsync(testUser);
                    Debug.WriteLine("Created test user");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating test user: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Creates default AI models for development purposes
        /// </summary>
        private async Task CreateDefaultAIModelsIfNotExists()
        {
            try
            {
                // Check if any models exist
                var modelCount = await _database.Table<AIModel>().CountAsync();
                if (modelCount == 0)
                {
                    var models = new[]
                    {
                        new AIModel 
                        { 
                            ModelName = "GPT-4 Turbo", 
                            ProviderName = "OpenAI",
                            IsAvailable = true,
                            MaxTokens = 4096,
                            DefaultTemperature = 0.7f
                        },
                        new AIModel 
                        { 
                            ModelName = "Claude 2", 
                            ProviderName = "Anthropic",
                            IsAvailable = true,
                            MaxTokens = 8192,
                            DefaultTemperature = 0.5f
                        },
                        new AIModel 
                        { 
                            ModelName = "Llama 2", 
                            ProviderName = "Meta",
                            IsAvailable = false,
                            MaxTokens = 4096,
                            DefaultTemperature = 0.6f
                        }
                    };
                    
                    foreach (var model in models)
                    {
                        await _database.InsertAsync(model);
                    }
                    
                    Debug.WriteLine("Created default AI models");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating default AI models: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Clears all data from the database tables
        /// </summary>
        public async Task ClearAllData(CancellationToken cancellationToken = default)
        {
            await _initializationLock.WaitAsync(cancellationToken);
            
            try
            {
                // Delete data in order to avoid foreign key constraints
                Debug.WriteLine("Clearing all data from database...");
                
                // First delete messages, which reference conversations
                await _database.ExecuteAsync("DELETE FROM Messages");
                Debug.WriteLine("Cleared Messages table");
                
                // Then delete conversations, which reference users and models
                await _database.ExecuteAsync("DELETE FROM Conversations");
                Debug.WriteLine("Cleared Conversations table");
                
                // We'll keep the users and AI models, but you can uncomment these if needed
                // await _database.ExecuteAsync("DELETE FROM Users");
                // await _database.ExecuteAsync("DELETE FROM AIModels");
                
                Debug.WriteLine("All data cleared from database");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error clearing database: {ex.Message}");
                throw;
            }
            finally
            {
                _initializationLock.Release();
            }
        }
    }
}
