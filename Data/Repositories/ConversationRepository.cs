using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Core.Models;
using NexusChat.Data.Context;
using NexusChat.Data.Interfaces;
using SQLite;

namespace NexusChat.Data.Repositories
{
    /// <summary>
    /// Repository implementation for Conversation data access
    /// </summary>
    public class ConversationRepository : BaseRepository<Conversation>, IConversationRepository
    {
        /// <summary>
        /// Initializes a new instance of the ConversationRepository class
        /// </summary>
        /// <param name="databaseService">The DatabaseService instance</param>
        public ConversationRepository(DatabaseService databaseService) 
            : base(databaseService.GetConnection())
        {
            // Initialize database table if it doesn't exist
            _database.CreateTableAsync<Conversation>().Wait();
        }

        /// <summary>
        /// Gets conversations by user ID
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>A list of conversations</returns>
        public async Task<List<Conversation>> GetByUserIdAsync(int userId)
        {
            try
            {
                var query = _database.Table<Conversation>()
                    .Where(c => c.UserId == userId)
                    .OrderByDescending(c => c.LastMessageDate)
                    .ToListAsync();
                    
                return await WithCancellation(query, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting conversations by user ID: {ex.Message}");
                return new List<Conversation>();
            }
        }

        /// <summary>
        /// Gets conversations by user ID with cancellation support
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A list of conversations</returns>
        public async Task<List<Conversation>> GetByUserIdAsync(int userId, CancellationToken cancellationToken)
        {
            try
            {
                var query = _database.Table<Conversation>()
                    .Where(c => c.UserId == userId)
                    .OrderByDescending(c => c.LastMessageDate)
                    .ToListAsync();
                    
                return await WithCancellation(query, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting conversations by user ID: {ex.Message}");
                return new List<Conversation>();
            }
        }

        /// <summary>
        /// Gets paginated conversations by user ID
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="limit">Max number of results</param>
        /// <param name="offset">Result offset for pagination</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A list of conversations</returns>
        public async Task<List<Conversation>> GetByUserIdAsync(
            int userId, 
            int limit = 50, 
            int offset = 0, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                // SQLite doesn't support Skip/Take in LINQ with async, so we'll load and filter
                var query = _database.Table<Conversation>()
                    .Where(c => c.UserId == userId)
                    .OrderByDescending(c => c.LastMessageDate)
                    .ToListAsync();
                    
                var allUserConversations = await WithCancellation(query, cancellationToken);
                
                return allUserConversations
                    .Skip(offset)
                    .Take(limit)
                    .ToList();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting paginated conversations by user ID: {ex.Message}");
                return new List<Conversation>();
            }
        }

        /// <summary>
        /// Gets active conversations
        /// </summary>
        /// <returns>A list of active conversations</returns>
        public async Task<List<Conversation>> GetActiveConversationsAsync()
        {
            try
            {
                var query = _database.Table<Conversation>()
                    .Where(c => c.IsActive)
                    .OrderByDescending(c => c.LastMessageDate)
                    .ToListAsync();
                    
                return await WithCancellation(query, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting active conversations: {ex.Message}");
                return new List<Conversation>();
            }
        }

        /// <summary>
        /// Gets active conversations with cancellation support
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A list of active conversations</returns>
        public async Task<List<Conversation>> GetActiveConversationsAsync(CancellationToken cancellationToken)
        {
            try
            {
                var query = _database.Table<Conversation>()
                    .Where(c => c.IsActive)
                    .OrderByDescending(c => c.LastMessageDate)
                    .ToListAsync();
                    
                return await WithCancellation(query, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting active conversations: {ex.Message}");
                return new List<Conversation>();
            }
        }

        /// <summary>
        /// Gets conversations by AI model ID
        /// </summary>
        /// <param name="modelId">The AI model ID</param>
        /// <returns>A list of conversations</returns>
        public async Task<List<Conversation>> GetByModelIdAsync(int modelId)
        {
            try
            {
                var query = _database.Table<Conversation>()
                    .Where(c => c.AIModelId == modelId)
                    .OrderByDescending(c => c.LastMessageDate)
                    .ToListAsync();
                    
                return await WithCancellation(query, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting conversations by AI model ID: {ex.Message}");
                return new List<Conversation>();
            }
        }

        /// <summary>
        /// Gets conversations by AI model ID with cancellation support
        /// </summary>
        /// <param name="modelId">The AI model ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A list of conversations</returns>
        public async Task<List<Conversation>> GetByModelIdAsync(int modelId, CancellationToken cancellationToken)
        {
            try
            {
                var query = _database.Table<Conversation>()
                    .Where(c => c.AIModelId == modelId)
                    .OrderByDescending(c => c.LastMessageDate)
                    .ToListAsync();
                    
                return await WithCancellation(query, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting conversations by AI model ID: {ex.Message}");
                return new List<Conversation>();
            }
        }

        /// <summary>
        /// Gets favorite conversations for a user
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>List of favorite conversations</returns>
        public async Task<List<Conversation>> GetFavoritesAsync(int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _database.Table<Conversation>()
                    .Where(c => c.UserId == userId && c.IsFavorite)
                    .OrderByDescending(c => c.LastMessageDate)
                    .ToListAsync();
                    
                return await WithCancellation(query, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting favorite conversations: {ex.Message}");
                return new List<Conversation>();
            }
        }

        /// <summary>
        /// Gets conversations by category
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="category">The category name</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>List of conversations in the category</returns>
        public async Task<List<Conversation>> GetByCategoryAsync(
            int userId, 
            string category, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(category))
                {
                    // Return conversations with no category
                    var query = _database.Table<Conversation>()
                        .Where(c => c.UserId == userId && c.Category == null)
                        .OrderByDescending(c => c.LastMessageDate)
                        .ToListAsync();
                        
                    return await WithCancellation(query, cancellationToken);
                }
                else
                {
                    var query = _database.Table<Conversation>()
                        .Where(c => c.UserId == userId && c.Category == category)
                        .OrderByDescending(c => c.LastMessageDate)
                        .ToListAsync();
                        
                    return await WithCancellation(query, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting conversations by category: {ex.Message}");
                return new List<Conversation>();
            }
        }

        /// <summary>
        /// Gets conversations with full-text search on title or content
        /// </summary>
        /// <param name="searchText">The text to search for</param>
        /// <returns>A list of matching conversations</returns>
        public async Task<List<Conversation>> SearchAsync(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return await GetAllAsync();
                
            try
            {
                var normalizedSearch = searchText.Trim().ToLowerInvariant();
                
                // We need to perform the search in memory as SQLite doesn't support full-text search in LINQ
                var query = _database.Table<Conversation>().ToListAsync();
                var allConversations = await WithCancellation(query, CancellationToken.None);
                
                return allConversations
                    .Where(c => 
                        (c.Title?.ToLowerInvariant().Contains(normalizedSearch) ?? false) || 
                        (c.Summary?.ToLowerInvariant().Contains(normalizedSearch) ?? false))
                    .OrderByDescending(c => c.LastMessageDate)
                    .ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error searching conversations: {ex.Message}");
                return new List<Conversation>();
            }
        }

        /// <summary>
        /// Gets conversations with full-text search on title or content with cancellation support
        /// </summary>
        /// <param name="searchText">The text to search for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A list of matching conversations</returns>
        public async Task<List<Conversation>> SearchAsync(string searchText, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return await GetAllAsync(cancellationToken);
                
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var normalizedSearch = searchText.Trim().ToLowerInvariant();
                
                // Create a task that can be cancelled
                var queryTask = _database.Table<Conversation>().ToListAsync();
                
                // Use Task.WhenAny to support cancellation
                var completedTask = await Task.WhenAny(queryTask, Task.Delay(-1, cancellationToken));
                
                // If the delay task completed first, cancellation was requested
                if (completedTask != queryTask && cancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException(cancellationToken);
                    
                // Get the result from the query task
                var allConversations = await queryTask;
                
                // Filter in memory
                return allConversations
                    .Where(c => 
                        (c.Title?.ToLowerInvariant().Contains(normalizedSearch) ?? false) || 
                        (c.Summary?.ToLowerInvariant().Contains(normalizedSearch) ?? false))
                    .OrderByDescending(c => c.LastMessageDate)
                    .ToList();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error searching conversations: {ex.Message}");
                return new List<Conversation>();
            }
        }
    }
}
