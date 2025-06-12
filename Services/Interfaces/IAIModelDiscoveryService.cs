using System.Collections.Generic;
using System.Threading.Tasks;
using NexusChat.Core.Models;

namespace NexusChat.Services.Interfaces
{
    /// <summary>
    /// Service for discovering AI models from various providers
    /// </summary>
    public interface IAIModelDiscoveryService
    {
        /// <summary>
        /// Discovers all models from all available providers
        /// </summary>
        Task<List<AIModel>> DiscoverAllModelsAsync();

        /// <summary>
        /// Discovers models for specific providers
        /// </summary>
        Task<List<AIModel>> DiscoverModelsForProvidersAsync(List<string> providerNames);

        /// <summary>
        /// Discovers models for a specific provider
        /// </summary>
        Task<List<AIModel>> DiscoverProviderModelsAsync(string providerName);

        /// <summary>
        /// Clears the discovery cache
        /// </summary>
        void ClearCache();
    }
}
