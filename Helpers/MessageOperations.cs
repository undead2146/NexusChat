using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NexusChat.Core.Models;

namespace NexusChat.Helpers
{
    /// <summary>
    /// Helper class for message operations
    /// </summary>
    public class MessageOperations
    {
        /// <summary>
        /// Creates a user message
        /// </summary>
        public static Message CreateUserMessage(string content, int conversationId)
        {
            // Generate a unique message ID based on timestamp
            int messageId = (int)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 100000);
            
            return new Message
            {
                Id = messageId,
                ConversationId = conversationId,
                Content = content?.Trim() ?? string.Empty,
                IsAI = false,
                Timestamp = DateTime.Now,
                Status = "Sent"
            };
        }
        
        /// <summary>
        /// Creates an AI message (response)
        /// </summary>
        public static Message CreateAIMessage(string content, int conversationId, int baseId)
        {
            return new Message
            {
                Id = baseId + 2, // Ensure unique ID sequence from user message
                ConversationId = conversationId,
                Content = content,
                IsAI = true,
                Timestamp = DateTime.Now,
                TokensUsed = Message.EstimateTokens(content),
                Status = "Delivered"
            };
        }
        
        /// <summary>
        /// Creates a typing indicator message
        /// </summary>
        public static Message CreateTypingMessage(int conversationId, int baseId)
        {
            return new Message
            {
                Id = baseId + 1, // Sequential ID based on user message
                ConversationId = conversationId,
                Content = "Typing...",
                IsAI = true,
                Timestamp = DateTime.Now,
                Status = "Typing"
            };
        }
        
        /// <summary>
        /// Creates an error message
        /// </summary>
        public static Message CreateErrorMessage(int conversationId, int baseId, string errorContent = null)
        {
            return new Message
            {
                Id = baseId + 2, // Ensure unique ID sequence
                ConversationId = conversationId,
                Content = errorContent ?? "Sorry, I couldn't generate a response. Please try again.",
                IsAI = true,
                Timestamp = DateTime.Now,
                Status = "Error"
            };
        }
        
        /// <summary>
        /// Cleans up typing indicator messages from a collection
        /// </summary>
        public static void CleanupTypingMessages(ObservableCollection<Message> messages)
        {
            try
            {
                var typingMessages = messages.Where(m => m?.Status == "Typing").ToList();
                foreach (var msg in typingMessages)
                {
                    Debug.WriteLine($"Removing typing message ID: {msg.Id}");
                    messages.Remove(msg);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error cleaning up typing messages: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Asynchronously cleans up typing indicator messages
        /// </summary>
        public static async Task CleanupTypingMessagesAsync(ObservableCollection<Message> messages)
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(() => 
                {
                    CleanupTypingMessages(messages);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CleanupTypingMessagesAsync: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Cleans up empty messages from a collection
        /// </summary>
        public static void CleanupEmptyMessages(ObservableCollection<Message> messages)
        {
            var emptyMessages = messages.Where(m => string.IsNullOrWhiteSpace(m?.Content)).ToList();
            foreach (var message in emptyMessages)
            {
                messages.Remove(message);
            }
        }
    }
}
