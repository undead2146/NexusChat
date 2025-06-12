using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NexusChat.Core.Models;
using NexusChat.Services.Interfaces;

namespace NexusChat.Services.Interfaces
{
    /// <summary>
    /// Factory for creating AI provider services and managing model discovery
    /// </summary>
    public interface IAIProviderFactory
    {
        
        /// <summary>
        /// Gets a provider service for a specific model
        /// </summary>
        Task<IAIProviderService> GetProviderForModelAsync(string providerName, string modelName);
        
        /// <summary>
        /// Gets available providers that have valid API keys
        /// </summary>
        Task<List<string>> GetActiveProvidersAsync();
        
        /// <summary>
        /// Gets available providers synchronously using cache
        /// </summary>
        List<string> GetActiveProvidersSync();
        
        /// <summary>
        /// Checks if a provider has a valid API key
        /// </summary>
        Task<bool> IsProviderActiveAsync(string providerName);
        
        /// <summary>
        /// Checks if a provider has a valid API key synchronously using cache
        /// </summary>
        bool IsProviderActiveSync(string providerName);
        
        /// <summary>
        /// Discovers all available models
        /// </summary>
        Task<List<AIModel>> DiscoverAllModelsAsync();
        
        /// <summary>
        /// Force refresh of cached models and API key availability
        /// </summary>
        Task ClearModelCacheAsync();
        
        /// <summary>
        /// Force refresh of cached models (synchronous version)
        /// </summary>
        void ClearModelCache();
    }
}
