using System;

namespace NexusChat.Core.Models
{
    /// <summary>
    /// Represents an AI provider with its properties
    /// </summary>
    public class AIProvider
    {
        /// <summary>
        /// The unique identifier or key name for the provider
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// User-friendly display name for the provider
        /// </summary>
        public string DisplayName { get; set; }
        
        /// <summary>
        /// Description of the provider
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Whether the provider is currently available and configured
        /// </summary>
        public bool IsAvailable { get; set; }
        
        /// <summary>
        /// API endpoint or base URL for the provider
        /// </summary>
        public string ApiEndpoint { get; set; }
        
        /// <summary>
        /// Documentation URL for the provider
        /// </summary>
        public string DocumentationUrl { get; set; }
    }
}
