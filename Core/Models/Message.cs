using SQLite;
using SQLiteNetExtensions.Attributes;
using System;
using System.Text.Json;

namespace NexusChat.Models
{
    /// <summary>
    /// Represents a message within a conversation
    /// </summary>
    public class Message
    {
        /// <summary>
        /// Unique identifier for the message
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        
        /// <summary>
        /// Conversation this message belongs to
        /// </summary>
        [Indexed]
        [ForeignKey(typeof(Conversation))]
        public int ConversationId { get; set; }
        
        /// <summary>
        /// Content of the message
        /// </summary>
        [NotNull]
        public string Content { get; set; }
        
        /// <summary>
        /// Whether this message is from the AI (true) or the user (false)
        /// </summary>
        public bool IsAI { get; set; }
        
        /// <summary>
        /// When the message was created
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Raw response data from AI provider (JSON)
        /// </summary>
        public string RawResponse { get; set; }
        
        /// <summary>
        /// Number of tokens used for this message
        /// </summary>
        public int TokensUsed { get; set; }
        
        /// <summary>
        /// Optional message type for special messages (e.g., "system", "error", "notification")
        /// </summary>
        [MaxLength(20)]
        public string MessageType { get; set; }
        
        /// <summary>
        /// Status of the message (e.g., "sending", "delivered", "error")
        /// </summary>
        [MaxLength(20)]
        public string Status { get; set; } = "delivered";
        
        /// <summary>
        /// Navigation property for the conversation
        /// </summary>
        [ManyToOne]
        public Conversation Conversation { get; set; }
        
        /// <summary>
        /// Default constructor
        /// </summary>
        public Message()
        {
            Timestamp = DateTime.UtcNow;
        }
        
        /// <summary>
        /// Creates a new message
        /// </summary>
        public Message(int conversationId, string content, bool isAI = false)
        {
            ConversationId = conversationId;
            Content = content;
            IsAI = isAI;
            Timestamp = DateTime.UtcNow;
            TokensUsed = 0; // Will be updated for AI messages after processing
            Status = "delivered";
        }
        
        /// <summary>
        /// Validates the message
        /// </summary>
        /// <param name="errorMessage">Error message if validation fails</param>
        /// <returns>True if valid, false otherwise</returns>
        public bool Validate(out string errorMessage)
        {
            // ConversationId validation
            if (ConversationId <= 0)
            {
                errorMessage = "Valid conversation ID is required";
                return false;
            }
            
            // Content validation
            if (string.IsNullOrWhiteSpace(Content))
            {
                errorMessage = "Message content is required";
                return false;
            }
            
            // MessageType validation (if provided)
            if (!string.IsNullOrWhiteSpace(MessageType) && MessageType.Length > 20)
            {
                errorMessage = "Message type cannot exceed 20 characters";
                return false;
            }
            
            // Status validation
            if (string.IsNullOrWhiteSpace(Status))
            {
                errorMessage = "Message status is required";
                return false;
            }
            
            if (Status.Length > 20)
            {
                errorMessage = "Status cannot exceed 20 characters";
                return false;
            }
            
            // All validations passed
            errorMessage = null;
            return true;
        }
        
        /// <summary>
        /// Estimates token count based on content length
        /// Note: This is a simplistic estimation, real token counting depends on the model
        /// </summary>
        /// <returns>Estimated token count</returns>
        public int EstimateTokens()
        {
            // Simple estimation: ~4 characters per token on average for English text
            // This is just a rough estimate for demonstration purposes
            return Content.Length / 4 + 1;
        }
        
        /// <summary>
        /// Updates the token count and returns the difference
        /// </summary>
        /// <param name="newTokenCount">New token count</param>
        /// <returns>Difference between old and new token counts</returns>
        public int UpdateTokens(int newTokenCount)
        {
            int difference = newTokenCount - TokensUsed;
            TokensUsed = newTokenCount;
            return difference;
        }
        
        /// <summary>
        /// Attempts to extract content from raw JSON response
        /// </summary>
        /// <returns>Extracted content or null if not possible</returns>
        public string ExtractContentFromRawResponse()
        {
            if (string.IsNullOrEmpty(RawResponse))
                return null;
                
            try
            {
                // This is a simplified example - in reality, the JSON structure
                // would depend on the specific AI provider's response format
                using var doc = JsonDocument.Parse(RawResponse);
                if (doc.RootElement.TryGetProperty("content", out var contentElement))
                {
                    return contentElement.GetString();
                }
                else if (doc.RootElement.TryGetProperty("message", out var messageElement))
                {
                    return messageElement.GetString();
                }
                else if (doc.RootElement.TryGetProperty("choices", out var choicesElement) &&
                         choicesElement.ValueKind == JsonValueKind.Array &&
                         choicesElement[0].TryGetProperty("message", out var choiceMessageElement) &&
                         choiceMessageElement.TryGetProperty("content", out var choiceContentElement))
                {
                    // Format similar to OpenAI responses
                    return choiceContentElement.GetString();
                }
            }
            catch (JsonException)
            {
                // If parsing fails, just return null
            }
            
            return null;
        }
        
        /// <summary>
        /// Creates a test message for development purposes
        /// </summary>
        public static Message CreateTestMessage(int conversationId = 1, bool isAI = false)
        {
            string content = isAI
                ? "This is a test AI response. I'm here to help you with your questions."
                : "This is a test user message. How can you help me today?";
                
            var message = new Message
            {
                ConversationId = conversationId,
                Content = content,
                IsAI = isAI,
                Timestamp = DateTime.UtcNow.AddMinutes(-5), // 5 minutes ago
                Status = "delivered"
            };
            
            if (isAI)
            {
                message.RawResponse = @"{""id"":""test-123"",""choices"":[{""message"":{""content"":""" + content + @"""}}],""usage"":{""total_tokens"":42}}";
                message.TokensUsed = 42;
            }
            
            return message;
        }
        
        /// <summary>
        /// Creates a system message for development purposes
        /// </summary>
        public static Message CreateSystemMessage(int conversationId, string content)
        {
            return new Message
            {
                ConversationId = conversationId,
                Content = content,
                IsAI = false,
                MessageType = "system",
                Timestamp = DateTime.UtcNow,
                Status = "delivered"
            };
        }
        
        /// <summary>
        /// String representation for debugging
        /// </summary>
        public override string ToString()
        {
            return $"Message: {(IsAI ? "AI" : "User")} - {Content.Substring(0, Math.Min(20, Content.Length))}...";
        }
    }
}
