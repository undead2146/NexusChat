using System;
using SQLite;

namespace NexusChat.Core.Models
{
    /// <summary>
    /// Represents an AI model with its metadata and capabilities
    /// </summary>
    [Table("AIModels")]
    public class AIModel
    {
        /// <summary>
        /// Gets or sets the ID of the model
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the model
        /// </summary>
        [MaxLength(100)]
        public string ModelName { get; set; }
        
        /// <summary>
        /// Gets or sets the provider name
        /// </summary>
        [MaxLength(50)]
        public string ProviderName { get; set; }
        
        /// <summary>
        /// Gets or sets the model description
        /// </summary>
        [MaxLength(500)]
        public string Description { get; set; }
        
        /// <summary>
        /// Gets or sets the maximum tokens
        /// </summary>
        public int MaxTokens { get; set; }
        
        /// <summary>
        /// Gets or sets the maximum context window
        /// </summary>
        public int MaxContextWindow { get; set; }
        
        /// <summary>
        /// Gets or sets whether streaming is supported
        /// </summary>
        public bool SupportsStreaming { get; set; }
        
        /// <summary>
        /// Gets or sets whether vision capabilities are supported
        /// </summary>
        public bool SupportsVision { get; set; }
        
        /// <summary>
        /// Gets or sets whether code completion is optimized
        /// </summary>
        public bool SupportsCodeCompletion { get; set; }
        
        /// <summary>
        /// Gets or sets whether the model is available
        /// </summary>
        public bool IsAvailable { get; set; }
        
        /// <summary>
        /// Gets or sets the API key variable name
        /// </summary>
        [MaxLength(100)]
        public string ApiKeyVariable { get; set; }
        
        /// <summary>
        /// Gets or sets the display name
        /// </summary>
        [MaxLength(100)]
        public string DisplayName { get; set; }
        
        /// <summary>
        /// Gets or sets the model version
        /// </summary>
        [MaxLength(50)]
        public string Version { get; set; }
        
        /// <summary>
        /// Gets or sets the default temperature
        /// </summary>
        public float DefaultTemperature { get; set; } = 0.7f;
        
        /// <summary>
        /// Gets or sets whether the model is currently selected
        /// </summary>
        public bool IsSelected { get; set; }
        
        /// <summary>
        /// Gets or sets whether the model is a favorite
        /// </summary>
        public bool IsFavorite { get; set; }
        
        /// <summary>
        /// Gets or sets whether the model is the default for its provider
        /// </summary>
        public bool IsDefault { get; set; }
        
        /// <summary>
        /// Gets or sets the usage count
        /// </summary>
        public int UsageCount { get; set; }
        
        /// <summary>
        /// Gets or sets when the model was last used
        /// </summary>
        public DateTime? LastUsed { get; set; }
        
        /// <summary>
        /// Gets or sets the display status
        /// </summary>
        public string DisplayStatus { get; set; } = "normal";
        
        /// <summary>
        /// Creates a copy of this model
        /// </summary>
        public AIModel Clone()
        {
            return new AIModel
            {
                Id = this.Id,
                ModelName = this.ModelName,
                ProviderName = this.ProviderName,
                Description = this.Description,
                MaxTokens = this.MaxTokens,
                MaxContextWindow = this.MaxContextWindow,
                SupportsStreaming = this.SupportsStreaming,
                SupportsVision = this.SupportsVision,
                SupportsCodeCompletion = this.SupportsCodeCompletion,
                IsAvailable = this.IsAvailable,
                ApiKeyVariable = this.ApiKeyVariable,
                DisplayName = this.DisplayName,
                Version = this.Version,
                DefaultTemperature = this.DefaultTemperature,
                IsSelected = this.IsSelected,
                IsFavorite = this.IsFavorite,
                IsDefault = this.IsDefault,
                UsageCount = this.UsageCount,
                LastUsed = this.LastUsed,
                DisplayStatus = this.DisplayStatus
            };
        }
    }
}
