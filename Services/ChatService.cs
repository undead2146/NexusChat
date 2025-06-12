using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Core.Models;
using NexusChat.Data.Interfaces;
using NexusChat.Services.Interfaces;

namespace NexusChat.Services
{
    /// <summary>
    /// Service for handling chat functionality
    /// </summary>
    public class ChatService : IChatService
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IConversationRepository _conversationRepository;
        private readonly IAIProviderFactory _providerFactory; // Correct type
        private readonly IAIModelManager _modelManager;

        /// <summary>
        /// Gets the current AI model being used
        /// </summary>
        public AIModel CurrentModel => _modelManager.CurrentModel;

        /// <summary>
        /// Occurs when streaming data is received
        /// </summary>
        public event EventHandler<string>? StreamingDataReceived;

        /// <summary>
        /// Occurs when the model is changed
        /// </summary>
        public event EventHandler<AIModel>? ModelChanged;

        /// <summary>
        /// Creates a new ChatService instance
        /// </summary>
        public ChatService(
            IMessageRepository messageRepository,
            IConversationRepository conversationRepository,
            IAIProviderFactory providerFactory, // Using IAIProviderFactory
            IAIModelManager modelManager)
        {
            _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
            _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
            _providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
            _modelManager = modelManager ?? throw new ArgumentNullException(nameof(modelManager));
        }

        /// <summary>
        /// Gets messages for a conversation
        /// </summary>
        public async Task<List<Message>> GetMessagesForConversationAsync(int conversationId)
        {
            try
            {
                return await _messageRepository.GetByConversationIdAsync(conversationId, 50, 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting messages for conversation: {ex.Message}");
                return new List<Message>();
            }
        }

        /// <summary>
        /// Gets a response from the AI for the given prompt
        /// </summary>
        public async Task<string> GetResponseAsync(string prompt, CancellationToken cancellationToken)
        {
            try
            {
                var service = await GetCurrentAIServiceAsync();
                if (service == null)
                {
                    throw new InvalidOperationException("No AI service is available. Please select a model.");
                }

                return await service.SendMessageAsync(prompt, cancellationToken);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting AI response: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets a streaming response from the AI
        /// </summary>
        public async Task<Stream> GetStreamingResponseAsync(string prompt, CancellationToken cancellationToken)
        {
            try
            {
                var service = await GetCurrentAIServiceAsync();
                if (service == null)
                {
                    throw new InvalidOperationException("No AI service is available. Please select a model.");
                }

                return await service.SendStreamedMessageAsync(prompt, cancellationToken, OnStreamingUpdate);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting AI streaming response: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Callback for streaming updates
        /// </summary>
        private void OnStreamingUpdate(string content)
        {
            StreamingDataReceived?.Invoke(this, content);
        }

        /// <summary>
        /// Changes the current AI model
        /// </summary>
        public async Task<bool> ChangeModelAsync(AIModel model)
        {
            try
            {
                bool success = await _modelManager.SetCurrentModelAsync(model);
                if (success)
                {
                    ModelChanged?.Invoke(this, model);
                }
                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error changing model: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the estimated token count for a message
        /// </summary>
        public async Task<int> EstimateTokens(string message)
        {
            try
            {
                if (string.IsNullOrEmpty(message))
                    return 0;

                var service = await GetCurrentAIServiceAsync();
                if (service != null)
                {
                    return service.EstimateTokens(message);
                }

                // Default estimation - ~4 chars per token
                return message.Length / 4 + 1;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error estimating tokens: {ex.Message}");
                return message.Length / 4 + 1; // Fallback estimation
            }
        }

        /// <summary>
        /// Deletes all messages for a conversation
        /// </summary>
        public async Task<bool> DeleteConversationMessagesAsync(int conversationId)
        {
            try
            {
                return await _messageRepository.DeleteByConversationIdAsync(conversationId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting conversation messages: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Creates a new conversation
        /// </summary>
        public async Task<Conversation> CreateConversationAsync(string title, string? modelName = null, string? providerName = null)
        {
            try
            {
                var now = DateTime.UtcNow;
                var conversation = new Conversation
                {
                    Title = title ?? "New Chat",
                    CreatedAt = now,
                    UpdatedAt = now,
                    ModelName = modelName ?? _modelManager.CurrentModel?.ModelName,
                    ProviderName = providerName ?? _modelManager.CurrentModel?.ProviderName
                };

                conversation.Id = await _conversationRepository.AddConversationAsync(conversation);
                return conversation;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating conversation: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Sends a message in a conversation
        /// </summary>
        public async Task<Message> SendMessageAsync(int conversationId, string content, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(content))
                    throw new ArgumentException("Message content cannot be empty", nameof(content));

                // Create user message and save
                var message = new Message
                {
                    ConversationId = conversationId,
                    AuthorType = "user",
                    Content = content,
                    Status = "sent",
                    SentAt = DateTime.UtcNow
                };

                int messageId = await _messageRepository.AddAsync(message);
                message.Id = messageId;

                // Update conversation last activity
                await _conversationRepository.UpdateLastActivityAsync(conversationId);

                return message;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error sending message: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Sends an AI message in a conversation
        /// </summary>
        public async Task<Message> SendAIMessageAsync(int conversationId, string userPrompt, CancellationToken cancellationToken = default)
        {
            try
            {
                // Get existing conversation
                var conversation = await _conversationRepository.GetByIdAsync(conversationId);
                if (conversation == null)
                {
                    throw new InvalidOperationException($"Conversation with ID {conversationId} not found");
                }

                // Create initial AI message
                var aiMessage = new Message
                {
                    Content = "",
                    ConversationId = conversationId,
                    IsAI = true,
                    MessageType = "ai",
                    Status = "thinking",
                    Timestamp = DateTime.Now
                };

                // Add to database
                aiMessage.Id = await _messageRepository.AddAsync(aiMessage);

                // Get AI service for current model
                var aiService = await GetCurrentAIServiceAsync();
                if (aiService == null)
                {
                    throw new InvalidOperationException("No AI service is configured");
                }

                // Create callback action for streaming updates
                Action<string> updateCallback = (string update) =>
                    UpdateAIMessageContentAsync(aiMessage, update).ConfigureAwait(false);

                // Call with correct parameter order
                var result = await aiService.SendStreamedMessageAsync(
                    userPrompt,
                    cancellationToken,
                    updateCallback
                );

                // Update message status
                aiMessage.Status = "complete";
                await _messageRepository.UpdateAsync(aiMessage, cancellationToken);

                return aiMessage;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SendAIMessageAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Updates AI message content during streaming
        /// </summary>
        private async Task UpdateAIMessageContentAsync(Message message, string content)
        {
            if (message == null || string.IsNullOrEmpty(content))
                return;

            try
            {
                // Update the message content
                message.Content = content;

                // Update in database
                await _messageRepository.UpdateAsync(message, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating AI message: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates AI message status
        /// </summary>
        private async Task UpdateAIMessageStatusAsync(Message message, CancellationToken cancellationToken)
        {
            try
            {
                await _messageRepository.UpdateAsync(message, cancellationToken);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating AI message status: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the current AI service based on the selected model
        /// </summary>
        private async Task<IAIProviderService?> GetCurrentAIServiceAsync()
        {
            await Task.Delay(1); // Make method truly async
            
            var currentModel = _modelManager.CurrentModel;
            if (currentModel == null)
            {
                Debug.WriteLine("No current model selected, using default");
                // return _providerFactory.GetDefaultService();
                return null; // No model selected, return null
            }

            return await _providerFactory.GetProviderForModelAsync(currentModel.ProviderName, currentModel.ModelName);
        }

        /// <summary>
        /// Gets the last message for a conversation
        /// </summary>
        public async Task<Message?> GetLastMessageAsync(int conversationId, CancellationToken cancellationToken = default)
        {
            try
            {
                var lastMessage = await _messageRepository.GetLastMessageForConversationAsync(conversationId, cancellationToken);
                return lastMessage;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting last message: {ex.Message}");
                return null;
            }
        }

        public async Task<List<Message>> GetMessagesAsync(int conversationId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _messageRepository.GetByConversationIdAsync(conversationId, 50, 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting messages: {ex.Message}");
                return new List<Message>();
            }
        }

        public async Task RegenerateMessageAsync(int conversationId, int messageId, CancellationToken cancellationToken = default)
        {
            try
            {
                var message = await _messageRepository.GetByIdAsync(messageId);
                if (message != null && message.IsAI)
                {
                    // Find the previous user message to regenerate from
                    var messages = await _messageRepository.GetByConversationIdAsync(conversationId, 100, 0);
                    var previousUserMessage = messages
                        .Where(m => !m.IsAI && m.SentAt < message.SentAt)
                        .OrderByDescending(m => m.SentAt)
                        .FirstOrDefault();

                    if (previousUserMessage != null)
                    {
                        await SendAIMessageAsync(conversationId, previousUserMessage.Content);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error regenerating message: {ex.Message}");
            }
        }
    }
}
