using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace NexusChat.Services.Interfaces
{
    /// <summary>
    /// Interface for high-level API key management with business logic
    /// </summary>
    public interface IApiKeyManager
    {
        /// <summary>
        /// Occurs when an API key is changed
        /// </summary>
        event EventHandler<string> ApiKeyChanged;
        
        /// <summary>
        /// Initializes the API key manager
        /// </summary>
        Task InitializeAsync();
        
        /// <summary>
        /// Resolves an API key for a provider and optional model with fallback logic
        /// </summary>
        /// <param name="providerName">Provider name (e.g., "Groq")</param>
        /// <param name="modelName">Optional model name for model-specific keys</param>
        /// <returns>Resolved API key or null if not found</returns>
        Task<string> ResolveApiKeyAsync(string providerName, string modelName = null);
        
        /// <summary>
        /// Gets an API key for a specific provider (legacy method)
        /// </summary>
        Task<string> GetApiKeyAsync(string providerName);
        
        /// <summary>
        /// Saves an API key for a provider
        /// </summary>
        Task<bool> SaveProviderApiKeyAsync(string providerName, string apiKey);
        
        /// <summary>
        /// Saves a model-specific API key
        /// </summary>
        Task<bool> SaveModelSpecificApiKeyAsync(string providerName, string modelName, string apiKey);
        
        /// <summary>
        /// Deletes an API key for a provider
        /// </summary>
        Task<bool> DeleteProviderApiKeyAsync(string providerName);
        
        /// <summary>
        /// Checks if an API key exists and is usable for a provider
        /// </summary>
        Task<bool> HasActiveApiKeyAsync(string providerName);
        
        /// <summary>
        /// Legacy method - use HasActiveApiKeyAsync instead
        /// </summary>
        Task<bool> HasApiKeyAsync(string providerName);
        
        /// <summary>
        /// Gets the API key for a specific model (legacy method)
        /// </summary>
        Task<string> GetModelApiKeyAsync(string providerName, string modelName);
        
        /// <summary>
        /// Validates API key format
        /// </summary>
        bool IsValidApiKeyFormat(string apiKey);
        
        /// <summary>
        /// Gets the environment variable name for a provider
        /// </summary>
        string GetProviderLevelKeyName(string providerName);
        
        /// <summary>
        /// Gets the environment variable name for a specific model
        /// </summary>
        string GetModelSpecificKeyName(string providerName, string modelName);

        /// <summary>
        /// Fast synchronous API key availability check using cache
        /// </summary>
        bool HasActiveApiKeySync(string providerName);

        /// <summary>
        /// Gets all actively available providers synchronously using cache
        /// </summary>
        List<string> GetActiveProvidersSync();

        /// <summary>
        /// Gets all actively available providers
        /// </summary>
        Task<List<string>> GetActiveProvidersAsync();

        /// <summary>
        /// Forces a refresh of the availability cache
        /// </summary>
        Task RefreshAvailabilityCacheAsync();
    }
}
