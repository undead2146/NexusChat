using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Core.Models;
using NexusChat.Data.Repositories;
using NexusChat.Services.Interfaces;

namespace NexusChat.Services
{
    /// <summary>
    /// Service that handles chat operations
    /// </summary>
    public class ChatService
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IConversationRepository _conversationRepository;
        private readonly IAIService _aiService;

        /// <summary>
        /// Initializes a new instance of the ChatService class
        /// </summary>
        public ChatService(IMessageRepository messageRepository, IConversationRepository conversationRepository, IAIService aiService)
        {
            _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
            _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
            _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
        }

        /// <summary>
        /// Loads an existing conversation or creates a new one
        /// </summary>
        public async Task<Conversation> LoadOrCreateConversationAsync(int conversationId, int modelId = 1, int userId = 1)
        {
            try
            {
                Debug.WriteLine("Attempting to load conversation");
                var conversation = await _conversationRepository.GetByIdAsync(conversationId);
                
                if (conversation == null)
                {
                    Debug.WriteLine("No conversation found, creating new one");
                    // Create a new conversation
                    conversation = new Conversation
                    {
                        UserId = userId,
                        Title = "New Chat",
                        ModelId = modelId,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now,
                        TotalTokensUsed = 0
                    };
                    
                    // Save the new conversation
                    await _conversationRepository.AddAsync(conversation);
                    Debug.WriteLine($"Created new conversation with ID: {conversation.Id}");
                }
                else
                {
                    Debug.WriteLine($"Loaded existing conversation with ID: {conversation.Id}");
                }

                return conversation;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading/creating conversation: {ex.Message}");
                
                // Create a local conversation object so the UI can still function
                return new Conversation
                {
                    Id = 0, // Temporary ID
                    UserId = userId,
                    Title = "Temporary Chat",
                    ModelId = modelId,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
            }
        }
        
        /// <summary>
        /// Gets messages for a conversation
        /// </summary>
        /// <param name="conversationId">The conversation ID</param>
        /// <param name="limit">Maximum number of messages to return</param>
        /// <param name="offset">Number of messages to skip</param>
        /// <returns>List of messages</returns>
        public async Task<List<Message>> GetMessageAsync(int conversationId, int limit = 100, int offset = 0)
        {
            try
            {
                return await _messageRepository.GetByConversationIdAsync(conversationId, limit, offset);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting messages: {ex.Message}");
                return new List<Message>();
            }
        }

        // Keep the original method for backward compatibility
        public async Task<List<Message>> GetMessagesAsync(int conversationId, int limit = 100)
        {
            return await GetMessageAsync(conversationId, limit, 0);
        }
        
        /// <summary>
        /// Sends a message to the AI and gets a response
        /// </summary>
        public async Task<string> GetAIResponseAsync(string userMessage, CancellationToken cancellationToken)
        {
            try
            {
                // Using a timeout for safety
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(60)); // 1 minute timeout
                
                return await _aiService.SendMessageAsync(userMessage, timeoutCts.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("AI request was canceled");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AI response error: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Saves a message to the database
        /// </summary>
        public async Task<Message> SaveMessageAsync(Message message)
        {
            try
            {
                if (message.ConversationId <= 0)
                {
                    throw new ArgumentException("ConversationId must be greater than 0");
                }
                
                return await _messageRepository.AddAsync(message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving message: {ex.Message}");
                return message; // Return original message even if save failed
            }
        }
        
        /// <summary>
        /// Updates a conversation
        /// </summary>
        public async Task<bool> UpdateConversationAsync(Conversation conversation)
        {
            try
            {
                return await _conversationRepository.UpdateAsync(conversation);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating conversation: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Clears all messages for a conversation
        /// </summary>
        public async Task<int> ClearConversationMessagesAsync(int conversationId)
        {
            try
            {
                await _messageRepository.EnsureDatabaseAsync();
                return await _messageRepository.DeleteByConversationIdAsync(conversationId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error clearing conversation messages: {ex.Message}");
                return 0;
            }
        }
        
        /// <summary>
        /// Deletes a conversation and its messages
        /// </summary>
        public async Task<bool> DeleteConversationAsync(int conversationId)
        {
            try
            {
                // First ensure both repositories are initialized
                await _messageRepository.EnsureDatabaseAsync();
                await _conversationRepository.EnsureDatabaseAsync();
                
                // Delete messages first
                int messagesDeleted = await _messageRepository.DeleteByConversationIdAsync(conversationId);
                Debug.WriteLine($"Deleted {messagesDeleted} messages for conversation {conversationId}");
                
                // Then delete the conversation
                int conversationsDeleted = await (_conversationRepository as ConversationRepository).DeleteAsync(conversationId);
                Debug.WriteLine($"Conversation delete result: {conversationsDeleted} rows affected");
                
                return conversationsDeleted > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting conversation: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Generates a title from the first message
        /// </summary>
        public string GenerateTitleFromMessage(string message)
        {
            try
            {
                // Simplified algorithm: take first few words or first sentence
                string title = message.Split('.')[0]; // First sentence
                
                // Limit length
                if (title.Length > 30)
                {
                    title = title.Substring(0, 27) + "...";
                }
                
                return title;
            }
            catch
            {
                return "New Chat";
            }
        }

        /// <summary>
        /// Gets the count of messages in a conversation
        /// </summary>
        public async Task<int> GetMessageCountAsync(int conversationId)
        {
            try
            {
                return await _messageRepository.GetMessageCountAsync(conversationId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting message count: {ex.Message}");
                return 0;
            }
        }
    }
}
