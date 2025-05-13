using System;
using System.Collections.Generic;
using SQLite;
using SQLiteNetExtensions.Attributes;

namespace NexusChat.Core.Models
{
    /// <summary>
    /// Represents a conversation between a user and AI
    /// </summary>
    [Table("Conversations")]
    public class Conversation
    {
        /// <summary>
        /// Gets or sets the unique identifier
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        
        /// <summary>
        /// Gets or sets the user ID that owns this conversation
        /// </summary>
        [Indexed]
        [ForeignKey(typeof(User))]
        public int UserId { get; set; } = 1; // Default to user ID 1 to prevent null foreign key issues
        
        /// <summary>
        /// Gets or sets the conversation title
        /// </summary>
        [MaxLength(100)]
        public string Title { get; set; } = "New Chat"; // Set default title
        
        /// <summary>
        /// Gets or sets when the conversation was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Gets or sets when the conversation was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Gets or sets the category/tag for this conversation
        /// </summary>
        [MaxLength(50)]
        public string Category { get; set; }
        
        /// <summary>
        /// Gets or sets a brief summary of this conversation
        /// </summary>
        [MaxLength(500)]
        public string Summary { get; set; }
        
        /// <summary>
        /// Gets or sets whether this conversation is a favorite
        /// </summary>
        public bool IsFavorite { get; set; } = false;
        
        /// <summary>
        /// Gets or sets whether this conversation is archived
        /// </summary>
        public bool IsArchived { get; set; } = false;
        
        /// <summary>
        /// Gets or sets the model name used in this conversation
        /// </summary>
        public string ModelName { get; set; }
        
        /// <summary>
        /// Gets or sets the provider name used in this conversation
        /// </summary>
        public string ProviderName { get; set; }
        
        /// <summary>
        /// Navigation property for the user
        /// </summary>
        [ManyToOne]
        public User User { get; set; }
        
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
            Messages = new List<Message>();
        }
        
        /// <summary>
        /// Creates a conversation with basic information
        /// </summary>
        public Conversation(int userId, string title = null)
        {
            UserId = userId;
            Title = title ?? "New Conversation";
            Messages = new List<Message>();
        }
        
        /// <summary>
        /// Updates the conversation with a new message's token count
        /// </summary>
        /// <param name="tokensUsed">Number of tokens used in the new message</param>
        public void AddTokens(int tokensUsed)
        {
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
        public static Conversation CreateTestConversation(int userId = 1)
        {
            return new Conversation
            {
                UserId = userId,
                Title = "Test Conversation",
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                UpdatedAt = DateTime.UtcNow
            };
        }
    }
}
