using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Core.Models;
using NexusChat.Data.Interfaces;
using NexusChat.Helpers;
using System.Diagnostics;

namespace NexusChat.Services
{
    /// <summary>
    /// Service for handling conversation operations
    /// </summary>
    public class ConversationService
    {
        private readonly IConversationRepository _conversationRepository;
        private readonly IMessageRepository _messageRepository;
        
        /// <summary>
        /// Creates a new instance of ConversationService
        /// </summary>
        public ConversationService(
            IConversationRepository conversationRepository,
            IMessageRepository messageRepository)
        {
            _conversationRepository = conversationRepository ?? 
                throw new ArgumentNullException(nameof(conversationRepository));
            _messageRepository = messageRepository ?? 
                throw new ArgumentNullException(nameof(messageRepository));
        }
        
        /// <summary>
        /// Records a new message sent in a conversation
        /// </summary>
        public async Task<bool> RecordMessageSentAsync(
            Conversation conversation,
            Message message,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (conversation == null)
                    return false;
                
                // Update conversation metadata
                conversation.UpdatedAt = DateTime.UtcNow;
                
                // Set message token count if not already set
                if (!message.TokenCount.HasValue)
                {
                    message.TokenCount = TokenHelper.EstimateTokens(message.Content);
                }
                
                // Add message to the conversation if it's not already there
                if (message.ConversationId <= 0)
                {
                    message.ConversationId = conversation.Id;
                    await _messageRepository.AddAsync(message, cancellationToken);
                }
                
                // Update conversation in the repository
                return await _conversationRepository.UpdateAsync(conversation, cancellationToken);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error recording message sent: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Records tokens used in a conversation and updates its state
        /// </summary>
        public async Task<bool> RecordTokensUsedAsync(
            int conversationId, 
            int tokensUsed,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var conversation = await _conversationRepository.GetByIdAsync(conversationId, cancellationToken);
                if (conversation == null)
                    return false;
                
                // Update conversation's timestamp
                conversation.UpdatedAt = DateTime.UtcNow;
                
                // Update conversation in repository
                return await _conversationRepository.UpdateAsync(conversation, cancellationToken);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error recording tokens used: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Updates conversation metadata based on its messages
        /// </summary>
        public async Task<bool> UpdateConversationMetadataAsync(
            int conversationId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var conversation = await _conversationRepository.GetByIdAsync(conversationId, cancellationToken);
                if (conversation == null)
                    return false;
                
                var messages = await _messageRepository.GetByConversationIdAsync(conversationId, cancellationToken: cancellationToken);
                
                if (messages.Count() > 0)
                {
                    // If no title is set, generate from first message
                    if (conversation.Title == "New Chat" && messages.Count() > 0)
                    {
                        var firstMessage = messages[0];
                        string content = firstMessage.Content;
                        
                        // Generate title from content (first 30 chars or first sentence)
                        int endIndex = Math.Min(30, content.Length);
                        int periodIndex = content.IndexOf('.');
                        
                        if (periodIndex > 0 && periodIndex < endIndex)
                            endIndex = periodIndex;
                            
                        conversation.Title = content.Substring(0, endIndex).Trim();
                        if (conversation.Title.Length >= 30)
                            conversation.Title += "...";
                    }
                    
                    // Update last updated time
                    conversation.UpdatedAt = DateTime.UtcNow;
                    
                    // Update model/provider info if available
                    var lastAiMessage = messages.FindLast(m => !m.IsUserMessage);
                    if (lastAiMessage != null)
                    {
                        if (!string.IsNullOrEmpty(lastAiMessage.ModelName))
                            conversation.ModelName = lastAiMessage.ModelName;
                        
                        if (!string.IsNullOrEmpty(lastAiMessage.ProviderName))
                            conversation.ProviderName = lastAiMessage.ProviderName;
                    }
                    
                    return await _conversationRepository.UpdateAsync(conversation, cancellationToken);
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating conversation metadata: {ex.Message}");
                return false;
            }
        }
    }
}
