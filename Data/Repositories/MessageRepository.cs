using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NexusChat.Core.Models;
using NexusChat.Data.Context;
using NexusChat.Data.Interfaces;
using SQLite;

namespace NexusChat.Data.Repositories
{
    /// <summary>
    /// Repository for message data
    /// </summary>
    public class MessageRepository : BaseRepository<Message>, IMessageRepository
    {
        private readonly DatabaseService _dbService;

        /// <summary>
        /// Creates a new instance of MessageRepository
        /// </summary>
        /// <param name="dbService">Database service</param>
        public MessageRepository(DatabaseService dbService) : base(dbService)
        {
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
        }

        /// <summary>
        /// Gets messages for a specific conversation
        /// </summary>
        public async Task<List<Message>> GetMessagesByConversationIdAsync(int conversationId)
        {
            try
            {
                return await Database.Table<Message>()
                    .Where(m => m.ConversationId == conversationId)
                    .OrderBy(m => m.Timestamp)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting messages for conversation: {ex.Message}");
                return new List<Message>();
            }
        }

        /// <summary>
        /// Gets messages for a specific conversation before a timestamp
        /// </summary>
        public async Task<List<Message>> GetMessagesByConversationIdBeforeTimestampAsync(int conversationId, DateTime timestamp, int limit)
        {
            try
            {
                return await Database.Table<Message>()
                    .Where(m => m.ConversationId == conversationId && m.Timestamp < timestamp)
                    .OrderByDescending(m => m.Timestamp)
                    .Take(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting messages before timestamp: {ex.Message}");
                return new List<Message>();
            }
        }

        /// <summary>
        /// Gets the count of messages for a conversation
        /// </summary>
        public async Task<int> GetMessageCountForConversationAsync(int conversationId)
        {
            try
            {
                return await Database.Table<Message>()
                    .Where(m => m.ConversationId == conversationId)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting message count: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Gets messages for a specific conversation (alternative name for compatibility)
        /// </summary>
        public async Task<List<Message>> GetMessagesByConversationAsync(int conversationId)
        {
            return await GetMessagesByConversationIdAsync(conversationId);
        }

        /// <summary>
        /// Adds a message to the database
        /// </summary>
        public async Task<int> AddMessageAsync(Message message)
        {
            if (message == null)
                return 0;

            return await AddAsync(message);
        }

        /// <summary>
        /// Updates a message in the database
        /// </summary>
        public async Task<bool> UpdateMessageAsync(Message message)
        {
            if (message == null)
                return false;

            return await UpdateAsync(message);
        }

        /// <summary>
        /// Deletes all messages for a conversation
        /// </summary>
        public async Task<bool> DeleteAllMessagesForConversationAsync(int conversationId)
        {
            try
            {
                var connection = await _dbService.GetConnectionAsync();
                await connection.ExecuteAsync("DELETE FROM Messages WHERE ConversationId = ?", conversationId);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting conversation messages: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Deletes messages by conversation ID (alternative name for compatibility)
        /// </summary>
        public async Task<bool> DeleteByConversationIdAsync(int conversationId)
        {
            return await DeleteAllMessagesForConversationAsync(conversationId);
        }

        /// <summary>
        /// Gets messages for a specific conversation with pagination
        /// </summary>
        public async Task<List<Message>> GetByConversationIdAsync(int conversationId, int limit = 20, int offset = 0)
        {
            try
            {
                return await Database.Table<Message>()
                    .Where(m => m.ConversationId == conversationId)
                    .OrderBy(m => m.Timestamp)
                    .Skip(offset)
                    .Take(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting messages with pagination: {ex.Message}");
                return new List<Message>();
            }
        }
    }
}
