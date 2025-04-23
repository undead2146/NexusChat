using System.Collections.Generic;
using System.Threading.Tasks;
using NexusChat.Core.Models;

namespace NexusChat.Services.Interfaces
{
    /// <summary>
    /// Service for loading model configurations from various sources
    /// </summary>
    public interface IModelLoaderService : IStartupInitializer
    {
        /// <summary>
        /// Gets all available model configurations
        /// </summary>
        Task<List<ModelConfiguration>> LoadModelsAsync();
        
        /// <summary>
        /// Gets all available model configurations with refresh option
        /// </summary>
        Task<List<ModelConfiguration>> LoadModelConfigurationsAsync(bool forceRefresh = false);
        
        /// <summary>
        /// Gets a specific model configuration
        /// </summary>
        Task<ModelConfiguration> GetModelConfigurationAsync(string providerName, string modelName);
        
        /// <summary>
        /// Loads provider configurations from database
        /// </summary>
        Task<List<ProviderConfiguration>> LoadProvidersAsync();
        
        /// <summary>
        /// Saves a model configuration
        /// </summary>
        Task SaveModelConfigurationAsync(ModelConfiguration config);
        
        /// <summary>
        /// Extracts models from environment variables
        /// </summary>
        Task<List<AIModel>> ExtractModelsFromEnvironmentAsync();
        
        /// <summary>
        /// Normalizes provider names for consistency
        /// </summary>
        string NormalizeProviderName(string providerName);
        
        /// <summary>
        /// Creates default model configurations
        /// </summary>
        Task<List<ModelConfiguration>> CreateFallbackConfigurationsAsync();
        
        /// <summary>
        /// Clears any cached configurations
        /// </summary>
        void ClearCache();
    }
}
