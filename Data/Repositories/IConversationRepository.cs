using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Core.Models;

namespace NexusChat.Data.Repositories
{
    /// <summary>
    /// Repository interface for Conversation data access operations
    /// </summary>
    public interface IConversationRepository : IRepository<Conversation>
    {
        /// <summary>
        /// Gets conversations for a specific user
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="limit">Maximum number of conversations to return</param>
        /// <param name="offset">Starting offset for pagination</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>List of user's conversations</returns>
        Task<List<Conversation>> GetByUserIdAsync(
            int userId, 
            int limit = 100, 
            int offset = 0, 
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Gets favorite conversations for a user
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>List of favorite conversations</returns>
        Task<List<Conversation>> GetFavoritesAsync(int userId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets conversations by category
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="category">The category name</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>List of conversations in the category</returns>
        Task<List<Conversation>> GetByCategoryAsync(
            int userId, 
            string category, 
            CancellationToken cancellationToken = default);
    }
}
