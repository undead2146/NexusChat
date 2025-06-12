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
        public async Task<List<Message>> GetMessagesByConversationIdAsync(int conversationId, CancellationToken cancellationToken = default)
        {
            try
            {
                var db = _dbService.Database;
                return await db.Table<Message>()
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
        public async Task<List<Message>> GetMessagesByConversationIdBeforeTimestampAsync(int conversationId, DateTime timestamp, int limit, CancellationToken cancellationToken = default)
        {
            try
            {
                var db = _dbService.Database;
                return await db.Table<Message>()
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
        public async Task<int> GetMessageCountForConversationAsync(int conversationId, CancellationToken cancellationToken = default)
        {
            try
            {
                var db = _dbService.Database;
                return await db.Table<Message>()
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
        public async Task<List<Message>> GetMessagesByConversationAsync(int conversationId, CancellationToken cancellationToken = default)
        {
            return await GetMessagesByConversationIdAsync(conversationId, cancellationToken);
        }

        /// <summary>
        /// Adds a message to the database
        /// </summary>
        public async Task<int> AddMessageAsync(Message message, CancellationToken cancellationToken = default)
        {
            if (message == null)
                return 0;

            return await AddAsync(message, cancellationToken);
        }

        /// <summary>
        /// Updates a message in the database
        /// </summary>
        public async Task<bool> UpdateMessageAsync(Message message, CancellationToken cancellationToken = default)
        {
            if (message == null)
                return false;

            return await UpdateAsync(message, cancellationToken);
        }

        /// <summary>
        /// Deletes all messages for a conversation
        /// </summary>
        public async Task<bool> DeleteAllMessagesForConversationAsync(int conversationId, CancellationToken cancellationToken = default)
        {
            try
            {
                var db = _dbService.Database;
                await db.ExecuteAsync("DELETE FROM Message WHERE ConversationId = ?", conversationId);
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
        public async Task<bool> DeleteByConversationIdAsync(int conversationId, CancellationToken cancellationToken = default)
        {
            return await DeleteAllMessagesForConversationAsync(conversationId, cancellationToken);
        }

        /// <summary>
        /// Gets messages for a specific conversation with pagination
        /// </summary>
        public async Task<List<Message>> GetByConversationIdAsync(int conversationId, int limit = 20, int offset = 0, CancellationToken cancellationToken = default)
        {
            try
            {
                var db = _dbService.Database;
                return await db.Table<Message>()
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

        /// <summary>
        /// Checks if a message belongs to a specific conversation
        /// </summary>
        public async Task<bool> BelongsToConversationAsync(int messageId, int conversationId, CancellationToken cancellationToken = default)
        {
            try
            {
                var db = _dbService.Database;
                var message = await db.Table<Message>().Where(m => m.Id == messageId && m.ConversationId == conversationId).FirstOrDefaultAsync();
                return message != null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking message conversation: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Moves a message to a new conversation
        /// </summary>
        public async Task<bool> MoveMessageToConversationAsync(int messageId, int newConversationId, CancellationToken cancellationToken = default)
        {
            try
            {
                var db = _dbService.Database;
                var message = await db.Table<Message>().Where(m => m.Id == messageId).FirstOrDefaultAsync();
                if (message == null)
                    return false;
                message.ConversationId = newConversationId;
                await db.UpdateAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error moving message to new conversation: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Deletes messages from a specific conversation
        /// </summary>
        public async Task<bool> DeleteMessagesFromConversationAsync(int conversationId, CancellationToken cancellationToken = default)
        {
            return await DeleteAllMessagesForConversationAsync(conversationId, cancellationToken);
        }

        /// <summary>
        /// Implements abstract SearchAsync for BaseRepository
        /// </summary>
        public override async Task<List<Message>> SearchAsync(string searchText, int limit = 50)
        {
            try
            {
                var db = _dbService.Database;
                return await db.Table<Message>()
                    .Where(m => m.Content.Contains(searchText))
                    .OrderByDescending(m => m.Timestamp)
                    .Take(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error searching messages: {ex.Message}");
                return new List<Message>();
            }
        }

        /// <summary>
        /// Gets the last message for a specific conversation
        /// </summary>
        public async Task<Message?> GetLastMessageForConversationAsync(int conversationId, CancellationToken cancellationToken = default)
        {
            try
            {
                var db = _dbService.Database;
                var lastMessage = await db.Table<Message>()
                    .Where(m => m.ConversationId == conversationId)
                    .OrderByDescending(m => m.Timestamp)
                    .FirstOrDefaultAsync();

                return lastMessage;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting last message for conversation: {ex.Message}");
                return null;
            }
        }
    }
}
