using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bogus;
using NexusChat.Core.Models;
using NexusChat.Data.Context;

namespace NexusChat.Helpers
{
    /// <summary>
    /// Helper class for generating test data using Bogus
    /// </summary>
    public class TestDataGenerator
    {
        private readonly DatabaseService _databaseService;
        private readonly Random _random = new Random();
        
        /// <summary>
        /// Initializes a new instance of TestDataGenerator
        /// </summary>
        public TestDataGenerator(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        }
        
        /// <summary>
        /// Generates test messages for a conversation
        /// </summary>
        public async Task<List<Message>> GenerateMessagesAsync(int conversationId, int count = 10)
        {
            await _databaseService.Initialize();
            
            // Message types
            var messageTypes = new[] { "Text", "System", "Image", "Audio" };
            
            // Status options
            var statusOptions = new[] { "Sent", "Delivered", "Read" };
            
            // Create fake data generator for user messages
            var userMsgFaker = new Faker<Message>()
                .RuleFor(m => m.ConversationId, _ => conversationId)
                .RuleFor(m => m.Content, f => f.Lorem.Paragraph())
                .RuleFor(m => m.IsAI, _ => false)
                .RuleFor(m => m.Timestamp, f => f.Date.Recent())
                .RuleFor(m => m.MessageType, f => f.PickRandom(messageTypes))
                .RuleFor(m => m.Status, f => f.PickRandom(statusOptions))
                .RuleFor(m => m.TokensUsed, f => f.Random.Number(10, 100));
            
            // Create fake data generator for AI messages
            var aiMsgFaker = new Faker<Message>()
                .RuleFor(m => m.ConversationId, _ => conversationId)
                .RuleFor(m => m.Content, f => f.Lorem.Paragraphs(2, 4))
                .RuleFor(m => m.IsAI, _ => true)
                .RuleFor(m => m.Timestamp, f => f.Date.Recent())
                .RuleFor(m => m.RawResponse, (_, m) => $"{{\"choices\":[{{\"message\":{{\"content\":\"{m.Content.Replace("\"", "\\\"")}\"}}}}]}}")
                .RuleFor(m => m.TokensUsed, f => f.Random.Number(50, 500))
                .RuleFor(m => m.MessageType, _ => "Text")
                .RuleFor(m => m.Status, _ => "Delivered");
            
            // Generate messages with alternating senders
            var messages = new List<Message>();
            for (int i = 0; i < count; i++)
            {
                messages.Add(i % 2 == 0 ? userMsgFaker.Generate() : aiMsgFaker.Generate());
            }
            
            // Save to database
            await _databaseService.Database.InsertAllAsync(messages);
            
            return messages;
        }
        
        /// <summary>
        /// Generates test conversations
        /// </summary>
        public async Task<List<Conversation>> GenerateConversationsAsync(int userId, int count = 5)
        {
            await _databaseService.Initialize();
            
            // Categories
            var categories = new[] { "General", "Work", "Personal", "Creative", "Technical" };
            
            // Create fake data generator
            var faker = new Faker<Conversation>()
                .RuleFor(c => c.UserId, _ => userId)
                .RuleFor(c => c.Title, f => f.Lorem.Sentence(3, 3).TrimEnd('.'))
                .RuleFor(c => c.ModelId, f => f.Random.Number(1, 3))
                .RuleFor(c => c.CreatedAt, f => f.Date.Past(1))
                .RuleFor(c => c.UpdatedAt, (f, c) => f.Date.Between(c.CreatedAt, DateTime.Now))
                .RuleFor(c => c.IsFavorite, f => f.Random.Bool(0.3f))
                .RuleFor(c => c.Category, f => f.PickRandom(categories))
                .RuleFor(c => c.Summary, f => f.Lorem.Paragraph())
                .RuleFor(c => c.TotalTokensUsed, f => f.Random.Number(100, 5000));
            
            // Generate conversations
            var conversations = faker.Generate(count);
            
            // Save to database
            await _databaseService.Database.InsertAllAsync(conversations);
            
            return conversations;
        }
        
        /// <summary>
        /// Generates test data for a complete conversation with messages
        /// </summary>
        public async Task<Conversation> GenerateCompleteConversationAsync(int userId, int messageCount = 10)
        {
            // Create conversation
            var conversations = await GenerateConversationsAsync(userId, 1);
            var conversation = conversations[0];
            
            // Add messages
            await GenerateMessagesAsync(conversation.Id, messageCount);
            
            return conversation;
        }
    }
}
