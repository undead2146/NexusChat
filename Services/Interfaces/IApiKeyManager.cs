using System.Threading.Tasks;

namespace NexusChat.Services.Interfaces
{
    /// <summary>
    /// Interface for managing API keys securely 
    /// </summary>
    public interface IApiKeyManager
    {
        /// <summary>
        /// Sets a custom API key for a specific service
        /// </summary>
        /// <param name="keyName">The name/identifier of the key</param>
        /// <param name="apiKey">The API key value</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> SetCustomApiKeyAsync(string keyName, string apiKey);
        
        /// <summary>
        /// Gets an API key by name
        /// </summary>
        /// <param name="keyName">The name/identifier of the key</param>
        /// <returns>The API key value, or null if not found</returns>
        string GetApiKey(string keyName);
        
        /// <summary>
        /// Validates the format of an API key
        /// </summary>
        /// <param name="apiKey">The API key to validate</param>
        /// <returns>True if the format is valid, false otherwise</returns>
        bool ValidateApiKeyFormat(string apiKey);
        
        /// <summary>
        /// Gets the appropriate key name for a provider and model combination
        /// </summary>
        /// <param name="providerName">The provider name</param>
        /// <param name="modelName">The model name</param>
        /// <returns>The key name to use for this provider/model</returns>
        string GetModelKeyFromNames(string providerName, string modelName);
        
        /// <summary>
        /// Resolves an API key for a model asynchronously with fallbacks
        /// </summary>
        /// <param name="modelName">The model name</param>
        /// <param name="providerName">The provider name</param>
        /// <returns>The API key or null if not found</returns>
        Task<string> ResolveApiKeyAsync(string modelName, string providerName);
    }
}
