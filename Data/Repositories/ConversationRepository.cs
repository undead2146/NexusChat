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
    /// Repository for Conversation data access operations
    /// </summary>
    public class ConversationRepository : BaseRepository<Conversation>, IConversationRepository
    {
        /// <summary>
        /// Initializes a new repository instance
        /// </summary>
        /// <param name="databaseService">Database service</param>
        public ConversationRepository(DatabaseService databaseService) : base(databaseService)
        {
        }
        
        /// <summary>
        /// Gets conversations for a specific user
        /// </summary>
        public async Task<List<Conversation>> GetByUserIdAsync(
            int userId, 
            int limit = 50, 
            int offset = 0, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _databaseService.Initialize(cancellationToken);
                return await _databaseService.Database.Table<Conversation>()
                    .Where(c => c.UserId == userId)
                    .OrderByDescending(c => c.UpdatedAt)
                    .Skip(offset)
                    .Take(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetByUserIdAsync: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Gets favorite conversations for a user
        /// </summary>
        public async Task<List<Conversation>> GetFavoritesAsync(int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                await _databaseService.Initialize(cancellationToken);
                return await _databaseService.Database.Table<Conversation>()
                    .Where(c => c.UserId == userId && c.IsFavorite)
                    .OrderByDescending(c => c.UpdatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetFavoritesAsync: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Gets conversations by category
        /// </summary>
        public async Task<List<Conversation>> GetByCategoryAsync(
            int userId, 
            string category, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _databaseService.Initialize(cancellationToken);
                return await _databaseService.Database.Table<Conversation>()
                    .Where(c => c.UserId == userId && c.Category == category)
                    .OrderByDescending(c => c.UpdatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetByCategoryAsync: {ex.Message}");
                throw;
            }
        }
    }
}
