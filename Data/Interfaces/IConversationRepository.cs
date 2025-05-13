using System;
using System.Collections.Generic;
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
        Task<List<Conversation>> GetAllConversationsAsync();
        
        /// <summary>
        /// Gets a conversation by ID
        /// </summary>
        Task<Conversation> GetConversationByIdAsync(int id);
        
        /// <summary>
        /// Gets recent conversations
        /// </summary>
        Task<List<Conversation>> GetRecentAsync(int limit = 10);
        
        /// <summary>
        /// Adds a conversation
        /// </summary>
        Task<int> AddConversationAsync(Conversation conversation);
        
        /// <summary>
        /// Updates a conversation
        /// </summary>
        Task<bool> UpdateConversationAsync(Conversation conversation);
        
        /// <summary>
        /// Deletes a conversation
        /// </summary>
        Task<bool> DeleteConversationAsync(int id);
        
        /// <summary>
        /// Updates the last activity of a conversation
        /// </summary>
        Task<bool> UpdateLastActivityAsync(int conversationId);
    }
}
