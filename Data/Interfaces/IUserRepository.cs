using System.Threading;
using System.Threading.Tasks;
using NexusChat.Core.Models;

namespace NexusChat.Data.Interfaces
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
        /// <returns>User if found, null otherwise</returns>
        Task<User?> GetByUsernameAsync(string username);

        /// <summary>
        /// Gets a user by username with cancellation token
        /// </summary>
        /// <param name="username">The username to search for</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>User if found, null otherwise</returns>
        Task<User> GetByUsernameAsync(string username, CancellationToken cancellationToken);

        /// <summary>
        /// Validates user credentials
        /// </summary>
        /// <param name="username">Username for validation</param>
        /// <param name="password">Password for validation</param>
        /// <returns>True if credentials are valid, false otherwise</returns>
        Task<bool> ValidateCredentialsAsync(string username, string password);

        /// <summary>
        /// Checks if a username exists
        /// </summary>
        /// <param name="username">The username to check</param>
        /// <returns>True if username exists, false otherwise</returns>
        Task<bool> UsernameExistsAsync(string username);
    }
}
