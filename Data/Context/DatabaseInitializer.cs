using System;
using System.Threading.Tasks;
using NexusChat.Core.Models;
using NexusChat.Services.Interfaces;
using SQLite;
using System.Diagnostics;

namespace NexusChat.Data.Context
{
    /// <summary>
    /// Initializes the database and creates required tables
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
        /// Initializes the database
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                Debug.WriteLine("Initializing database...");
                var connection = _dbService.GetAsyncConnection();
                
                // Create tables for all model classes
                await connection.CreateTableAsync<AIModel>();
                await connection.CreateTableAsync<User>();
                await connection.CreateTableAsync<Conversation>();
                await connection.CreateTableAsync<Message>();
                
                Debug.WriteLine("Database initialized successfully");
                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing database: {ex.Message}");
                throw; 
            }
        }
    }
}
