using System.Threading;
using System.Threading.Tasks;

namespace NexusChat.Services.Interfaces
{
    /// <summary>
    /// Interface for AI service providers
    /// </summary>
    public interface IAIService
    {
        /// <summary>
        /// Gets the name of the current AI model
        /// </summary>
        string ModelName { get; }
        
        /// <summary>
        /// Gets the provider name
        /// </summary>
        string ProviderName { get; }
        
        /// <summary>
        /// Sends a message to the AI service and gets a response
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>The AI's response text</returns>
        Task<string> SendMessageAsync(string message, CancellationToken cancellationToken = default);
    }
}
