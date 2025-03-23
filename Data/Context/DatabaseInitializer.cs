using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NexusChat.Data.Context
{
    /// <summary>
    /// Database initializer for startup
    /// </summary>
    public class DatabaseInitializer : IStartupInitializer
    {
        private readonly DatabaseService _databaseService;

        public DatabaseInitializer(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        }

        /// <summary>
        /// Initializes the database during app startup
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                Debug.WriteLine("Initializing database at startup...");
                await _databaseService.SafeInitializeAsync();
                Debug.WriteLine("Database initialization completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during database initialization: {ex.Message}");
                // Just log the error, don't rethrow - we don't want to crash the app on startup
            }
        }
    }
}
