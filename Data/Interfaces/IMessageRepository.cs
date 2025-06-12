using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Core.Models;

namespace NexusChat.Data.Interfaces
{
    /// <summary>
    /// Repository interface for Message data access
    /// </summary>
    public interface IMessageRepository : IRepository<Message>
    {
        /// <summary>
        /// Gets messages for a specific conversation
        /// </summary>
        Task<List<Message>> GetMessagesByConversationIdAsync(int conversationId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets messages for a specific conversation (alternative name for compatibility)
        /// </summary>
        Task<List<Message>> GetMessagesByConversationAsync(int conversationId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets messages for a specific conversation before a timestamp
        /// </summary>
        Task<List<Message>> GetMessagesByConversationIdBeforeTimestampAsync(int conversationId, DateTime timestamp, int limit, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets the count of messages for a conversation
        /// </summary>
        Task<int> GetMessageCountForConversationAsync(int conversationId, CancellationToken cancellationToken = default);
        
        
        /// <summary>
        /// Adds a message to the database
        /// </summary>
        Task<int> AddMessageAsync(Message message, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Updates a message in the database
        /// </summary>
        Task<bool> UpdateMessageAsync(Message message, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Deletes all messages for a conversation
        /// </summary>
        Task<bool> DeleteAllMessagesForConversationAsync(int conversationId, CancellationToken cancellationToken = default);
        
        
        /// <summary>
        /// Gets messages for a specific conversation with pagination
        /// </summary>
        Task<List<Message>> GetByConversationIdAsync(int conversationId, int limit = 20, int offset = 0, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Deletes messages by conversation ID
        /// </summary>
        Task<bool> DeleteByConversationIdAsync(int conversationId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Checks if a message belongs to a specific conversation
        /// </summary>
        Task<bool> BelongsToConversationAsync(int messageId, int conversationId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Moves a message to a new conversation
        /// </summary>
        Task<bool> MoveMessageToConversationAsync(int messageId, int newConversationId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Deletes messages from a specific conversation
        /// </summary>
        Task<bool> DeleteMessagesFromConversationAsync(int conversationId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets the last message for a specific conversation
        /// </summary>
        Task<Message?> GetLastMessageForConversationAsync(int conversationId, CancellationToken cancellationToken = default);
    }
}
