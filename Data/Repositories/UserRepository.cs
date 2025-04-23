using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Core.Models;
using NexusChat.Data.Context;
using NexusChat.Data.Interfaces;
using SQLite;
using BCrypt.Net;

namespace NexusChat.Data.Repositories
{
    /// <summary>
    /// Repository implementation for User data access
    /// </summary>
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        /// <summary>
        /// Initializes a new instance of the UserRepository class
        /// </summary>
        /// <param name="databaseService">Database service</param>
        public UserRepository(DatabaseService databaseService) 
            : base(databaseService.GetConnection())
        {
            // Ensure table creation
            _database.CreateTableAsync<User>().Wait();
        }

        /// <summary>
        /// Gets a user by username
        /// </summary>
        /// <param name="username">The username</param>
        /// <returns>User if found, null otherwise</returns>
        public async Task<User> GetByUsernameAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return null;

            try
            {
                return await _database.Table<User>()
                    .Where(u => u.Username.ToLower() == username.ToLower())
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting user by username: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets a user by username with cancellation support
        /// </summary>
        /// <param name="username">The username</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>User if found, null otherwise</returns>
        public async Task<User> GetByUsernameAsync(string username, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(username))
                return null;

            try
            {
                // Create a task that can be cancelled
                var queryTask = _database.Table<User>()
                    .Where(u => u.Username.ToLower() == username.ToLower())
                    .FirstOrDefaultAsync();

                // Use Task.WhenAny to support cancellation
                var completedTask = await Task.WhenAny(queryTask, Task.Delay(-1, cancellationToken));
                
                // If the delay task completed first, cancellation was requested
                if (completedTask != queryTask)
                    throw new OperationCanceledException(cancellationToken);

                // Get the result from the query task
                return await queryTask;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting user by username: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Validates user credentials
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <returns>True if credentials are valid, false otherwise</returns>
        public async Task<bool> ValidateCredentialsAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return false;

            try
            {
                var user = await GetByUsernameAsync(username);
                if (user == null)
                    return false;

                // Use BCrypt to verify the password
                return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error validating credentials: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if username exists
        /// </summary>
        /// <param name="username">Username to check</param>
        /// <returns>True if username exists, false otherwise</returns>
        public async Task<bool> UsernameExistsAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            try
            {
                var user = await GetByUsernameAsync(username);
                return user != null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking username existence: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Creates a user with password hashing
        /// </summary>
        /// <param name="user">User model</param>
        /// <param name="password">Plain text password</param>
        /// <returns>Created user ID</returns>
        public async Task<int> CreateUserWithPasswordAsync(User user, string password)
        {
            if (user == null || string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("User and password must be provided");

            try
            {
                // Check if username already exists
                if (await UsernameExistsAsync(user.Username))
                    throw new InvalidOperationException("Username already exists");

                // Hash the password
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                
                // Set creation date if not already set
                if (user.CreatedAt == default)
                    user.CreatedAt = DateTime.UtcNow;
                
                // Add the user
                return await AddAsync(user);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating user: {ex.Message}");
                throw;
            }
        }
    }
}
