using System;
using System.Threading.Tasks;

namespace NexusChat.Services.Interfaces
{
    /// <summary>
    /// Interface for API key management
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
        /// Gets an API key for a specific provider
        /// </summary>
        Task<string> GetApiKeyAsync(string providerName);
        
        /// <summary>
        /// Saves an API key for a provider
        /// </summary>
        Task<bool> SaveApiKeyAsync(string providerName, string apiKey);
        
        /// <summary>
        /// Deletes an API key for a provider
        /// </summary>
        Task<bool> DeleteApiKeyAsync(string providerName);
        
        /// <summary>
        /// Checks if an API key exists for a provider
        /// </summary>
        Task<bool> HasApiKeyAsync(string providerName);
    }
}
