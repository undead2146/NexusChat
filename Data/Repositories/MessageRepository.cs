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
using SQLiteNetExtensionsAsync.Extensions;

namespace NexusChat.Data.Repositories
{
    /// <summary>
    /// Repository implementation for Message data access
    /// </summary>
    public class MessageRepository : BaseRepository<Message>, IMessageRepository
    {
        private readonly DatabaseService _databaseService;

        /// <summary>
        /// Initializes a new instance of the MessageRepository
        /// </summary>
        /// <param name="databaseService">Database service</param>
        public MessageRepository(DatabaseService databaseService)
            : base(databaseService.Database) // Pass the SQLiteAsyncConnection to the base constructor
        {
            _databaseService = databaseService;
        }

        /// <summary>
        /// Gets all messages for a conversation
        /// </summary>
        public async Task<List<Message>> GetMessagesByConversationAsync(int conversationId)
        {
            try
            {
                var query = _databaseService.Database.Table<Message>()
                    .Where(m => m.ConversationId == conversationId)
                    .OrderBy(m => m.Timestamp)
                    .ToListAsync();

                return await WithCancellation(query, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting messages for conversation {conversationId}: {ex.Message}");
                return new List<Message>();
            }
        }

        /// <summary>
        /// Gets all messages for a conversation with cancellation support
        /// </summary>
        public async Task<List<Message>> GetMessagesByConversationAsync(int conversationId, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var query = _databaseService.Database.Table<Message>()
                    .Where(m => m.ConversationId == conversationId)
                    .OrderBy(m => m.Timestamp)
                    .ToListAsync();

                return await WithCancellation(query, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting messages for conversation {conversationId}: {ex.Message}");
                return new List<Message>();
            }
        }

        /// <summary>
        /// Gets messages for a conversation with pagination
        /// </summary>
        public async Task<List<Message>> GetMessagesByConversationAsync(int conversationId, int limit, int offset)
        {
            try
            {
                var query = _databaseService.Database.Table<Message>()
                    .Where(m => m.ConversationId == conversationId)
                    .OrderBy(m => m.Timestamp)
                    .Skip(offset)
                    .Take(limit)
                    .ToListAsync();

                return await WithCancellation(query, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting paginated messages for conversation {conversationId}: {ex.Message}");
                return new List<Message>();
            }
        }

        /// <summary>
        /// Gets messages for a conversation with pagination and cancellation support
        /// </summary>
        public async Task<List<Message>> GetMessagesByConversationAsync(int conversationId, int limit, int offset, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var query = _databaseService.Database.Table<Message>()
                    .Where(m => m.ConversationId == conversationId)
                    .OrderBy(m => m.Timestamp)
                    .Skip(offset)
                    .Take(limit)
                    .ToListAsync();

                return await WithCancellation(query, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting paginated messages for conversation {conversationId}: {ex.Message}");
                return new List<Message>();
            }
        }

        /// <summary>
        /// Gets the most recent message for a conversation
        /// </summary>
        public async Task<Message> GetLastMessageAsync(int conversationId)
        {
            try
            {
                var query = _databaseService.Database.Table<Message>()
                    .Where(m => m.ConversationId == conversationId)
                    .OrderByDescending(m => m.Timestamp)
                    .FirstOrDefaultAsync();

                return await WithCancellation(query, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting last message for conversation {conversationId}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets the most recent message for a conversation with cancellation support
        /// </summary>
        public async Task<Message> GetLastMessageAsync(int conversationId, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var query = _databaseService.Database.Table<Message>()
                    .Where(m => m.ConversationId == conversationId)
                    .OrderByDescending(m => m.Timestamp)
                    .FirstOrDefaultAsync();

                return await WithCancellation(query, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting last message for conversation {conversationId}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets the count of messages for a conversation
        /// </summary>
        public async Task<int> GetMessageCountAsync(int conversationId)
        {
            try
            {
                var query = _databaseService.Database.Table<Message>()
                    .Where(m => m.ConversationId == conversationId)
                    .CountAsync();

                return await WithCancellation(query, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting message count for conversation {conversationId}: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Gets the count of messages for a conversation with cancellation support
        /// </summary>
        public async Task<int> GetMessageCountAsync(int conversationId, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var query = _databaseService.Database.Table<Message>()
                    .Where(m => m.ConversationId == conversationId)
                    .CountAsync();

                return await WithCancellation(query, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting message count for conversation {conversationId}: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Deletes all messages for a conversation
        /// </summary>
        public async Task<int> DeleteByConversationAsync(int conversationId)
        {
            try
            {
                var query = _databaseService.Database.ExecuteAsync(
                    "DELETE FROM Messages WHERE ConversationId = ?", 
                    conversationId);

                return await WithCancellation(query, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting messages for conversation {conversationId}: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Deletes all messages for a conversation with cancellation support
        /// </summary>
        public async Task<int> DeleteByConversationAsync(int conversationId, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var query = _databaseService.Database.ExecuteAsync(
                    "DELETE FROM Messages WHERE ConversationId = ?", 
                    conversationId);

                return await WithCancellation(query, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting messages for conversation {conversationId}: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Adds a message to a conversation
        /// </summary>
        public async Task<int> AddMessageAsync(Message message, int conversationId)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            try
            {
                message.ConversationId = conversationId;

                if (message.Timestamp == default)
                {
                    message.Timestamp = DateTime.Now;
                }

                var query = _databaseService.Database.InsertAsync(message);

                return await WithCancellation(query, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error adding message to conversation {conversationId}: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Adds a message to a conversation with cancellation support
        /// </summary>
        public async Task<int> AddMessageAsync(Message message, int conversationId, CancellationToken cancellationToken)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                message.ConversationId = conversationId;

                if (message.Timestamp == default)
                {
                    message.Timestamp = DateTime.Now;
                }

                var query = _databaseService.Database.InsertAsync(message);

                return await WithCancellation(query, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error adding message to conversation {conversationId}: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Gets all messages for a conversation (alias for backward compatibility)
        /// </summary>
        public async Task<List<Message>> GetByConversationIdAsync(int conversationId)
        {
            return await GetMessagesByConversationAsync(conversationId);
        }

        /// <summary>
        /// Deletes all messages for a conversation (alias for backward compatibility)
        /// </summary>
        public async Task<int> DeleteByConversationIdAsync(int conversationId)
        {
            return await DeleteByConversationAsync(conversationId);
        }

        private async Task<T> WithCancellation<T>(Task<T> task, CancellationToken cancellationToken)
        {
            // For SQLite operations that don't natively support cancellation
            var tcs = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(() => tcs.TrySetResult(true)))
            {
                if (await Task.WhenAny(task, tcs.Task) == tcs.Task && cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(cancellationToken);
                }
            }
            
            return await task;
        }
    }
}
