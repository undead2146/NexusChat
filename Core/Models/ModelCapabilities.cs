using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NexusChat.Core.Models
{
    /// <summary>
    /// Represents capabilities and limits of an AI model
    /// </summary>
    public class ModelCapabilities
    {
        /// <summary>
        /// Maximum number of tokens the model can process in one request
        /// </summary>
        public int MaxTokens { get; set; } = 4096;
        
        /// <summary>
        /// Maximum context window size for the model (input + output tokens)
        /// </summary>
        public int MaxContextWindow { get; set; } = 8192;
        
        /// <summary>
        /// Default temperature for the model
        /// </summary>
        public float DefaultTemperature { get; set; } = 0.7f;
        
        /// <summary>
        /// Whether the model supports streaming responses
        /// </summary>
        public bool SupportsStreaming { get; set; } = true;
        
        /// <summary>
        /// Whether the model supports vision/image inputs
        /// </summary>
        public bool SupportsVision { get; set; } = false;
        
        /// <summary>
        /// Whether the model has been fine-tuned for code
        /// </summary>
        public bool SupportsCodeCompletion { get; set; } = false;

        /// <summary>
        /// Indicates if the model supports generating images
        /// </summary>
        public bool SupportsImageGeneration { get; set; }

        /// <summary>
        /// Indicates if the model supports function/tool calling
        /// </summary>
        public bool SupportsFunctionCalling { get; set; }

        /// <summary>
        /// Whether the model is marked as a favourite
        /// </summary>
        public bool IsFavourite { get; set; } = false;

        /// <summary>
        /// List of language codes supported by this model
        /// </summary>
        public IList<string> SupportedLanguages { get; set; } = new List<string>();
        
        /// <summary>
        /// Default constructor
        /// </summary>
        public ModelCapabilities() { }
        
        /// <summary>
        /// Constructor with basic parameters
        /// </summary>
        /// <param name="maxTokens">Maximum tokens</param>
        /// <param name="maxContextWindow">Maximum context window</param>
        /// <param name="defaultTemperature">Default temperature</param>
        /// <param name="supportsStreaming">Whether model supports streaming</param>
        public ModelCapabilities(
            int maxTokens = 4096, 
            int maxContextWindow = 8192, 
            float defaultTemperature = 0.7f, 
            bool supportsStreaming = true)
        {
            MaxTokens = maxTokens;
            MaxContextWindow = maxContextWindow;
            DefaultTemperature = defaultTemperature;
            SupportsStreaming = supportsStreaming;
        }
        
        /// <summary>
        /// Creates a deep copy of this ModelCapabilities instance
        /// </summary>
        /// <returns>New instance with the same values</returns>
        public ModelCapabilities Clone()
        {
            return new ModelCapabilities
            {
                MaxTokens = this.MaxTokens,
                MaxContextWindow = this.MaxContextWindow,
                DefaultTemperature = this.DefaultTemperature,
                SupportsStreaming = this.SupportsStreaming,
                SupportsVision = this.SupportsVision,
                SupportsCodeCompletion = this.SupportsCodeCompletion,
                SupportsImageGeneration = this.SupportsImageGeneration,
                SupportsFunctionCalling = this.SupportsFunctionCalling,
                IsFavourite = this.IsFavourite,
                SupportedLanguages = new List<string>(this.SupportedLanguages)
            };
        }
    }
}
