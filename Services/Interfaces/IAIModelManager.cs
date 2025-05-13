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
        /// Loads models from all providers
        /// </summary>
        Task<bool> LoadModelsFromAllProvidersAsync();
    }
}
