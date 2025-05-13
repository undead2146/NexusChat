using System;
using System.Collections.Generic;
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
        Task<List<Message>> GetMessagesByConversationIdAsync(int conversationId);
        
        /// <summary>
        /// Gets messages for a specific conversation before a timestamp
        /// </summary>
        Task<List<Message>> GetMessagesByConversationIdBeforeTimestampAsync(int conversationId, DateTime timestamp, int limit);
        
        /// <summary>
        /// Gets the count of messages for a conversation
        /// </summary>
        Task<int> GetMessageCountForConversationAsync(int conversationId);
        
        /// <summary>
        /// Gets messages for a specific conversation (alternative name for compatibility)
        /// </summary>
        Task<List<Message>> GetMessagesByConversationAsync(int conversationId);
        
        /// <summary>
        /// Adds a message to the database
        /// </summary>
        Task<int> AddMessageAsync(Message message);
        
        /// <summary>
        /// Updates a message in the database
        /// </summary>
        Task<bool> UpdateMessageAsync(Message message);
        
        /// <summary>
        /// Deletes all messages for a conversation
        /// </summary>
        Task<bool> DeleteAllMessagesForConversationAsync(int conversationId);
        
        /// <summary>
        /// Deletes messages by conversation ID (alternative name for compatibility)
        /// </summary>
        Task<bool> DeleteByConversationIdAsync(int conversationId);
        
        /// <summary>
        /// Gets messages for a specific conversation with pagination
        /// </summary>
        Task<List<Message>> GetByConversationIdAsync(int conversationId, int limit = 20, int offset = 0);
    }
}
