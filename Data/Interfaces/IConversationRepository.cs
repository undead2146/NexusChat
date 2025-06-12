using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Core.Models;

namespace NexusChat.Data.Interfaces
{
    /// <summary>
    /// Repository interface for Conversation data access
    /// </summary>
    public interface IConversationRepository : IRepository<Conversation>
    {
        /// <summary>
        /// Gets all conversations
        /// </summary>
        Task<List<Conversation>> GetAllConversationsAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets a conversation by ID
        /// </summary>
        Task<Conversation?> GetConversationByIdAsync(int id, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets recent conversations
        /// </summary>
        Task<List<Conversation>> GetRecentAsync(int limit = 10, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Adds a conversation
        /// </summary>
        Task<int> AddConversationAsync(Conversation conversation, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Updates a conversation
        /// </summary>
        Task<bool> UpdateConversationAsync(Conversation conversation, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Deletes a conversation
        /// </summary>
        Task<bool> DeleteConversationAsync(int id, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Updates the last activity of a conversation
        /// </summary>
        Task<bool> UpdateLastActivityAsync(int conversationId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets conversations by user ID
        /// </summary>
        Task<List<Conversation>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
        
        Task<List<Conversation>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
