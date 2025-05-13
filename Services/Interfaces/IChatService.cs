using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Core.Models;

namespace NexusChat.Services.Interfaces
{
    /// <summary>
    /// Interface for chat service functionality
    /// </summary>
    public interface IChatService
    {
        /// <summary>
        /// Gets the current AI model being used
        /// </summary>
        AIModel CurrentModel { get; }

        /// <summary>
        /// Occurs when streaming data is received
        /// </summary>
        event EventHandler<string> StreamingDataReceived;
        
        /// <summary>
        /// Occurs when the model is changed
        /// </summary>
        event EventHandler<AIModel> ModelChanged;

        /// <summary>
        /// Gets a response from the AI for the given prompt
        /// </summary>
        Task<string> GetResponseAsync(string prompt, CancellationToken cancellationToken);

        /// <summary>
        /// Gets a streaming response from the AI
        /// </summary>
        Task<Stream> GetStreamingResponseAsync(string prompt, CancellationToken cancellationToken);

        /// <summary>
        /// Changes the current AI model
        /// </summary>
        Task<bool> ChangeModelAsync(AIModel model);

        /// <summary>
        /// Gets the estimated token count for a message
        /// </summary>
        Task<int> EstimateTokens(string message);

        /// <summary>
        /// Sends a message to the AI service
        /// </summary>
        Task<Message> SendAIMessageAsync(int conversationId, string userPrompt, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Sends a message in a conversation
        /// </summary>
        Task<Message> SendMessageAsync(int conversationId, string content, CancellationToken cancellationToken = default);
    }
}
