using System.Threading;
using System.Threading.Tasks;
using NexusChat.Core.Models;

namespace NexusChat.Data.Repositories
{
    /// <summary>
    /// Repository interface for User data access operations
    /// </summary>
    public interface IUserRepository : IRepository<User>
    {
        /// <summary>
        /// Gets a user by username
        /// </summary>
        /// <param name="username">The username to search for</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>User if found, null otherwise</returns>
        Task<User> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Authenticates a user with username and password
        /// </summary>
        /// <param name="username">Username for authentication</param>
        /// <param name="password">Password for authentication</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>User if authentication succeeds, null otherwise</returns>
        Task<User> AuthenticateUserAsync(
            string username, 
            string password, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Updates the user's last login time
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        Task UpdateLastLoginAsync(int userId, CancellationToken cancellationToken = default);
    }
}
