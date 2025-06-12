using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NexusChat.Services.Interfaces
{
    /// <summary>
    /// Interface for low-level API key storage and retrieval operations
    /// </summary>
    public interface IApiKeyStorageProvider
    {
        /// <summary>
        /// Initializes the storage provider
        /// </summary>
        Task InitializeAsync();
        
        /// <summary>
        /// Gets a stored value by exact key name
        /// </summary>
        /// <param name="exactKeyName">Exact key name to retrieve</param>
        /// <returns>Stored value if found, otherwise null</returns>
        Task<string> GetStoredValueAsync(string exactKeyName);
        
        /// <summary>
        /// Gets all stored values with a specific prefix
        /// </summary>
        /// <param name="prefix">Prefix to filter by</param>
        /// <returns>Dictionary of matching key names and values</returns>
        Task<Dictionary<string, string>> GetAllStoredValuesWithPrefixAsync(string prefix);
        
        /// <summary>
        /// Saves a value to storage
        /// </summary>
        /// <param name="exactKeyName">Exact key name to store</param>
        /// <param name="value">Value to store</param>
        /// <returns>True if successful, otherwise false</returns>
        Task<bool> SaveStoredValueAsync(string exactKeyName, string value);
        
        /// <summary>
        /// Deletes a value from storage
        /// </summary>
        /// <param name="exactKeyName">Exact key name to delete</param>
        /// <returns>True if successful, otherwise false</returns>
        Task<bool> DeleteStoredValueAsync(string exactKeyName);
    }
}
