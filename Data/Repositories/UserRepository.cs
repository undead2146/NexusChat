using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Core.Models;
using NexusChat.Data.Context;
using SQLite;

namespace NexusChat.Data.Repositories
{
    /// <summary>
    /// Repository for user data access operations
    /// </summary>
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        /// <summary>
        /// Initializes a new instance of UserRepository
        /// </summary>
        /// <param name="databaseService">Database service</param>
        public UserRepository(DatabaseService databaseService) : base(databaseService)
        {
        }
        
        /// <summary>
        /// Gets a user by username
        /// </summary>
        public async Task<User> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            try
            {
                await _databaseService.Initialize(cancellationToken);
                return await _databaseService.Database.Table<User>()
                    .Where(u => u.Username == username)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetByUsernameAsync: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Authenticates a user
        /// </summary>
        public async Task<User> AuthenticateUserAsync(
            string username, 
            string password, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _databaseService.Initialize(cancellationToken);
                var user = await GetByUsernameAsync(username, cancellationToken);
                
                if (user != null && user.VerifyPassword(password))
                {
                    return user;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in AuthenticateUserAsync: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Updates the user's last login time
        /// </summary>
        public async Task UpdateLastLoginAsync(int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                await _databaseService.Initialize(cancellationToken);
                var user = await GetByIdAsync(userId, cancellationToken);
                
                if (user != null)
                {
                    user.LastLogin = DateTime.UtcNow;
                    await _databaseService.Database.UpdateAsync(user);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in UpdateLastLoginAsync: {ex.Message}");
                throw;
            }
        }
    }
}
