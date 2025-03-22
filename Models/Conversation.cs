using SQLite;
using SQLiteNetExtensions.Attributes;
using System;
using System.Collections.Generic;

namespace NexusChat.Models
{
    /// <summary>
    /// Represents a conversation thread between a user and an AI model
    /// </summary>
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
        /// Title of the conversation
        /// </summary>
        [MaxLength(100)]
        public string Title { get; set; }
        
        /// <summary>
        /// AI model used for this conversation
        /// </summary>
        [ForeignKey(typeof(AIModel))]
        public int? ModelId { get; set; }
        
        /// <summary>
        /// Date and time when the conversation was created
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// Date and time when the conversation was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; }
        
        /// <summary>
        /// Whether the conversation is marked as favorite
        /// </summary>
        public bool IsFavorite { get; set; }
        
        /// <summary>
        /// Category for organization/filtering
        /// </summary>
        [MaxLength(50)]
        public string Category { get; set; }
        
        /// <summary>
        /// Summary of the conversation (auto-generated or user-edited)
        /// </summary>
        [MaxLength(200)]
        public string Summary { get; set; }
        
        /// <summary>
        /// Total token count used in this conversation
        /// </summary>
        public int TotalTokensUsed { get; set; }
        
        /// <summary>
        /// Navigation property for the user who owns this conversation
        /// </summary>
        [ManyToOne]
        public User User { get; set; }
        
        /// <summary>
        /// Navigation property for the AI model used in this conversation
        /// </summary>
        [OneToOne]
        public AIModel Model { get; set; }
        
        /// <summary>
        /// Navigation property for the messages in this conversation
        /// </summary>
        [OneToMany(CascadeOperations = CascadeOperation.All)]
        public List<Message> Messages { get; set; }
        
        /// <summary>
        /// Default constructor required by SQLite
        /// </summary>
        public Conversation()
        {
            Messages = new List<Message>();
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
        
        /// <summary>
        /// Creates a conversation with basic required properties
        /// </summary>
        public Conversation(int userId, string title = null, int? modelId = null)
        {
            UserId = userId;
            Title = title ?? "New Conversation";
            ModelId = modelId;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            IsFavorite = false;
            Messages = new List<Message>();
            TotalTokensUsed = 0;
        }
        
        /// <summary>
        /// Validates the conversation model
        /// </summary>
        /// <param name="errorMessage">Error message if validation fails</param>
        /// <returns>True if valid, false otherwise</returns>
        public bool Validate(out string errorMessage)
        {
            // UserId validation
            if (UserId <= 0)
            {
                errorMessage = "Valid user ID is required";
                return false;
            }
            
            // Title validation
            if (string.IsNullOrWhiteSpace(Title))
            {
                errorMessage = "Conversation title is required";
                return false;
            }
            
            if (Title.Length > 100)
            {
                errorMessage = "Conversation title cannot exceed 100 characters";
                return false;
            }
            
            // Category validation
            if (!string.IsNullOrWhiteSpace(Category) && Category.Length > 50)
            {
                errorMessage = "Category cannot exceed 50 characters";
                return false;
            }
            
            // Summary validation
            if (!string.IsNullOrWhiteSpace(Summary) && Summary.Length > 200)
            {
                errorMessage = "Summary cannot exceed 200 characters";
                return false;
            }
            
            // All validations passed
            errorMessage = null;
            return true;
        }
        
        /// <summary>
        /// Updates the last modified timestamp
        /// </summary>
        public void Touch()
        {
            UpdatedAt = DateTime.UtcNow;
        }
        
        /// <summary>
        /// Adds a token count to the total
        /// </summary>
        public void AddTokens(int tokens)
        {
            if (tokens > 0)
            {
                TotalTokensUsed += tokens;
                Touch();
            }
        }
        
        /// <summary>
        /// Toggles the favorite status
        /// </summary>
        /// <returns>The new favorite status</returns>
        public bool ToggleFavorite()
        {
            IsFavorite = !IsFavorite;
            Touch();
            return IsFavorite;
        }
        
        /// <summary>
        /// Creates a test conversation for development purposes
        /// </summary>
        public static Conversation CreateTestConversation(int userId = 1, int? modelId = 1)
        {
            return new Conversation
            {
                UserId = userId,
                Title = "Test Conversation",
                ModelId = modelId,
                CreatedAt = DateTime.UtcNow.AddDays(-3), // Created 3 days ago
                UpdatedAt = DateTime.UtcNow.AddHours(-5), // Updated 5 hours ago
                IsFavorite = true,
                Category = "Testing",
                Summary = "This is a test conversation created for development purposes.",
                TotalTokensUsed = 1250
            };
        }
        
        /// <summary>
        /// String representation for debugging
        /// </summary>
        public override string ToString()
        {
            return $"Conversation: {Title} (ID: {Id}, UserId: {UserId})";
        }
    }
}
