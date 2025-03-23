using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Core.Models;

namespace NexusChat.Data.Repositories
{
    /// <summary>
    /// Repository interface for Message data access operations
    /// </summary>
    public interface IMessageRepository : IRepository<Message>
    {
        /// <summary>
        /// Gets messages for a specific conversation
        /// </summary>
        /// <param name="conversationId">The conversation identifier</param>
        /// <param name="limit">Maximum number of messages to return (for pagination)</param>
        /// <param name="offset">Starting offset (for pagination)</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>List of messages for the conversation</returns>
        Task<List<Message>> GetByConversationIdAsync(
            int conversationId, 
            int limit = 50, 
            int offset = 0, 
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Gets the count of messages in a conversation
        /// </summary>
        /// <param name="conversationId">The conversation identifier</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Number of messages</returns>
        Task<int> GetMessageCountAsync(int conversationId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Deletes all messages for a conversation
        /// </summary>
        /// <param name="conversationId">The conversation identifier</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Number of messages deleted</returns>
        Task<int> DeleteByConversationIdAsync(int conversationId, CancellationToken cancellationToken = default);
    }
}
