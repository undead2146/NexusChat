using SQLite;
using System;

namespace NexusChat.Models
{
    /// <summary>
    /// Represents an AI model that can be used for conversations
    /// </summary>
    public class AIModel
    {
        /// <summary>
        /// Unique identifier for the model
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        
        /// <summary>
        /// Name of the AI provider (e.g., OpenAI, Anthropic)
        /// </summary>
        [NotNull, MaxLength(50)]
        public string ProviderName { get; set; }
        
        /// <summary>
        /// Name of the specific model (e.g., GPT-4, Claude-2)
        /// </summary>
        [NotNull, MaxLength(100)]
        public string ModelName { get; set; }
        
        /// <summary>
        /// Description of the model capabilities
        /// </summary>
        [MaxLength(500)]
        public string Description { get; set; }
        
        /// <summary>
        /// Maximum tokens the model can process
        /// </summary>
        public int MaxTokens { get; set; }
        
        /// <summary>
        /// Default temperature setting
        /// </summary>
        public float DefaultTemperature { get; set; } = 0.7f;
        
        /// <summary>
        /// Whether the model is currently available for use
        /// </summary>
        public bool IsAvailable { get; set; } = true;
        
        /// <summary>
        /// Path to the model's icon
        /// </summary>
        public string IconPath { get; set; }
        
        /// <summary>
        /// Creates a test AI model for development purposes
        /// </summary>
        public static AIModel CreateTestModel()
        {
            return new AIModel
            {
                ProviderName = "OpenAI",
                ModelName = "GPT-4",
                Description = "Advanced language model for general purpose AI tasks",
                MaxTokens = 8192,
                DefaultTemperature = 0.7f,
                IsAvailable = true,
                IconPath = "openai_logo.png"
            };
        }
    }
}
