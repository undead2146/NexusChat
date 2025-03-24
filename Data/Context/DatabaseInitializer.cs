using NexusChat.Core.Models;
using NexusChat.Services.Interfaces;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NexusChat.Data.Context
{
    /// <summary>
    /// Initializes the database and ensures all required tables are created
    /// </summary>
    public class DatabaseInitializer : IStartupInitializer
    {
        private readonly DatabaseService _databaseService;

        /// <summary>
        /// Initializes a new instance of the DatabaseInitializer class
        /// </summary>
        public DatabaseInitializer(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        }

        /// <summary>
        /// Initialize the database with required tables
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                Debug.WriteLine("DatabaseInitializer: Initializing database...");
                
                await _databaseService.Initialize();
                
                // Create tables for our model classes
                await _databaseService.Database.CreateTablesAsync(
                    SQLite.CreateFlags.None,
                    typeof(User),
                    typeof(Conversation),
                    typeof(Message),
                    typeof(AIModel)
                );
                
                await InitializeDefaultModelsAsync();
                
                Debug.WriteLine("DatabaseInitializer: Database initialization complete");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DatabaseInitializer: Error initializing database: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Initialize default AI models if none exist
        /// </summary>
        private async Task InitializeDefaultModelsAsync()
        {
            try
            {
                var modelCount = await _databaseService.Database.Table<AIModel>().CountAsync();
                
                if (modelCount == 0)
                {
                    Debug.WriteLine("DatabaseInitializer: Creating default AI models");
                    
                    var defaultModels = new[]
                    {
                        new AIModel
                        {
                            ProviderName = "OpenAI",
                            ModelName = "GPT-4 Turbo",
                            Description = "OpenAI's most advanced model, optimized for both quality and speed",
                            MaxTokens = 8192,
                            DefaultTemperature = 0.7f,
                            IsAvailable = true
                        },
                        new AIModel
                        {
                            ProviderName = "Anthropic",
                            ModelName = "Claude 3 Opus",
                            Description = "Anthropic's most capable model for complex tasks requiring deep analysis",
                            MaxTokens = 100000,
                            DefaultTemperature = 0.5f,
                            IsAvailable = true
                        },
                        new AIModel
                        {
                            ProviderName = "NexusChat",
                            ModelName = "Dummy AI Model",
                            Description = "A simple model for testing and development",
                            MaxTokens = 2048,
                            DefaultTemperature = 0.7f,
                            IsAvailable = true
                        }
                    };
                    
                    foreach (var model in defaultModels)
                    {
                        await _databaseService.Database.InsertAsync(model);
                    }
                    
                    Debug.WriteLine("DatabaseInitializer: Default AI models created");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DatabaseInitializer: Error creating default models: {ex.Message}");
            }
        }
    }
}
