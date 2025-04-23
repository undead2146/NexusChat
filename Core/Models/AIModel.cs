using System;
using SQLite;
using System.Text.Json.Serialization;

namespace NexusChat.Core.Models
{
    /// <summary>
    /// Represents an AI model in the application
    /// </summary>
    [Table("AIModels")]
    public class AIModel
    {
        /// <summary>
        /// Unique identifier for the model
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        
        /// <summary>
        /// Name of the model (e.g., "gpt-4", "llama3-70b-8192")
        /// </summary>
        [MaxLength(100)]
        public string ModelName { get; set; }
        
        /// <summary>
        /// Name of the provider (e.g., "OpenAI", "Groq", "OpenRouter")
        /// </summary>
        [MaxLength(50)]
        public string ProviderName { get; set; }
        
        /// <summary>
        /// Brief description of the model
        /// </summary>
        [MaxLength(500)]
        public string Description { get; set; }
        
        /// <summary>
        /// Maximum number of tokens the model can process in one request
        /// </summary>
        public int MaxTokens { get; set; }
        
        /// <summary>
        /// Maximum context window size for the model (input + output tokens)
        /// </summary>
        public int MaxContextWindow { get; set; }
        
        /// <summary>
        /// Default temperature for the model
        /// </summary>
        public float DefaultTemperature { get; set; } = 0.7f;
        
        /// <summary>
        /// Whether the model is the default for the user
        /// </summary>
        public bool IsDefault { get; set; }

                
        /// <summary>
        /// Whether the model is marked as a favourite
        /// </summary>
        public bool IsFavourite { get; set; }


        /// <summary>
        /// Indicates whether the model is currently available for use
        /// </summary>
        public bool IsAvailable { get; set; } = true;

        /// <summary>
        /// Whether the model supports streaming responses
        /// </summary>
        public bool SupportsStreaming { get; set; }
        
        /// <summary>
        /// Whether the model supports code completion capabilities
        /// </summary>
        public bool SupportsCodeCompletion { get; set; }
        
        /// <summary>
        /// Whether the model requires a specific API key (versus a provider-level key)
        /// </summary>
        public bool RequiresModelSpecificApiKey { get; set; }
        
        /// <summary>
        /// Custom API key for this model (if applicable)
        /// </summary>
        public string ApiKey { get; set; }
        
        /// <summary>
        /// Model capabilities - not stored in database, used for API communication
        /// </summary>
        [JsonIgnore]
        [Ignore]
        public ModelCapabilities Capabilities { get; set; }
        
        /// <summary>
        /// Creates capabilities object on-demand for API operations
        /// </summary>
        /// <returns>Model capabilities based on this model's properties</returns>

        public ModelCapabilities GetCapabilities()
        {
            return Capabilities ?? (Capabilities = new ModelCapabilities
            {
                MaxTokens = MaxTokens,
                MaxContextWindow = MaxContextWindow,
                DefaultTemperature = DefaultTemperature,
                SupportsStreaming = SupportsStreaming,
                SupportsCodeCompletion = SupportsCodeCompletion
            });
        }
        
        /// <summary>
        /// Date the model was created in database
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Date the model was last updated
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
        
        /// <summary>
        /// Default constructor
        /// </summary>
        public AIModel() { }
        
        /// <summary>
        /// Constructor with basic required fields
        /// </summary>
        public AIModel(string modelName, string providerName, string description = null)
        {
            ModelName = modelName;
            ProviderName = providerName;
            Description = description ?? $"{providerName} {modelName}";
            CreatedAt = DateTime.UtcNow;
        }
        
        /// <summary>
        /// String representation for debugging
        /// </summary>
        public override string ToString()
        {
            return $"{ProviderName} - {ModelName}";
        }
    }
}
