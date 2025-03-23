using SQLite;
using NexusChat.Models;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace NexusChat.Data
{
    /// <summary>
    /// Service for database operations
    /// </summary>
    public class DatabaseService
    {
        private readonly SQLiteAsyncConnection _database;
        private readonly string _databasePath;
        private bool _isInitialized = false;
        private bool _isInitializing = false;
        private readonly SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Creates a new instance of DatabaseService with the specified database path
        /// </summary>
        public DatabaseService()
        {
            _databasePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "nexuschat.db3");
            
            _database = new SQLiteAsyncConnection(_databasePath,
                SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
        }

        /// <summary>
        /// Gets the database connection
        /// </summary>
        public SQLiteAsyncConnection Database
        {
            get
            {
                if (!_isInitialized)
                    throw new InvalidOperationException("Database not initialized. Call Initialize first.");
                return _database;
            }
        }

        /// <summary>
        /// Initializes the database by creating tables
        /// </summary>
        public async Task Initialize(CancellationToken cancellationToken = default)
        {
            // Use a semaphore to prevent multiple simultaneous initialization attempts
            if (_isInitialized)
                return;
                
            await _initLock.WaitAsync(cancellationToken);
            
            try
            {
                if (_isInitialized)
                    return;
                    
                _isInitializing = true;
                Debug.WriteLine("Initializing database...");
                
                // Enable foreign keys
                await _database.ExecuteAsync("PRAGMA foreign_keys = ON");
                
                // Create tables
                await _database.CreateTableAsync<User>();
                await _database.CreateTableAsync<Conversation>();
                await _database.CreateTableAsync<Message>();
                await _database.CreateTableAsync<AIModel>();
                //await _database.CreateTableAsync<APIKey>();
                //await _database.CreateTableAsync<Setting>();
                
                // Set initialization flag
                _isInitialized = true;
                Debug.WriteLine("Database initialized successfully!");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing database: {ex}");
                throw; // Re-throw so callers know initialization failed
            }
            finally
            {
                _isInitializing = false;
                _initLock.Release();
            }
        }

        /// <summary>
        /// Creates a test user for development purposes
        /// </summary>
        public async Task<User> CreateTestUserIfNotExists()
        {
            await Initialize();
            
            // Check if test user exists
            var existingUser = await _database.Table<User>()
                .Where(u => u.Username == "testuser")
                .FirstOrDefaultAsync();

            if (existingUser != null)
                return existingUser;

            // Create test user
            var testUser = User.CreateTestUser();
            await _database.InsertAsync(testUser);
            return testUser;
        }
        
        /// <summary>
        /// Clears all data from the database tables
        /// </summary>
        public async Task ClearAllData(CancellationToken cancellationToken = default)
        {
            await Initialize(cancellationToken);
            
            try
            {
                // Delete in reverse order of dependency
                // Don't pass the cancellation token to ExecuteAsync - it doesn't support it
                await _database.ExecuteAsync("DELETE FROM Message");
                cancellationToken.ThrowIfCancellationRequested();
                
                await _database.ExecuteAsync("DELETE FROM Conversation");
                cancellationToken.ThrowIfCancellationRequested();
                
                await _database.ExecuteAsync("DELETE FROM User");
                cancellationToken.ThrowIfCancellationRequested();
                
                await _database.ExecuteAsync("DELETE FROM AIModel");
                
                Debug.WriteLine("All database data cleared");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error clearing database: {ex}");
                throw;
            }
        }
    }

    /// <summary>
    /// Interface for startup initialization
    /// </summary>
    public interface IStartupInitializer
    {
        Task Initialize();
    }

    /// <summary>
    /// Database initializer for startup
    /// </summary>
    public class DatabaseInitializer : IStartupInitializer
    {
        private readonly DatabaseService _databaseService;

        public DatabaseInitializer(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task Initialize()
        {
            await _databaseService.Initialize();
            await _databaseService.CreateTestUserIfNotExists();
        }
    }
}
