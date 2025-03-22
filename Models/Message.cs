using SQLite;
using SQLiteNetExtensions.Attributes;
using System;

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
        /// Creates a new user message
        /// </summary>
        public Message(int conversationId, string content, bool isAI = false)
        {
            ConversationId = conversationId;
            Content = content;
            IsAI = isAI;
            Timestamp = DateTime.UtcNow;
            TokensUsed = 0; // Will be updated for AI messages after processing
        }
    }
}
