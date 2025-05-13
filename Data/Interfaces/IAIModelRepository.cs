using System.Collections.Generic;
using System.Threading.Tasks;
using NexusChat.Core.Models;

namespace NexusChat.Data.Interfaces
{
    /// <summary>
    /// Interface for AI model repository operations
    /// </summary>
    public interface IAIModelRepository : IRepository<AIModel>
    {
        /// <summary>
        /// Gets a model by its provider and name
        /// </summary>
        Task<AIModel> GetModelByNameAsync(string providerName, string modelName);
        
        /// <summary>
        /// Gets all models for a specific provider
        /// </summary>
        Task<List<AIModel>> GetByProviderAsync(string providerName);
        
        /// <summary>
        /// Gets all favorite models
        /// </summary>
        Task<List<AIModel>> GetFavoriteModelsAsync();
        
        /// <summary>
        /// Gets the currently selected model
        /// </summary>
        Task<AIModel> GetCurrentModelAsync();
        
        /// <summary>
        /// Gets all default models
        /// </summary>
        Task<List<AIModel>> GetDefaultModelsAsync();
        
        /// <summary>
        /// Gets all active models (with valid API keys)
        /// </summary>
        Task<List<AIModel>> GetActiveModelsAsync();
        
        /// <summary>
        /// Sets a model as the current selected model
        /// </summary>
        Task<bool> SetCurrentModelAsync(AIModel model);
        
        /// <summary>
        /// Sets a model as default for its provider
        /// </summary>
        Task<bool> SetAsDefaultAsync(string providerName, string modelName);
        
        /// <summary>
        /// Sets a model's favorite status
        /// </summary>
        Task<bool> SetFavoriteStatusAsync(string providerName, string modelName, bool isFavorite);
        
        /// <summary>
        /// Records usage of a model
        /// </summary>
        Task<bool> RecordUsageAsync(string providerName, string modelName);
    }
}
