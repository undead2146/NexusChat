using SQLite;
using SQLiteNetExtensions.Attributes;
using System;
using NexusChat.Helpers;

namespace NexusChat.Core.Models
{
    /// <summary>
    /// Represents a message in a conversation
    /// </summary>
    [Table("Messages")]
    public class Message
    {
        /// <summary>
        /// Gets or sets the unique identifier
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        
        /// <summary>
        /// Gets or sets the conversation ID this message belongs to
        /// </summary>
        [ForeignKey(typeof(Conversation))]
        [Indexed]
        public int ConversationId { get; set; }
        
        /// <summary>
        /// Gets or sets the message content
        /// </summary>
        public string Content { get; set; } = "";
        
        /// <summary>
        /// Gets or sets the sender ID (user or system)
        /// </summary>
        public string SenderId { get; set; }
        
        /// <summary>
        /// Gets or sets whether this is a user message (true) or AI message (false)
        /// </summary>
        public bool IsUserMessage { get; set; }
        
        /// <summary>
        /// Gets or sets the timestamp of the message
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
        
        /// <summary>
        /// Gets or sets the token count for this message
        /// </summary>
        public int? TokenCount { get; set; }
        
        /// <summary>
        /// Gets or sets the status of the message (Sending, Sent, Failed, etc.)
        /// </summary>
        public string Status { get; set; } = "sent";
        
        /// <summary>
        /// Gets or sets special formatting or type information about the message
        /// </summary>
        public string MessageType { get; set; } = "text";
        
        /// <summary>
        /// Gets or sets the raw response data
        /// </summary>
        public string RawResponse { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the model that generated this message
        /// </summary>
        public string ModelName { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the provider that generated this message
        /// </summary>
        public string ProviderName { get; set; }
        
        /// <summary>
        /// Gets or sets any error information for this message
        /// </summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// Gets or sets whether this message is an error
        /// </summary>
        public bool IsError { get; set; }

        /// <summary>
        /// Gets or sets the author type (user or AI)
        /// </summary>
        public string AuthorType { get; set; } = "user";

        /// <summary>
        /// Gets or sets the sent timestamp of the message
        /// </summary>
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the created timestamp of the message
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the updated timestamp of the message
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets whether this message is from AI (compatibility property)
        /// </summary>
        public bool IsAI 
        { 
            get => AuthorType == "ai";
            set => AuthorType = value ? "ai" : "user";
        }

        /// <summary>
        /// Gets or sets the number of tokens used in this message
        /// </summary>
        [Ignore]
        public int TokensUsed => TokenCount ?? TokenHelper.EstimateTokens(Content);
        
        /// <summary>
        /// Basic constructor
        /// </summary>
        public Message() { }
        
        /// <summary>
        /// Constructor with basic properties
        /// </summary>
        public Message(string content, bool isUserMessage)
        {
            Content = content;
            IsUserMessage = isUserMessage;
            Timestamp = DateTime.Now;
        }
        
        /// <summary>
        /// Constructor with more properties
        /// </summary>
        public Message(string content, bool isUserMessage, string modelName)
        {
            Content = content;
            IsUserMessage = isUserMessage;
            ModelName = modelName;
            Timestamp = DateTime.Now;
        }
    }
}
