using System;

namespace NexusChat.Services.ApiKeyManagement
{
    /// <summary>
    /// Metadata about an API key
    /// </summary>
    public class KeyMetadata
    {
        /// <summary>
        /// Gets or sets the source of the API key
        /// </summary>
        public KeySource Source { get; set; }
        
        /// <summary>
        /// Gets or sets when the key was created or first loaded
        /// </summary>
        public DateTime CreatedTime { get; set; }
        
        /// <summary>
        /// Gets or sets when the key was last used
        /// </summary>
        public DateTime LastUsedTime { get; set; }
        
        /// <summary>
        /// Gets or sets the number of times the key has been used
        /// </summary>
        public int UseCount { get; set; }
    }
    
    /// <summary>
    /// Source of an API key
    /// </summary>
    public enum KeySource
    {
        /// <summary>
        /// Key was loaded from environment variables
        /// </summary>
        Environment,
        
        /// <summary>
        /// Key was loaded from secure storage
        /// </summary>
        SecureStorage,
        
        /// <summary>
        /// Key was defined by user through UI
        /// </summary>
        UserDefined
    }
}
