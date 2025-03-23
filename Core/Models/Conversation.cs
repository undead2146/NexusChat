using System;
using System.Collections.Generic;
using SQLite;
using SQLiteNetExtensions.Attributes;

namespace NexusChat.Core.Models
{
    /// <summary>
    /// Represents a conversation between a user and an AI
    /// </summary>
    [Table("Conversations")]
    public class Conversation
    {
        /// <summary>
        /// Unique identifier for the conversation
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        
        /// <summary>
        /// User who owns this conversation
        /// </summary>
        [Indexed]
        [ForeignKey(typeof(User))]
        public int UserId { get; set; }
        
        /// <summary>
        /// AI model used for this conversation
        /// </summary>
        [ForeignKey(typeof(AIModel))]
        public int ModelId { get; set; }
        
        /// <summary>
        /// Title/name of the conversation
        /// </summary>
        [MaxLength(100)]
        public string Title { get; set; }
        
        /// <summary>
        /// Optional category for organization
        /// </summary>
        [MaxLength(50)]
        public string Category { get; set; }
        
        /// <summary>
        /// When the conversation was created
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// When the conversation was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; }
        
        /// <summary>
        /// Whether the conversation is marked as favorite
        /// </summary>
        public bool IsFavorite { get; set; }
        
        /// <summary>
        /// Whether the conversation is archived
        /// </summary>
        public bool IsArchived { get; set; }
        
        /// <summary>
        /// Total tokens used in this conversation
        /// </summary>
        public int TotalTokensUsed { get; set; }
        
        /// <summary>
        /// Brief summary of the conversation content
        /// </summary>
        [MaxLength(500)]
        public string Summary { get; set; }
        
        /// <summary>
        /// Navigation property for the user
        /// </summary>
        [ManyToOne]
        public User User { get; set; }
        
        /// <summary>
        /// Navigation property for the AI model
        /// </summary>
        [ManyToOne]
        public AIModel Model { get; set; }
        
        /// <summary>
        /// Navigation property for messages in this conversation
        /// </summary>
        [OneToMany]
        public List<Message> Messages { get; set; }
        
        /// <summary>
        /// Default constructor
        /// </summary>
        public Conversation()
        {
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            IsFavorite = false;
            IsArchived = false;
            TotalTokensUsed = 0;
            Messages = new List<Message>();
        }
        
        /// <summary>
        /// Creates a conversation with basic information
        /// </summary>
        public Conversation(int userId, int modelId, string title = null)
        {
            UserId = userId;
            ModelId = modelId;
            Title = title ?? "New Conversation";
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            IsFavorite = false;
            IsArchived = false;
            TotalTokensUsed = 0;
            Messages = new List<Message>();
        }
        
        /// <summary>
        /// Updates the conversation with a new message's token count
        /// </summary>
        /// <param name="tokensUsed">Number of tokens used in the new message</param>
        public void AddTokens(int tokensUsed)
        {
            TotalTokensUsed += tokensUsed;
            UpdatedAt = DateTime.UtcNow;
        }
        
        /// <summary>
        /// String representation for debugging
        /// </summary>
        public override string ToString()
        {
            return $"Conversation: {Title} (ID: {Id}, Messages: {Messages?.Count ?? 0})";
        }
        
        /// <summary>
        /// Creates a test conversation for development purposes
        /// </summary>
        public static Conversation CreateTestConversation(int userId = 1, int modelId = 1)
        {
            return new Conversation
            {
                UserId = userId,
                ModelId = modelId,
                Title = "Test Conversation",
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                UpdatedAt = DateTime.UtcNow,
                TotalTokensUsed = 0
            };
        }
    }
}
