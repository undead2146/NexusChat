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
using SQLiteNetExtensions.Extensions;

namespace NexusChat.Data.Repositories
{
    /// <summary>
    /// Repository for conversation data
    /// </summary>
    public class ConversationRepository : BaseRepository<Conversation>, IConversationRepository
    {
        private readonly DatabaseService _databaseService;
        private readonly IMessageRepository _messageRepository;
        private SQLiteAsyncConnection? _connection;
        private SQLiteConnection? _database;

        /// <summary>
        /// Creates a new instance of ConversationRepository
        /// </summary>
        public ConversationRepository(DatabaseService databaseService, IMessageRepository messageRepository)
            : base(databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
            
            // Call schema migration on initialization
            EnsureSchemaUpdatedAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Initializes the repository
        /// </summary>
        protected async Task InitializeAsync()
        {
            try
            {
                if (_connection == null)
                {
                    _connection = _databaseService.GetAsyncConnection();
                    _database = _databaseService.GetSyncConnection();
                    await _databaseService.EnsureInitializedAsync();
                    
                    // Ensure schema is up to date
                    await EnsureSchemaUpdatedAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing ConversationRepository: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Ensures the database schema is up to date with the model
        /// </summary>
        private async Task EnsureSchemaUpdatedAsync()
        {
            try
            {
                Debug.WriteLine("Checking for schema updates in Conversations table");
                var connection = await _databaseService.GetConnectionAsync();
                
                // Get existing columns
                var tableInfo = await connection.GetTableInfoAsync("Conversations");
                
                // Check if ModelName column exists - if not, add it
                if (!tableInfo.Any(c => c.Name.Equals("ModelName", StringComparison.OrdinalIgnoreCase)))
                {
                    Debug.WriteLine("Adding ModelName column to Conversations table");
                    await connection.ExecuteAsync("ALTER TABLE Conversations ADD COLUMN ModelName TEXT");
                }
                
                // Check if ProviderName column exists - if not, add it
                if (!tableInfo.Any(c => c.Name.Equals("ProviderName", StringComparison.OrdinalIgnoreCase)))
                {
                    Debug.WriteLine("Adding ProviderName column to Conversations table");
                    await connection.ExecuteAsync("ALTER TABLE Conversations ADD COLUMN ProviderName TEXT");
                }
                
                Debug.WriteLine("Schema update check completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking/updating schema: {ex.Message}");
                // Don't throw - we want initialization to continue even if this fails
            }
        }

        // Removed invalid override of AddAsync(Conversation entity)

        /// <summary>
        /// Gets all conversations with paging
        /// </summary>
        public async Task<List<Conversation>> GetConversationsAsync(int limit = 50, int offset = 0, CancellationToken cancellationToken = default)
        {
            try
            {
                await InitializeAsync();

                var query = _connection.Table<Conversation>()
                    .OrderByDescending(c => c.UpdatedAt)
                    .Skip(offset)
                    .Take(limit);

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ConversationRepository: Error getting conversations - {ex.Message}");
                return new List<Conversation>();
            }
        }

        /// <summary>
        /// Gets all conversations for a user
        /// </summary>
        public async Task<List<Conversation>> GetByUserIdAsync(int userId)
        {
            return await GetByUserIdAsync(userId, CancellationToken.None);
        }

        /// <summary>
        /// Gets all conversations for a user with cancellation support
        /// </summary>
        public async Task<List<Conversation>> GetByUserIdAsync(int userId, CancellationToken cancellationToken)
        {
            try
            {
                await InitializeAsync();
                
                var query = _connection!.Table<Conversation>()
                    .Where(c => c.UserId == userId)
                    .OrderByDescending(c => c.UpdatedAt);

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ConversationRepository: Error getting conversations by user - {ex.Message}");
                return new List<Conversation>();
            }
        }

        /// <summary>
        /// Gets all conversations for a user with paging
        /// </summary>
        public async Task<List<Conversation>> GetByUserIdAsync(int userId, int limit, int offset, CancellationToken cancellationToken = default)
        {
            try
            {
                await InitializeAsync();
                
                var query = _connection!.Table<Conversation>()
                    .Where(c => c.UserId == userId)
                    .OrderByDescending(c => c.UpdatedAt)
                    .Skip(offset)
                    .Take(limit);

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ConversationRepository: Error getting conversations by user with paging - {ex.Message}");
                return new List<Conversation>();
            }
        }

        /// <summary>
        /// Gets all favorite conversations for a user
        /// </summary>
        public async Task<List<Conversation>> GetfavoritesAsync(int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                await InitializeAsync();
                
                var query = _connection!.Table<Conversation>()
                    .Where(c => c.UserId == userId && c.IsFavorite)
                    .OrderByDescending(c => c.UpdatedAt);

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ConversationRepository: Error getting favorite conversations - {ex.Message}");
                return new List<Conversation>();
            }
        }

        /// <summary>
        /// Gets all active (non-archived) conversations
        /// </summary>
        public async Task<List<Conversation>> GetActiveConversationsAsync()
        {
            return await GetActiveConversationsAsync(CancellationToken.None);
        }

        /// <summary>
        /// Gets all active (non-archived) conversations with cancellation support
        /// </summary>
        public async Task<List<Conversation>> GetActiveConversationsAsync(CancellationToken cancellationToken)
        {
            try
            {
                await InitializeAsync();
                
                var query = _connection!.Table<Conversation>()
                    .Where(c => !c.IsArchived)
                    .OrderByDescending(c => c.UpdatedAt);

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ConversationRepository: Error getting active conversations - {ex.Message}");
                return new List<Conversation>();
            }
        }

        /// <summary>
        /// Gets all conversations with a specific tag/category for a user
        /// </summary>
        public async Task<List<Conversation>> GetByCategoryAsync(int userId, string category, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(category))
                return new List<Conversation>();
                
            try
            {
                await InitializeAsync();
                
                var query = _connection!.Table<Conversation>()
                    .Where(c => c.UserId == userId && c.Category == category)
                    .OrderByDescending(c => c.UpdatedAt);

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ConversationRepository: Error getting conversations by category - {ex.Message}");
                return new List<Conversation>();
            }
        }

        /// <summary>
        /// Gets all conversations with a specific tag
        /// </summary>
        public async Task<List<Conversation>> GetByTagAsync(string tag, int limit = 50, int offset = 0, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(tag))
                return new List<Conversation>();
                
            try
            {
                await InitializeAsync();
                
                var query = _connection!.Table<Conversation>()
                    .Where(c => c.Category == tag)
                    .OrderByDescending(c => c.UpdatedAt)
                    .Skip(offset)
                    .Take(limit);

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ConversationRepository: Error getting conversations by tag - {ex.Message}");
                return new List<Conversation>();
            }
        }

        /// <summary>
        /// Gets all conversations with a specific model ID
        /// </summary>
        public async Task<List<Conversation>> GetByModelIdAsync(int modelId)
        {
            return await GetByModelIdAsync(modelId, CancellationToken.None);
        }

        /// <summary>
        /// Gets all conversations with a specific model ID with cancellation support
        /// </summary>
        public async Task<List<Conversation>> GetByModelIdAsync(int modelId, CancellationToken cancellationToken)
        {
            if (modelId <= 0)
                return new List<Conversation>();
                
            try
            {
                await Task.Delay(1); // Make method truly async
                // TODO: Needs implementation based on how model IDs are stored in conversations
                // For now just return empty list as this is a new method
                return new List<Conversation>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ConversationRepository: Error getting conversations by model ID - {ex.Message}");
                return new List<Conversation>();
            }
        }

        /// <summary>
        /// Gets all conversations with a specific model
        /// </summary>
        public async Task<List<Conversation>> GetByModelAsync(string modelName, int limit = 50, int offset = 0, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(modelName))
                return new List<Conversation>();
                
            try
            {
                await InitializeAsync();
                
                var query = _connection!.Table<Conversation>()
                    .Where(c => c.ModelName == modelName)
                    .OrderByDescending(c => c.UpdatedAt)
                    .Skip(offset)
                    .Take(limit);

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ConversationRepository: Error getting conversations by model - {ex.Message}");
                return new List<Conversation>();
            }
        }

        /// <summary>
        /// Searches conversations by content
        /// </summary>
        public async Task<List<Conversation>> SearchAsync(string searchTerm)
        {
            return await SearchAsync(searchTerm, CancellationToken.None);
        }

        /// <summary>
        /// Searches conversations by content with cancellation support
        /// </summary>
        public async Task<List<Conversation>> SearchAsync(string searchTerm, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return new List<Conversation>();
                
            try
            {
                await InitializeAsync();
                
                var query = _connection!.Table<Conversation>()
                    .Where(c => c.Title.Contains(searchTerm) ||
                                (c.Summary != null && c.Summary.Contains(searchTerm)));

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ConversationRepository: Error searching conversations - {ex.Message}");
                return new List<Conversation>();
            }
        }


        /// <summary>
        /// Implements the required abstract SearchAsync(string searchText, int limit) from BaseRepository
        /// </summary>
        public override async Task<List<Conversation>> SearchAsync(string searchText, int limit = 50)
        {
            if (string.IsNullOrEmpty(searchText))
                return new List<Conversation>();

            try
            {
                await InitializeAsync();
                var query = _connection!.Table<Conversation>()
                    .Where(c => c.Title.Contains(searchText) ||
                                (c.Summary != null && c.Summary.Contains(searchText)))
                    .OrderByDescending(c => c.UpdatedAt)
                    .Take(limit);

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ConversationRepository: Error searching conversations (abstract): {ex.Message}");
                return new List<Conversation>();
            }
        }

        /// <summary>
        /// Gets all messages for a conversation
        /// </summary>
        public async Task<List<Message>> GetMessagesAsync(int conversationId, int limit = 100, int offset = 0, CancellationToken cancellationToken = default)
        {
            try
            {
                await InitializeAsync();
                
                var messages = await _connection!.Table<Message>()
                    .Where(m => m.ConversationId == conversationId)
                    .OrderBy(m => m.Timestamp)
                    .Skip(offset)
                    .Take(limit)
                    .ToListAsync();

                return messages;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ConversationRepository: Error getting messages - {ex.Message}");
                return new List<Message>();
            }
        }

        /// <summary>
        /// Gets all messages for a conversation in reverse order
        /// </summary>
        public async Task<List<Message>> GetMessagesReverseAsync(int conversationId, int limit = 100, int offset = 0, CancellationToken cancellationToken = default)
        {
            try
            {
                await InitializeAsync();
                
                var messages = await _connection!.Table<Message>()
                    .Where(m => m.ConversationId == conversationId)
                    .OrderByDescending(m => m.Timestamp)
                    .Skip(offset)
                    .Take(limit)
                    .ToListAsync();

                messages.Reverse(); // Reverse to get chronological order
                return messages;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ConversationRepository: Error getting messages in reverse - {ex.Message}");
                return new List<Message>();
            }
        }

        /// <summary>
        /// Adds a message to a conversation
        /// </summary>
        public async Task<int> AddMessageAsync(Message message, CancellationToken cancellationToken = default)
        {
            if (message == null)
                return 0;
                
            try
            {
                await InitializeAsync();
                
                // Get the conversation
                var conversation = await GetByIdAsync(message.ConversationId, cancellationToken);
                if (conversation != null)
                {
                    // Update conversation timestamp
                    conversation.UpdatedAt = DateTime.Now;

                    // If this is first message, update title
                    if (string.IsNullOrEmpty(conversation.Title) || conversation.Title == "New Chat")
                    {
                        conversation.Title = message.Content.Length > 50
                            ? message.Content.Substring(0, 47) + "..."
                            : message.Content;
                    }
                
                    // Save message
                    int messageId = await _messageRepository.AddAsync(message);

                    // Update conversation
                    cancellationToken.ThrowIfCancellationRequested(); // Honor cancellation before call
                    int id = await AddAsync(conversation);

                    return messageId;
                }
                
                return 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ConversationRepository: Error adding message - {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Counts messages in a conversation
        /// </summary>
        public async Task<int> GetMessageCountAsync(int conversationId)
        {
            try
            {
                await InitializeAsync();
                
                return await _connection!.Table<Message>()
                    .Where(m => m.ConversationId == conversationId)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ConversationRepository: Error counting messages - {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Gets the latest message in a conversation
        /// </summary>
        public async Task<Message?> GetLatestMessageAsync(int conversationId)
        {
            try
            {
                await InitializeAsync();
                
                return await _connection!.Table<Message>()
                    .Where(m => m.ConversationId == conversationId)
                    .OrderByDescending(m => m.Timestamp)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ConversationRepository: Error getting latest message - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Updates the last activity timestamp for a conversation
        /// </summary>
        public async Task<bool> UpdateLastActivityAsync(int conversationId, CancellationToken cancellationToken = default)
        {
            try
            {
                var conversation = await GetByIdAsync(conversationId, cancellationToken);
                if (conversation != null)
                {
                    conversation.UpdatedAt = DateTime.UtcNow;
                    return await UpdateAsync(conversation, cancellationToken);
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating conversation last activity: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Adds a new conversation and returns the ID
        /// </summary>
        public async Task<int> AddConversationAsync(Conversation conversation, CancellationToken cancellationToken = default)
        {
            try
            {
                return await AddAsync(conversation, cancellationToken);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error adding conversation: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Gets all conversations
        /// </summary>
        public async Task<List<Conversation>> GetAllConversationsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await GetAllAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting all conversations: {ex.Message}");
                return new List<Conversation>();
            }
        }
        
        /// <summary>
        /// Gets a conversation by ID
        /// </summary>
        public async Task<Conversation?> GetConversationByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                return await GetByIdAsync(id, cancellationToken);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting conversation by ID: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Gets all recent conversations
        /// </summary>
        public async Task<List<Conversation>> GetRecentAsync(int limit = 10, CancellationToken cancellationToken = default)
        {
            try
            {
                await InitializeAsync();
                return await _connection!.Table<Conversation>()
                    .OrderByDescending(c => c.UpdatedAt)
                    .Take(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting recent conversations: {ex.Message}");
                return new List<Conversation>();
            }
        }
        
        /// <summary>
        /// Updates a conversation in the database
        /// </summary>
        public async Task<bool> UpdateConversationAsync(Conversation conversation, CancellationToken cancellationToken = default)
        {
            if (conversation == null)
                return false;
                
            try
            {
                return await UpdateAsync(conversation, cancellationToken);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating conversation: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Deletes a conversation from the database
        /// </summary>
        public async Task<bool> DeleteConversationAsync(int id, CancellationToken cancellationToken = default)
        {
            if (id <= 0)
                return false;
                
            try
            {
                return await DeleteAsync(id, cancellationToken);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting conversation: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Conversation>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                await InitializeAsync();
                
                var conversations = await _connection!.Table<Conversation>()
                    .Where(c => c.UserId == (int)userId.GetHashCode()) // Convert Guid to int for compatibility
                    .OrderByDescending(c => c.UpdatedAt)
                    .ToListAsync();

                return conversations;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting conversations by user ID: {ex.Message}");
                return new List<Conversation>();
            }
        }

        /// <summary>
        /// Gets the most recent conversation for a user
        /// </summary>
        public async Task<Conversation?> GetMostRecentByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                await InitializeAsync();
                
                return await _connection!.Table<Conversation>()
                    .Where(c => c.UserId == userId)
                    .OrderByDescending(c => c.UpdatedAt)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ConversationRepository: Error getting most recent conversation by user - {ex.Message}");
                return null;
            }
        }
    }
}
