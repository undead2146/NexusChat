using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Core.Models;

namespace NexusChat.Data.Interfaces
{
    /// <summary>
    /// Repository for AI models
    /// </summary>
    public interface IAIModelRepository : IRepository<AIModel>
    {
        /// <summary>
        /// Gets available models (where IsAvailable = true)
        /// </summary>
        Task<List<AIModel>> GetAvailableModelsAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets models by availability status
        /// </summary>
        Task<List<AIModel>> GetModelsByAvailabilityAsync(bool isAvailable, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets models with API key configuration in the database
        /// Note: This doesn't check if those API keys are currently valid at runtime
        /// </summary>
        Task<List<AIModel>> GetModelsWithApiKeyConfigAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets models by provider name
        /// </summary>
        Task<List<AIModel>> GetByProviderAsync(string providerName, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets a model by provider name and model name
        /// </summary>
        Task<AIModel> GetModelByNameAsync(string providerName, string modelName, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets the currently selected model
        /// </summary>
        Task<AIModel> GetCurrentModelAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets all favorite models
        /// </summary>
        Task<List<AIModel>> GetFavoriteModelsAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets all default models (one per provider)
        /// </summary>
        Task<List<AIModel>> GetDefaultModelsAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Sets a model as the currently selected model
        /// </summary>
        Task<bool> SetCurrentModelAsync(AIModel model, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Sets a model as default for its provider
        /// </summary>
        Task<bool> SetAsDefaultAsync(string providerName, string modelName, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Sets a model's favorite status
        /// </summary>
        Task<bool> SetFavoriteStatusAsync(string providerName, string modelName, bool isFavorite, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Records usage of a model
        /// </summary>
        Task<bool> RecordUsageAsync(string providerName, string modelName, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets active models (models that are available and have API keys configured)
        /// </summary>
        Task<List<AIModel>> GetActiveModelsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes all models for a specific provider
        /// </summary>
        Task<int> DeleteModelsByProviderAsync(string providerName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if any models exist for a specific provider
        /// </summary>
        Task<bool> HasModelsForProviderAsync(string providerName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets count of models for a specific provider
        /// </summary>
        Task<int> GetModelCountByProviderAsync(string providerName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes database duplicates
        /// </summary>
        Task<int> CleanupDuplicatesAsync();
        
        /// <summary>
        /// Clears cached data for a specific provider
        /// </summary>
        Task ClearProviderCacheAsync(string providerName, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets all models from the database
        /// </summary>
        Task<List<AIModel>> GetAllModelsAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Clears the current model selection
        /// </summary>
        Task<bool> ClearCurrentModelAsync(CancellationToken cancellationToken = default);
    }
}
