using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Core.Models;
using NexusChat.Data.Context;
using NexusChat.Data.Interfaces;
using BCrypt.Net;

namespace NexusChat.Data.Repositories
{
    /// <summary>
    /// Repository for user management
    /// </summary>
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        /// <summary>
        /// Creates a new instance of UserRepository
        /// </summary>
        public UserRepository(DatabaseService databaseService) 
            : base(databaseService)
        {
        }
        
        /// <summary>
        /// Initializes the repository
        /// </summary>
        public async Task InitializeAsync()
        {
            await InitializeRepositoryAsync();
            
            try
            {
                await Database.CreateTableAsync<User>();
                Debug.WriteLine("UserRepository: Created User table");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UserRepository: Error creating table - {ex.Message}");
            }
        }
        
        /// <summary>
        /// Gets a user by username
        /// </summary>
        public async Task<User> GetByUsernameAsync(string username)
        {
            if (string.IsNullOrEmpty(username))
                return null;
                
            try
            {
                await EnsureDatabaseAsync();
                return await Database.Table<User>()
                    .Where(u => u.Username == username)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UserRepository: Error getting user by username - {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Gets a user by username with cancellation support
        /// </summary>
        public async Task<User> GetByUsernameAsync(string username, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(username))
                return null;
                
            try
            {
                await EnsureDatabaseAsync(cancellationToken);
                return await Database.Table<User>()
                    .Where(u => u.Username == username)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UserRepository: Error getting user by username - {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Checks if a username exists
        /// </summary>
        public async Task<bool> UsernameExistsAsync(string username)
        {
            if (string.IsNullOrEmpty(username))
                return false;
                
            try
            {
                await EnsureDatabaseAsync();
                return await Database.Table<User>()
                    .Where(u => u.Username == username)
                    .CountAsync() > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UserRepository: Error checking username existence - {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Validates user credentials
        /// </summary>
        public async Task<bool> ValidateCredentialsAsync(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return false;
                
            try
            {
                var user = await GetByUsernameAsync(username);
                if (user == null)
                    return false;
                    
                // Verify password using BCrypt
                return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UserRepository: Error validating credentials - {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Gets a user by display name
        /// </summary>
        public async Task<User> GetByDisplayNameAsync(string displayName)
        {
            if (string.IsNullOrEmpty(displayName))
                return null;
                
            try
            {
                await EnsureDatabaseAsync();
                return await Database.Table<User>()
                    .Where(u => u.DisplayName == displayName)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UserRepository: Error getting user by display name - {ex.Message}");
                return null;
            }
        }
    }
}
