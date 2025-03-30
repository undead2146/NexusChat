using System.Threading;
using System.Threading.Tasks;

namespace NexusChat.Services.Interfaces
{
    /// <summary>
    /// Interface for AI service providers that can generate responses to prompts
    /// </summary>
    public interface IAIService
    {
        /// <summary>
        /// Gets the name of the AI model being used
        /// </summary>
        string ModelName { get; }
        
        /// <summary>
        /// Gets the name of the provider offering the AI service
        /// </summary>
        string ProviderName { get; }
        
        /// <summary>
        /// Sends a message to the AI service and gets a response
        /// </summary>
        /// <param name="prompt">The user's message or prompt</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>The AI's response text</returns>
        Task<string> SendMessageAsync(string prompt, CancellationToken cancellationToken);
        
        /// <summary>
        /// Gets information about the model's capabilities
        /// </summary>
        /// <returns>Dictionary of capability information</returns>
        Task<ModelCapabilities> GetCapabilitiesAsync();
        
        /// <summary>
        /// Estimates the number of tokens in the given text
        /// </summary>
        /// <param name="text">Text to analyze</param>
        /// <returns>Estimated token count</returns>
        int EstimateTokens(string text);
    }
    
    /// <summary>
    /// Represents the capabilities of an AI model
    /// </summary>
    public class ModelCapabilities
    {
        /// <summary>
        /// Maximum number of tokens the model can process in a single request
        /// </summary>
        public int MaxTokens { get; set; }
        
        /// <summary>
        /// Whether the model supports image generation
        /// </summary>
        public bool SupportsImageGeneration { get; set; }
        
        /// <summary>
        /// Whether the model supports code completion
        /// </summary>
        public bool SupportsCodeCompletion { get; set; }
        
        /// <summary>
        /// Whether the model supports function calling
        /// </summary>
        public bool SupportsFunctionCalling { get; set; }
        
        /// <summary>
        /// Default temperature setting for the model
        /// </summary>
        public float DefaultTemperature { get; set; }
    }
}
