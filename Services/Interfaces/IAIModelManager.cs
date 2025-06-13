using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NexusChat.Core.Models;

namespace NexusChat.Services.Interfaces
{
    /// <summary>
    /// Interface for the AI model manager service
    /// </summary>
    public interface IAIModelManager : IStartupInitializer
    {
        /// <summary>
        /// Gets the current model
        /// </summary>
        AIModel CurrentModel { get; }
        
        /// <summary>
        /// Event that fires when the current model changes
        /// </summary>
        event EventHandler<AIModel> CurrentModelChanged;
        
        /// <summary>
        /// Gets all available models
        /// </summary>
        Task<List<AIModel>> GetAllModelsAsync();
        
        /// <summary>
        /// Gets all models for a specific provider
        /// </summary>
        Task<List<AIModel>> GetProviderModelsAsync(string providerName);
        
        /// <summary>
        /// Gets all favorite models
        /// </summary>
        Task<List<AIModel>> GetFavoriteModelsAsync();
        
        /// <summary>
        /// Sets a model as the current selected model
        /// </summary>
        Task<bool> SetCurrentModelAsync(AIModel model);
        
        /// <summary>
        /// Sets a model as default for its provider
        /// </summary>
        Task<bool> SetDefaultModelAsync(string providerName, string modelName);
        
        /// <summary>
        /// Sets a model's favorite status
        /// </summary>
        Task<bool> SetFavoriteStatusAsync(string providerName, string modelName, bool isFavorite);
        
        /// <summary>
        /// Records usage of a model
        /// </summary>
        Task<bool> RecordModelUsageAsync(string providerName, string modelName);
        
        /// <summary>
        /// Discovers and loads models from all available sources
        /// </summary>
        Task<bool> DiscoverAndLoadModelsAsync();
        
        /// <summary>
        /// Discovers and loads models for a specific provider
        /// </summary>
        Task<bool> DiscoverAndLoadProviderModelsAsync(string providerName);
        
        /// <summary>
        /// Processes a list of discovered models and adds new ones to the database
        /// </summary>
        /// <param name="discoveredModels">List of models discovered from providers</param>
        /// <returns>True if new models were added, false otherwise</returns>
        Task<bool> ProcessDiscoveredModelsAsync(List<AIModel> discoveredModels);
        
        /// <summary>
        /// Handles cleanup when a provider is removed
        /// </summary>
        /// <param name="providerName">Name of the provider that was removed</param>
        Task OnProviderRemovedAsync(string providerName);
        
        /// <summary>
        /// Clears the current model selection
        /// </summary>
        Task ClearCurrentModelAsync();
        
        /// <summary>
        /// Refreshes the model cache
        /// </summary>
        Task RefreshModelCacheAsync();
    }
}
