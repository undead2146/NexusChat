using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Core.Models;

namespace NexusChat.Services.Interfaces
{
    /// <summary>
    /// Interface for AI service providers
    /// </summary>
    public interface IAIProviderService
    {
        /// <summary>
        /// Gets the name of the model
        /// </summary>
        string ModelName { get; }
        
        /// <summary>
        /// Gets the name of the provider
        /// </summary>
        string ProviderName { get; }
        
        /// <summary>
        /// Gets whether streaming is supported
        /// </summary>
        bool SupportsStreaming { get; }
        
        /// <summary>
        /// Gets the maximum context window size
        /// </summary>
        int MaxContextWindow { get; }
        
        /// <summary>
        /// Sends a message to the AI service
        /// </summary>
        /// <param name="prompt">The prompt to send</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The AI response</returns>
        Task<string> SendMessageAsync(string prompt, CancellationToken cancellationToken);
        
        /// <summary>
        /// Sends a message with streaming response
        /// </summary>
        /// <param name="prompt">The prompt to send</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="onMessageUpdate">Callback for content updates</param>
        /// <returns>Stream with the full response</returns>
        Task<Stream> SendStreamedMessageAsync(string prompt, CancellationToken cancellationToken, Action<string> onMessageUpdate);
        
        /// <summary>
        /// Gets the capabilities of this model
        /// </summary>
        Task<AIModel> GetCapabilitiesAsync();
        
        /// <summary>
        /// Estimates token count for text
        /// </summary>
        int EstimateTokens(string text);
    }
}
