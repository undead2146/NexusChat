using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Core.Models;

namespace NexusChat.Data.Interfaces
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
        /// <param name="limit">Maximum number of conversations to return (for pagination)</param>
        /// <param name="offset">Starting offset (for pagination)</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>List of conversations for the user</returns>
        Task<List<Conversation>> GetByUserIdAsync(
            int userId, 
            int limit = 50, 
            int offset = 0, 
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Gets conversations for a specific user
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <returns>List of conversations for the user</returns>
        Task<List<Conversation>> GetByUserIdAsync(int userId);

        /// <summary>
        /// Gets conversations for a specific user
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of conversations for the user</returns>
        Task<List<Conversation>> GetByUserIdAsync(int userId, CancellationToken cancellationToken);
        
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

        /// <summary>
        /// Gets active conversations
        /// </summary>
        /// <returns>List of active conversations</returns>
        Task<List<Conversation>> GetActiveConversationsAsync();

        /// <summary>
        /// Gets active conversations
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of active conversations</returns>
        Task<List<Conversation>> GetActiveConversationsAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Gets conversations by model ID
        /// </summary>
        /// <param name="modelId">The model identifier</param>
        /// <returns>List of conversations for the model</returns>
        Task<List<Conversation>> GetByModelIdAsync(int modelId);

        /// <summary>
        /// Gets conversations by model ID
        /// </summary>
        /// <param name="modelId">The model identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of conversations for the model</returns>
        Task<List<Conversation>> GetByModelIdAsync(int modelId, CancellationToken cancellationToken);

        /// <summary>
        /// Searches conversations by text
        /// </summary>
        /// <param name="searchText">Text to search for</param>
        /// <returns>List of matching conversations</returns>
        Task<List<Conversation>> SearchAsync(string searchText);

        /// <summary>
        /// Searches conversations by text
        /// </summary>
        /// <param name="searchText">Text to search for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of matching conversations</returns>
        Task<List<Conversation>> SearchAsync(string searchText, CancellationToken cancellationToken);
    }
}
