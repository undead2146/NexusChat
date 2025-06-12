using System;
using System.Threading.Tasks;
using NexusChat.Core.Models;
using NexusChat.Services.Interfaces;
using SQLite;
using System.Diagnostics;

namespace NexusChat.Data.Context
{
    /// <summary>
    /// Initializes the database and seeds initial data if needed
    /// </summary>
    public class DatabaseInitializer : IStartupInitializer
    {
        private readonly DatabaseService _dbService;
        
        /// <summary>
        /// Creates a new instance of DatabaseInitializer
        /// </summary>
        public DatabaseInitializer(DatabaseService dbService)
        {
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
        }
        
        /// <summary>
        /// Initializes the database and seeds initial data
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                Debug.WriteLine("Initializing database...");
                
                // Initialize the database structure
                await _dbService.Initialize();
                
                // Seed initial data if needed
                await SeedInitialDataAsync();
                
                Debug.WriteLine("Database initialized successfully");
                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing database: {ex.Message}");
                throw; 
            }
        }
        
        /// <summary>
        /// Seeds initial data if database is empty
        /// </summary>
        private async Task SeedInitialDataAsync()
        {
            var connection = _dbService.GetAsyncConnection();
            
            // Check if we need to seed data
            bool needsSeedData = await connection.Table<User>().CountAsync() == 0;
            
            if (needsSeedData)
            {
                Debug.WriteLine("Seeding initial data...");
                
                // Create default user if none exists
                var defaultUser = new User
                {
                    Username = "default",
                    DisplayName = "Default User",
                    CreatedAt = DateTime.UtcNow
                };
                
                await connection.InsertAsync(defaultUser);
                Debug.WriteLine("Seeded default user");
            }
        }
    }
}
