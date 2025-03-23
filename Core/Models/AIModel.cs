using System;
using SQLite;

namespace NexusChat.Core.Models
{
    /// <summary>
    /// Represents an AI model that can be used for conversations
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
        /// Name of the model (e.g., "GPT-4", "Claude 2")
        /// </summary>
        [NotNull, MaxLength(50)]
        public string ModelName { get; set; }
        
        /// <summary>
        /// Provider of the model (e.g., "OpenAI", "Anthropic")
        /// </summary>
        [NotNull, MaxLength(50)]
        public string ProviderName { get; set; }
        
        /// <summary>
        /// Description of the model's capabilities
        /// </summary>
        [MaxLength(500)]
        public string Description { get; set; }
        
        /// <summary>
        /// Whether the model is currently available
        /// </summary>
        public bool IsAvailable { get; set; }
        
        /// <summary>
        /// Maximum number of tokens the model can process
        /// </summary>
        public int MaxTokens { get; set; }
        
        /// <summary>
        /// Default temperature setting for the model
        /// </summary>
        public float DefaultTemperature { get; set; }
        
        /// <summary>
        /// Default constructor
        /// </summary>
        public AIModel()
        {
            IsAvailable = true;
            DefaultTemperature = 0.7f;
        }
        
        /// <summary>
        /// Creates an AI model with basic information
        /// </summary>
        public AIModel(string modelName, string providerName, int maxTokens)
        {
            ModelName = modelName;
            ProviderName = providerName;
            MaxTokens = maxTokens;
            IsAvailable = true;
            DefaultTemperature = 0.7f;
        }
        
        /// <summary>
        /// String representation for debugging
        /// </summary>
        public override string ToString()
        {
            return $"{ModelName} by {ProviderName}";
        }
    }
}
