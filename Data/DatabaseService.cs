using SQLite;
using NexusChat.Models;
using System.Threading.Tasks;
using System;
using System.IO;

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
        public async Task Initialize()
        {
            if (_isInitialized)
                return;

            // Create tables
            await _database.CreateTableAsync<User>();

            // Set initialization flag
            _isInitialized = true;
        }

        /// <summary>
        /// Creates a test user for development purposes
        /// </summary>
        public async Task<User> CreateTestUserIfNotExists()
        {
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
