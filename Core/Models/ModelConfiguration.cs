using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SQLite;
using SQLiteNetExtensions.Attributes;

namespace NexusChat.Core.Models
{
    /// <summary>
    /// Represents configuration for an AI model with provider information
    /// </summary>
    [Table("ModelConfigurations")]
    public class ModelConfiguration
    {
        /// <summary>
        /// Gets or sets the unique identifier for this configuration
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        
        /// <summary>
        /// Gets or sets the model ID
        /// </summary>
        public string ModelId { get; set; }
        
        /// <summary>
        /// Gets or sets the provider ID
        /// </summary>
        public string ProviderId { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the model
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the maximum tokens supported by the model
        /// </summary>
        public int MaxTokens { get; set; }
        
        /// <summary>
        /// Gets or sets the provider name (e.g., Groq, OpenRouter)
        /// </summary>
        [Required]
        [SQLite.MaxLength(50)]
        [Indexed]
        public string ProviderName { get; set; }
        
        /// <summary>
        /// Gets or sets the model identifier (e.g., llama3-70b-8192, anthropic/claude-3-opus)
        /// </summary>
        [Required]
        [SQLite.MaxLength(100)]
        [Indexed]
        public string ModelIdentifier { get; set; }
        
        /// <summary>
        /// Gets or sets the human-readable description of the model
        /// </summary>
        [SQLite.MaxLength(500)]
        public string Description { get; set; }
        
        /// <summary>
        /// Gets or sets whether this model is enabled for use
        /// </summary>
        public bool IsEnabled { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether this is a default model
        /// </summary>
        public bool IsDefault { get; set; }
        
        /// <summary>
        /// Gets or sets whether this is a favourite model
        /// </summary>
        public bool IsFavourite { get; set; }
        
        /// <summary>
        /// Gets or sets the capabilities of this model
        /// </summary>
        [TextBlob("CapabilitiesJson")]
        public ModelCapabilities Capabilities { get; set; }
        
        /// <summary>
        /// Gets or sets the JSON serialized capabilities (for database storage)
        /// </summary>
        [Column("Capabilities")]
        public string CapabilitiesJson { get; set; }
        
        /// <summary>
        /// Gets or sets the environment variable name for this model's API key
        /// </summary>
        [SQLite.MaxLength(100)]
        public string ApiKeyEnvironmentVariable { get; set; }
        
        /// <summary>
        /// Gets or sets the API endpoint for this model
        /// </summary>
        [SQLite.MaxLength(255)]
        public string ApiEndpoint { get; set; }
        
        /// <summary>
        /// Gets or sets additional provider-specific configuration as JSON
        /// </summary>
        public string AdditionalConfigJson { get; set; }
        
        /// <summary>
        /// Gets or sets the order priority for display
        /// </summary>
        public int DisplayOrder { get; set; } = 100;
        
        /// <summary>
        /// Gets or sets the source of this configuration
        /// </summary>
        public ConfigurationSource Source { get; set; } = ConfigurationSource.Environment;
        
        /// <summary>
        /// Gets or sets the date this configuration was created
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Gets or sets the date this configuration was last modified
        /// </summary>
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Creates a unique key for caching
        /// </summary>
        public string GetUniqueKey()
        {
            return $"{ProviderName.ToLowerInvariant()}:{ModelIdentifier.ToLowerInvariant()}";
        }
    }
    
    /// <summary>
    /// Source of the model configuration
    /// </summary>
    public enum ConfigurationSource
    {
        /// <summary>
        /// Unknown source
        /// </summary>
        Unknown = 0,
        
        /// <summary>
        /// Configuration loaded from environment variables
        /// </summary>
        Environment = 1,
        
        /// <summary>
        /// Configuration loaded from database
        /// </summary>
        Database = 2,
        
        /// <summary>
        /// Configuration created or modified by user
        /// </summary>
        UserDefined = 3
    }
}
