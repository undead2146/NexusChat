using System.Collections.Generic;
using System.Threading.Tasks;

namespace NexusChat.Services.Interfaces
{
    /// <summary>
    /// Interface for secure API key storage
    /// </summary>
    public interface IApiKeyProvider
    {
        /// <summary>
        /// Initializes the provider
        /// </summary>
        Task InitializeAsync();
        
        /// <summary>
        /// Gets an API key by name
        /// </summary>
        /// <param name="keyName">Name of the key</param>
        /// <returns>API key if found, otherwise null</returns>
        Task<string> GetApiKeyAsync(string keyName);
        
        /// <summary>
        /// Gets an API key synchronously (for immediate access)
        /// </summary>
        /// <param name="keyName">Name of the key</param>
        /// <returns>API key if found, otherwise null</returns>
        string GetApiKey(string keyName);
        
        /// <summary>
        /// Gets all available API keys
        /// </summary>
        /// <returns>Dictionary of key names and values</returns>
        Dictionary<string, string> GetAllApiKeys();
        
        /// <summary>
        /// Gets API keys with a specific prefix
        /// </summary>
        /// <param name="prefix">Prefix to filter by</param>
        /// <returns>Dictionary of matching key names and values</returns>
        Dictionary<string, string> GetApiKeysWithPrefix(string prefix);
        
        /// <summary>
        /// Saves an API key
        /// </summary>
        /// <param name="keyName">Name of the key</param>
        /// <param name="apiKey">API key to save</param>
        /// <returns>True if successful, otherwise false</returns>
        Task<bool> SaveApiKeyAsync(string keyName, string apiKey);
        
        /// <summary>
        /// Deletes an API key
        /// </summary>
        /// <param name="keyName">Name of the key to delete</param>
        /// <returns>True if successful, otherwise false</returns>
        Task<bool> DeleteApiKeyAsync(string keyName);
    }
}
