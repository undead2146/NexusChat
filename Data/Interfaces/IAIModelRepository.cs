using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Core.Models;

namespace NexusChat.Data.Interfaces
{
    /// <summary>
    /// Repository interface for AI Model data access operations
    /// </summary>
    public interface IAIModelRepository : IRepository<AIModel>
    {
        /// <summary>
        /// Gets the default AI model
        /// </summary>
        /// <returns>The default AI model</returns>
        Task<AIModel> GetDefaultModelAsync();

        /// <summary>
        /// Gets AI models by provider
        /// </summary>
        /// <param name="providerName">The provider name</param>
        /// <returns>List of AI models for the provider</returns>
        Task<List<AIModel>> GetByProviderAsync(string providerName);

        /// <summary>
        /// Sets an AI model as the default
        /// </summary>
        /// <param name="modelId">The model ID to set as default</param>
        /// <returns>True if successful</returns>
        Task<bool> SetAsDefaultAsync(int modelId);
        
        /// <summary>
        /// Sets an AI model as the default (alternative naming)
        /// </summary>
        /// <param name="modelId">The model ID to set as default</param>
        /// <returns>True if successful</returns>
        Task<bool> SetDefaultModelAsync(int modelId) => SetAsDefaultAsync(modelId);

        /// <summary>
        /// Updates the model API key
        /// </summary>
        /// <param name="modelId">Model ID</param>
        /// <param name="apiKey">API key</param>
        /// <returns>True if successful</returns>
        Task<bool> UpdateApiKeyAsync(int modelId, string apiKey);
        
        /// <summary>
        /// Gets all AI models
        /// </summary>
        /// <returns>List of all models</returns>
        Task<List<AIModel>> GetAllModelsAsync();
        
        /// <summary>
        /// Gets a model by provider name and model name
        /// </summary>
        /// <param name="providerName">Provider name</param>
        /// <param name="modelName">Model name</param>
        /// <returns>Model if found, null otherwise</returns>
        Task<AIModel> GetModelByNameAsync(string providerName, string modelName);
        
        /// <summary>
        /// Gets a model by ID - provides compatibility with IRepository<AIModel>
        /// </summary>
        /// <param name="id">Model ID</param>
        /// <returns>Model if found, null otherwise</returns>
        Task<AIModel> GetModelByIdAsync(int id) => GetByIdAsync(id);
        
        /// <summary>
        /// Gets the default model configuration
        /// </summary>
        /// <returns>Default model configuration</returns>
        Task<ModelConfiguration> GetDefaultConfigurationAsync();
        
        /// <summary>
        /// Updates an AI model
        /// </summary>
        /// <param name="model">Model to update</param>
        /// <returns>True if successful</returns>
        Task<bool> UpdateModelAsync(AIModel model);
        
        /// <summary>
        /// Adds a new AI model
        /// </summary>
        /// <param name="model">Model to add</param>
        /// <returns>Model ID if successful, -1 otherwise</returns>
        Task<int> AddModelAsync(AIModel model);

        /// <summary>
        /// Toggles the favorite status of a model
        /// </summary>
        Task<bool> ToggleFavoriteAsync(int modelId);

        /// <summary>
        /// Gets all favorite models
        /// </summary>
        Task<List<AIModel>> GetFavoriteModelsAsync();
    }
}
