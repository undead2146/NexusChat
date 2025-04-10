using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Core.Models;

namespace NexusChat.Services.Interfaces
{
    /// <summary>
    /// Interface for AI service providers that defines standard capabilities
    /// for interacting with various AI models across different providers.
    /// </summary>
    public interface IAIService
    {
        /// <summary>
        /// Gets the name of the model this service is using
        /// </summary>
        string ModelName { get; }
        
        /// <summary>
        /// Gets the name of the AI provider (e.g., Groq, OpenRouter, etc.)
        /// </summary>
        string ProviderName { get; }
        
        /// <summary>
        /// Indicates whether this service supports streaming responses
        /// </summary>
        bool SupportsStreaming { get; }
        
        /// <summary>
        /// Gets the maximum context window size in tokens
        /// </summary>
        int MaxContextWindow { get; }
        
        /// <summary>
        /// Sends a message to the AI and gets a complete text response
        /// </summary>
        /// <param name="prompt">The message to send</param>
        /// <param name="cancellationToken">Token for canceling the operation</param>
        /// <returns>The AI's response as a string</returns>
        Task<string> SendMessageAsync(string prompt, CancellationToken cancellationToken);
        
        /// <summary>
        /// Sends a message to the AI and gets a streamed response
        /// </summary>
        /// <param name="prompt">The message to send</param>
        /// <param name="cancellationToken">Token for canceling the operation</param>
        /// <param name="onMessageUpdate">Callback that receives partial messages as they arrive</param>
        /// <returns>The streamed response as a Stream</returns>
        Task<Stream> SendStreamedMessageAsync(string prompt, CancellationToken cancellationToken, Action<string> onMessageUpdate);
        
        /// <summary>
        /// Gets the capabilities of the current model including context window,
        /// supported features, and performance characteristics
        /// </summary>
        /// <returns>A ModelCapabilities object describing the model's capabilities</returns>
        Task<ModelCapabilities> GetCapabilitiesAsync();
        
        /// <summary>
        /// Estimates the number of tokens in the provided text based on
        /// the tokenization algorithm used by the current model
        /// </summary>
        /// <param name="text">The text to analyze</param>
        /// <returns>Estimated token count</returns>
        int EstimateTokens(string text);
    }
}
