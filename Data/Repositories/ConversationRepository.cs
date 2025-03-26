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

        /// <summary>
        /// Deletes a conversation by its identifier
        /// </summary>
        /// <param name="id">The conversation identifier</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Number of rows affected</returns>
        public async Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                await _databaseService.Initialize(cancellationToken);
                
                // First check if the table exists
                bool tableExists = await _databaseService.TableExistsAsync("Conversations");
                if (!tableExists)
                {
                    Debug.WriteLine("Conversations table doesn't exist. Creating it now...");
                    await _databaseService.Database.CreateTableAsync<Conversation>();
                    Debug.WriteLine("Conversations table created");
                    return 0;
                }
                
                // Delete the conversation by ID
                int rowsAffected = await _databaseService.Database.DeleteAsync<Conversation>(id);
                Debug.WriteLine($"Deleted conversation {id}: {rowsAffected} rows affected");
                return rowsAffected;
            }
            catch (SQLiteException ex)
            {
                Debug.WriteLine($"SQLite error in ConversationRepository.DeleteAsync: {ex.Message}");
                return 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ConversationRepository.DeleteAsync: {ex.Message}");
                return 0;
            }
        }

        // Regular implementation to satisfy the interface
        async Task<bool> IRepository<Conversation>.DeleteAsync(int id, CancellationToken cancellationToken)
        {
            int rowsAffected = await DeleteAsync(id, cancellationToken);
            return rowsAffected > 0;
        }
    }
}
