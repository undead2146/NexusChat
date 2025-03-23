using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Core.Models;
using NexusChat.Data.Context;
using SQLite;

namespace NexusChat.Data.Repositories
{
    /// <summary>
    /// Repository for Message data access operations
    /// </summary>
    public class MessageRepository : BaseRepository<Message>, IMessageRepository
    {
        /// <summary>
        /// Initializes a new repository instance
        /// </summary>
        /// <param name="databaseService">Database service</param>
        public MessageRepository(DatabaseService databaseService) : base(databaseService)
        {
        }
        
        /// <summary>
        /// Gets messages for a specific conversation
        /// </summary>
        public async Task<List<Message>> GetByConversationIdAsync(
            int conversationId, 
            int limit = 100, 
            int offset = 0, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _databaseService.Initialize(cancellationToken);
                return await _databaseService.Database.Table<Message>()
                    .Where(m => m.ConversationId == conversationId)
                    .OrderBy(m => m.Timestamp) // Order by timestamp to get messages in chronological order
                    .Skip(offset)
                    .Take(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetByConversationIdAsync: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Gets the most recent message in a conversation
        /// </summary>
        public async Task<Message> GetMostRecentMessageAsync(
            int conversationId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _databaseService.Initialize(cancellationToken);
                return await _databaseService.Database.Table<Message>()
                    .Where(m => m.ConversationId == conversationId)
                    .OrderByDescending(m => m.Timestamp)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetMostRecentMessageAsync: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Gets messages that match a search term
        /// </summary>
        public async Task<List<Message>> SearchMessagesAsync(
            string searchTerm,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<Message>();
                
            try
            {
                await _databaseService.Initialize(cancellationToken);
                
                // SQLite doesn't support LIKE with parameters directly in query expressions
                // So we'll get all messages and filter in memory
                var allMessages = await _databaseService.Database.Table<Message>().ToListAsync();
                return allMessages.Where(m => m.Content.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SearchMessagesAsync: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Gets the count of messages in a conversation
        /// </summary>
        public async Task<int> GetMessageCountAsync(int conversationId, CancellationToken cancellationToken = default)
        {
            try
            {
                await _databaseService.Initialize(cancellationToken);
                return await _databaseService.Database.Table<Message>()
                    .Where(m => m.ConversationId == conversationId)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetMessageCountAsync: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Deletes all messages for a conversation
        /// </summary>
        public async Task<int> DeleteByConversationIdAsync(int conversationId, CancellationToken cancellationToken = default)
        {
            try
            {
                await _databaseService.Initialize(cancellationToken);
                return await _databaseService.Database.ExecuteAsync(
                    "DELETE FROM Message WHERE ConversationId = ?", 
                    conversationId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in DeleteByConversationIdAsync: {ex.Message}");
                throw;
            }
        }
    }
}
