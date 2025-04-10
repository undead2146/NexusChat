using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Core.Models;

namespace NexusChat.Data.Interfaces
{
    /// <summary>
    /// Repository interface for Message data access operations
    /// </summary>
    public interface IMessageRepository : IRepository<Message>
    {
        /// <summary>
        /// Gets all messages for a conversation
        /// </summary>
        /// <param name="conversationId">Conversation ID</param>
        /// <returns>List of messages</returns>
        Task<List<Message>> GetMessagesByConversationAsync(int conversationId);

        /// <summary>
        /// Gets all messages for a conversation with cancellation support
        /// </summary>
        /// <param name="conversationId">Conversation ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of messages</returns>
        Task<List<Message>> GetMessagesByConversationAsync(int conversationId, CancellationToken cancellationToken);

        /// <summary>
        /// Gets messages for a conversation with pagination
        /// </summary>
        /// <param name="conversationId">Conversation ID</param>
        /// <param name="limit">Maximum number of messages</param>
        /// <param name="offset">Starting offset</param>
        /// <returns>List of messages</returns>
        Task<List<Message>> GetMessagesByConversationAsync(int conversationId, int limit, int offset);

        /// <summary>
        /// Gets messages for a conversation with pagination and cancellation support
        /// </summary>
        /// <param name="conversationId">Conversation ID</param>
        /// <param name="limit">Maximum number of messages</param>
        /// <param name="offset">Starting offset</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of messages</returns>
        Task<List<Message>> GetMessagesByConversationAsync(int conversationId, int limit, int offset, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the most recent message for a conversation
        /// </summary>
        /// <param name="conversationId">Conversation ID</param>
        /// <returns>Most recent message</returns>
        Task<Message> GetLastMessageAsync(int conversationId);

        /// <summary>
        /// Gets the most recent message for a conversation with cancellation support
        /// </summary>
        /// <param name="conversationId">Conversation ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Most recent message</returns>
        Task<Message> GetLastMessageAsync(int conversationId, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the count of messages for a conversation
        /// </summary>
        /// <param name="conversationId">Conversation ID</param>
        /// <returns>Number of messages</returns>
        Task<int> GetMessageCountAsync(int conversationId);

        /// <summary>
        /// Gets the count of messages for a conversation with cancellation support
        /// </summary>
        /// <param name="conversationId">Conversation ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of messages</returns>
        Task<int> GetMessageCountAsync(int conversationId, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes all messages for a conversation
        /// </summary>
        /// <param name="conversationId">Conversation ID</param>
        /// <returns>Number of messages deleted</returns>
        Task<int> DeleteByConversationAsync(int conversationId);

        /// <summary>
        /// Deletes all messages for a conversation with cancellation support
        /// </summary>
        /// <param name="conversationId">Conversation ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of messages deleted</returns>
        Task<int> DeleteByConversationAsync(int conversationId, CancellationToken cancellationToken);

        /// <summary>
        /// Adds a message to a conversation
        /// </summary>
        /// <param name="message">Message to add</param>
        /// <param name="conversationId">Conversation ID</param>
        /// <returns>ID of the added message</returns>
        Task<int> AddMessageAsync(Message message, int conversationId);

        /// <summary>
        /// Adds a message to a conversation with cancellation support
        /// </summary>
        /// <param name="message">Message to add</param>
        /// <param name="conversationId">Conversation ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>ID of the added message</returns>
        Task<int> AddMessageAsync(Message message, int conversationId, CancellationToken cancellationToken);

        // Legacy methods for backward compatibility
        Task<List<Message>> GetByConversationIdAsync(int conversationId);
        Task<int> DeleteByConversationIdAsync(int conversationId);
    }
}
