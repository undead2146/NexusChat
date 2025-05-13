using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NexusChat.Core.Models;
using NexusChat.Services.Interfaces;

namespace NexusChat.Services.Interfaces
{
    /// <summary>
    /// Interface for the AI provider factory
    /// </summary>
    public interface IAIProviderFactory
    {
        /// <summary>
        /// Gets a default service when no specific model is selected
        /// </summary>
        IAIProviderService GetDefaultService();
        
        /// <summary>
        /// Gets a provider service for a specific model
        /// </summary>
        IAIProviderService GetProviderForModel(string providerName, string modelName);
        
        /// <summary>
        /// Gets all available providers
        /// </summary>
        List<string> GetAvailableProviders();
        
        /// <summary>
        /// Gets available providers that have valid API keys
        /// </summary>
        Task<List<string>> GetActiveProvidersAsync();
        
        /// <summary>
        /// Gets models available for a specific provider (async)
        /// </summary>
        Task<List<AIModel>> GetModelsForProviderAsync(string providerName);
        
        /// <summary>
        /// Gets models available for a specific provider (sync)
        /// </summary>
        List<AIModel> GetModelsForProvider(string providerName);
        
        /// <summary>
        /// Gets all available models from all providers
        /// </summary>
        Task<List<AIModel>> GetAllModelsAsync();
        
        /// <summary>
        /// Gets all available models (sync)
        /// </summary>
        List<AIModel> GetAllModels();
        
        /// <summary>
        /// Checks if a provider is available
        /// </summary>
        bool IsProviderAvailable(string providerName);
        
        /// <summary>
        /// Checks if a provider has a valid API key
        /// </summary>
        Task<bool> IsProviderActiveAsync(string providerName);
        
        /// <summary>
        /// Force refresh of cached models
        /// </summary>
        void ClearModelCache();
    }
}
