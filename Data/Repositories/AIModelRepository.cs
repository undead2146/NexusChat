using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Core.Models;
using NexusChat.Data.Context;
using NexusChat.Data.Interfaces;
using SQLite;

namespace NexusChat.Data.Repositories
{
    /// <summary>
    /// Repository implementation for AI Model data access operations
    /// </summary>
    public class AIModelRepository : BaseRepository<AIModel>, IAIModelRepository
    {
        /// <summary>
        /// Initializes a new instance of AIModelRepository
        /// </summary>
        /// <param name="databaseService">The database service</param>
        public AIModelRepository(DatabaseService databaseService)
            : base(databaseService.GetConnection())
        {
            // Ensure table is created
            _database.CreateTableAsync<AIModel>().Wait();
        }

        /// <summary>
        /// Gets the default AI model
        /// </summary>
        /// <returns>The default AI model if found, null otherwise</returns>
        public async Task<AIModel> GetDefaultModelAsync()
        {
            try
            {
                // Query for a model with IsDefault flag set to true
                var defaultModel = await _database.Table<AIModel>()
                    .Where(m => m.IsDefault)
                    .FirstOrDefaultAsync();

                return defaultModel;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting default model: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets models by provider name
        /// </summary>
        /// <param name="providerName">Provider name to filter by</param>
        /// <returns>List of models for the provider</returns>
        public async Task<List<AIModel>> GetByProviderAsync(string providerName)
        {
            if (string.IsNullOrEmpty(providerName))
                return new List<AIModel>();

            try
            {
                return await _database.Table<AIModel>()
                    .Where(m => m.ProviderName == providerName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting models by provider: {ex.Message}");
                return new List<AIModel>();
            }
        }

        /// <summary>
        /// Sets a model as the default
        /// </summary>
        /// <param name="modelId">ID of the model to set as default</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> SetAsDefaultAsync(int modelId)
        {
            try
            {
                // Use a transaction to ensure data integrity
                await _database.RunInTransactionAsync(tran => 
                {
                    // First, unset IsDefault on all models
                    tran.Execute("UPDATE AIModels SET IsDefault = 0");
                    
                    // Then set IsDefault on the specified model
                    tran.Execute("UPDATE AIModels SET IsDefault = 1 WHERE Id = ?", modelId);
                });
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting default model: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Updates the API key for a model
        /// </summary>
        /// <param name="modelId">ID of the model</param>
        /// <param name="apiKey">New API key</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> UpdateApiKeyAsync(int modelId, string apiKey)
        {
            try
            {
                var model = await GetByIdAsync(modelId);
                if (model == null)
                    return false;
                
                model.ApiKey = apiKey;
                var result = await UpdateAsync(model);
                return result > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating API key: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets all AI models
        /// </summary>
        /// <returns>List of all AI models</returns>
        public async Task<List<AIModel>> GetAllModelsAsync()
        {
            try
            {
                return await GetAllAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting all models: {ex.Message}");
                return new List<AIModel>();
            }
        }

        /// <summary>
        /// Gets a model by provider name and model identifier
        /// </summary>
        /// <param name="providerName">Provider name</param>
        /// <param name="modelIdentifier">Model identifier/name</param>
        /// <returns>Matching model or null if not found</returns>
        public async Task<AIModel> GetModelByNameAsync(string providerName, string modelIdentifier)
        {
            try
            {
                return await _database.Table<AIModel>()
                    .Where(m => m.ProviderName == providerName && m.ModelName == modelIdentifier)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting model by name: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets the default model configuration
        /// </summary>
        /// <returns>Default model configuration or null if not found</returns>
        public async Task<ModelConfiguration> GetDefaultConfigurationAsync()
        {
            try
            {
                var defaultModel = await GetDefaultModelAsync();
                if (defaultModel == null)
                    return null;

                return new ModelConfiguration 
                {
                    ProviderName = defaultModel.ProviderName,
                    ModelIdentifier = defaultModel.ModelName,
                    IsDefault = true
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting default configuration: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Updates an AI model
        /// </summary>
        /// <param name="model">The model to update</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> UpdateModelAsync(AIModel model)
        {
            try
            {
                var result = await UpdateAsync(model);
                return result > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating model: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Adds a new model
        /// </summary>
        /// <param name="model">The model to add</param>
        /// <returns>ID of the new model, or -1 if failed</returns>
        public async Task<int> AddModelAsync(AIModel model)
        {
            try
            {
                return await AddAsync(model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding model: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// Toggles the favorite status of a model
        /// </summary>
        /// <param name="modelId">ID of the model to toggle favorite status</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> ToggleFavoriteAsync(int modelId)
        {
            try
            {
                // Get current favorite state
                var model = await GetByIdAsync(modelId);
                if (model == null) return false;

                // Toggle favorite state
                model.IsFavourite = !model.IsFavourite;

                // Update the model
                await _database.UpdateAsync(model);
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error toggling favorite status: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets all favorite models
        /// </summary>
        /// <returns>List of favorite models</returns>
        public async Task<List<AIModel>> GetFavoriteModelsAsync()
        {
            try
            {
                return await _database.Table<AIModel>()
                    .Where(m => m.IsFavourite)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting favorite models: {ex.Message}");
                return new List<AIModel>();
            }
        }
    }
}

