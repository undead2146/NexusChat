using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bogus;
using NexusChat.Core.Models;
using NexusChat.Data.Context;
using System.Linq;
using System.Collections.ObjectModel;
using System.Threading;

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
        /// Generates test users with realistic data
        /// </summary>
        /// <param name="count">Number of users to generate</param>
        /// <param name="generatedEntities">Collection to add generated entities to for display</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of generated users</returns>
        public async Task<List<User>> GenerateUsersAsync(int count, ObservableCollection<object> generatedEntities = null, CancellationToken cancellationToken = default)
        {
            await _databaseService.Initialize();
            
            // Ensure count is within reasonable limits
            int usersToGenerate = Math.Clamp(count, 1, 100);
            
            // Create fake data generator
            var faker = new Faker<User>()
                .RuleFor(u => u.Username, f => CleanUsername(f.Internet.UserName()))
                .RuleFor(u => u.DisplayName, f => f.Name.FullName())
                .RuleFor(u => u.Email, f => f.Internet.Email())
                .RuleFor(u => u.DateCreated, f => f.Date.Past(1))
                .RuleFor(u => u.PreferredTheme, f => f.Random.ArrayElement(new[] { "Light", "Dark", "System" }))
                .RuleFor(u => u.AvatarPath, f => f.Internet.Avatar());
            
            // Generate users with delay to avoid UI freeze for large generations
            var users = new List<User>();
            for (int i = 0; i < usersToGenerate; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                    
                var user = faker.Generate();
                user.SetPassword($"password{i}");  // Use a standard test password
                users.Add(user);
                
                // Add to display collection if provided
                if (generatedEntities != null && i < 20) // Limit to display 20 items max
                {
                    await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(() => 
                    {
                        generatedEntities.Add(user);
                    });
                }
                
                // Small delay to keep UI responsive for large generations
                if (i % 5 == 0)
                    await Task.Delay(1);
            }
            
            // Save to database - remove cancellationToken from InsertAllAsync
            await _databaseService.Database.InsertAllAsync(users);
            
            return users;
        }
        
        /// <summary>
        /// Ensures default AI models exist in the database
        /// </summary>
        public async Task<List<AIModel>> EnsureDefaultModelsExistAsync(CancellationToken cancellationToken = default)
        {
            await _databaseService.Initialize();
            
            // Remove cancellationToken from ToListAsync
            var existingModels = await _databaseService.Database.Table<AIModel>().ToListAsync();
            if (existingModels.Count > 0)
                return existingModels;
                
            // Create model definitions
            var models = new List<AIModel>
            {
                new AIModel
                {
                    ProviderName = "OpenAI",
                    ModelName = "GPT-4",
                    Description = "Advanced language model for general purpose AI tasks",
                    MaxTokens = 8192,
                    DefaultTemperature = 0.7f,
                    IsAvailable = true
                },
                new AIModel
                {
                    ProviderName = "Anthropic",
                    ModelName = "Claude-3",
                    Description = "Advanced reasoning and conversation AI model",
                    MaxTokens = 100000,
                    DefaultTemperature = 0.5f,
                    IsAvailable = true
                },
                new AIModel
                {
                    ProviderName = "Meta",
                    ModelName = "Llama-3",
                    Description = "Open ecosystem large language model",
                    MaxTokens = 4096,
                    DefaultTemperature = 0.6f,
                    IsAvailable = true
                },
                new AIModel
                {
                    ProviderName = "NexusChat",
                    ModelName = "DummyModel",
                    Description = "Simulated AI model for testing",
                    MaxTokens = 2048,
                    DefaultTemperature = 0.7f,
                    IsAvailable = true
                }
            };
            
            // Save to database - remove cancellationToken
            await _databaseService.Database.InsertAllAsync(models);
            
            return models;
        }
        
        /// <summary>
        /// Generates test conversations for users in the database
        /// </summary>
        /// <param name="count">Number of conversations to generate</param>
        /// <param name="generatedEntities">Collection to add generated entities to for display</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task<List<Conversation>> GenerateConversationsAsync(
            int count, 
            ObservableCollection<object> generatedEntities = null,
            CancellationToken cancellationToken = default)
        {
            await _databaseService.Initialize();
            
            // Remove cancellationToken from ToListAsync
            var users = await _databaseService.Database.Table<User>().ToListAsync();
            if (users.Count == 0)
                throw new InvalidOperationException("No users found. Please generate users first.");
                
            // Get user IDs
            var userIds = users.Select(u => u.Id).ToList();
            
            // Ensure we have AI models
            var models = await EnsureDefaultModelsExistAsync();
            var modelIds = models.Select(m => m.Id).ToList();
            
            // Categories and tags for more realistic data
            var categories = new[] { "General", "Work", "Personal", "Creative", "Technical", "Research", "Education" };
            var topics = new[] { "AI", "Programming", "Philosophy", "Science", "Mathematics", "History", "Art", "Finance", "Health" };
            
            // Create fake data generator with more realistic titles
            var faker = new Faker<Conversation>()
                .RuleFor(c => c.UserId, f => f.PickRandom(userIds))
                .RuleFor(c => c.Title, f => CreateConversationTitle(f, topics))
                .RuleFor(c => c.ModelId, f => f.PickRandom(modelIds))
                .RuleFor(c => c.CreatedAt, f => f.Date.Past(1))
                // Fix the UpdatedAt rule
                .RuleFor(c => c.UpdatedAt, (f, c) => f.Date.Between(c.CreatedAt, DateTime.Now))
                .RuleFor(c => c.IsFavorite, f => f.Random.Bool(0.3f))
                .RuleFor(c => c.IsArchived, f => f.Random.Bool(0.15f))
                .RuleFor(c => c.Category, f => f.PickRandom(categories))
                .RuleFor(c => c.Summary, f => f.Lorem.Paragraph())
                .RuleFor(c => c.TotalTokensUsed, f => f.Random.Number(100, 5000));
            
            // Generate conversations with delay to avoid UI freeze
            var conversations = new List<Conversation>();
            int conversationsToGenerate = Math.Clamp(count, 1, 50); // Limit to reasonable number
            
            for (int i = 0; i < conversationsToGenerate; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                    
                var conversation = faker.Generate();
                conversations.Add(conversation);
                
                // Add to display collection if provided
                if (generatedEntities != null && i < 20) // Limit to display 20 items max
                {
                    await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(() => 
                    {
                        generatedEntities.Add(conversation);
                    });
                }
                
                // Small delay to keep UI responsive
                if (i % 5 == 0)
                    await Task.Delay(1);
            }
            
            // Save to database - remove cancellationToken
            await _databaseService.Database.InsertAllAsync(conversations);
            
            return conversations;
        }
        
        /// <summary>
        /// Generates test messages for conversations in the database
        /// </summary>
        /// <param name="count">Number of messages to generate per conversation</param>
        /// <param name="generatedEntities">Collection to add generated entities to for display</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task<List<Message>> GenerateMessagesAsync(
            int count, 
            ObservableCollection<object> generatedEntities = null,
            CancellationToken cancellationToken = default)
        {
            await _databaseService.Initialize();
            
            // Reset counter for each new generation batch
            allGeneratedCount = 0;
            
            var conversations = await _databaseService.Database.Table<Conversation>().ToListAsync();
            if (conversations.Count == 0)
                throw new InvalidOperationException("No conversations found. Please generate conversations first.");
                
            Dictionary<int, int> messageCountByConversation = new Dictionary<int, int>();
            List<Message> allGeneratedMessages = new List<Message>();
            
            // Instead of dividing by conversations, ensure we generate the exact requested count
            int totalToGenerate = Math.Min(count, 100); // Limit to reasonable number
            
            // Calculate messages per conversation - distribute evenly but ensure total matches count
            int messagesPerConversation = totalToGenerate / conversations.Count;
            int remainder = totalToGenerate % conversations.Count;
            
            // Generate messages for each conversation
            for (int i = 0; i < conversations.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                    
                // Add one extra message to some conversations to account for remainder
                int messagesToGenerate = messagesPerConversation + (i < remainder ? 1 : 0);
                
                // Skip if no messages to generate for this conversation
                if (messagesToGenerate <= 0)
                    continue;
                    
                var conversation = conversations[i];
                var messages = await GenerateMessagesForConversationAsync(
                    conversation.Id, 
                    messagesToGenerate, 
                    generatedEntities,
                    cancellationToken);
                    
                allGeneratedMessages.AddRange(messages);
                
                // Update conversation tokens
                int totalTokens = messages.Where(m => m.IsAI).Sum(m => m.TokensUsed);
                conversation.TotalTokensUsed += totalTokens;
                await _databaseService.Database.UpdateAsync(conversation);
                
                // Update message counts
                messageCountByConversation[conversation.Id] = messages.Count;
                
                // Small delay to keep UI responsive
                await Task.Delay(1);
            }
            
            return allGeneratedMessages;
        }
        
        /// <summary>
        /// Generates test messages for a specific conversation with realistic chat patterns
        /// </summary>
        public async Task<List<Message>> GenerateMessagesForConversationAsync(
            int conversationId, 
            int count = 10,
            ObservableCollection<object> generatedEntities = null,
            CancellationToken cancellationToken = default)
        {
            await _databaseService.Initialize();
            
            // Message types
            var messageTypes = new[] { "Text", "System", "Image", "Code" };
            
            // Status options
            var statusOptions = new[] { "Sent", "Delivered", "Read" };
            
            // Get the conversation to understand its context
            // Fix FirstOrDefaultAsync query
            var conversation = await _databaseService.Database.Table<Conversation>()
                .Where(c => c.Id == conversationId)
                .FirstOrDefaultAsync();
                
            if (conversation == null)
                throw new InvalidOperationException($"Conversation with ID {conversationId} not found");
                
            // Create template prompts based on conversation category or title
            var prompts = CreateContextualPrompts(conversation.Category, conversation.Title);
            
            // Create fake data generator for user messages
            var userMsgFaker = new Faker<Message>()
                .RuleFor(m => m.ConversationId, _ => conversationId)
                .RuleFor(m => m.Content, f => f.PickRandom(prompts.UserPrompts))
                .RuleFor(m => m.IsAI, _ => false)
                .RuleFor(m => m.Timestamp, f => f.Date.Recent())
                .RuleFor(m => m.MessageType, f => f.PickRandom(messageTypes))
                .RuleFor(m => m.Status, f => f.PickRandom(statusOptions))
                .RuleFor(m => m.TokensUsed, f => f.Random.Number(10, 100));
            
            // Create fake data generator for AI messages
            var aiMsgFaker = new Faker<Message>()
                .RuleFor(m => m.ConversationId, _ => conversationId)
                .RuleFor(m => m.Content, f => f.PickRandom(prompts.AIResponses))
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
                if (cancellationToken.IsCancellationRequested)
                    break;
                    
                var message = i % 2 == 0 ? userMsgFaker.Generate() : aiMsgFaker.Generate();
                
                // Ensure timestamps are in order
                if (i > 0)
                {
                    message.Timestamp = messages[i - 1].Timestamp.AddMinutes(_random.Next(1, 10));
                }
                
                messages.Add(message);
                
                // Only add up to 20 items total to the display collection
                if (generatedEntities != null && allGeneratedCount < 20)
                {
                    await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(() => 
                    {
                        generatedEntities.Add(message);
                    });
                    allGeneratedCount++;
                }
            }
            
            // Save to database - remove cancellationToken
            await _databaseService.Database.InsertAllAsync(messages);
            
            return messages;
        }
        
        // Helper to clean usernames
        private string CleanUsername(string username)
        {
            if (string.IsNullOrEmpty(username))
                return "user" + _random.Next(1000, 9999);

            // Replace spaces with underscores and remove special characters
            var cleaned = new string(username.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
            
            // Ensure it's not empty after cleaning and reasonable length
            if (string.IsNullOrEmpty(cleaned) || cleaned.Length < 4)
                return "user" + _random.Next(1000, 9999);
            
            // Truncate if too long
            if (cleaned.Length > 20)
                cleaned = cleaned.Substring(0, 20);
            
            return cleaned;
        }
        
        // Counter for limiting the number of generated items displayed
        private int allGeneratedCount = 0;
        
        // Helper to create more realistic conversation titles
        private string CreateConversationTitle(Faker f, string[] topics)
        {
            var topic = f.PickRandom(topics);
            var templates = new[]
            {
                $"Discussion about {topic}",
                $"Help me understand {topic}",
                $"Questions on {topic}",
                $"Exploring {topic} concepts",
                $"{topic} research assistance",
                $"Learning about {topic}",
                $"{topic} project ideas",
                $"Explain {topic} simply"
            };
            
            return f.PickRandom(templates);
        }
        
        // Helper to create contextual prompts and responses
        private (List<string> UserPrompts, List<string> AIResponses) CreateContextualPrompts(string category, string title)
        {
            var userPrompts = new List<string>();
            var aiResponses = new List<string>();
            
            // Base prompts for all categories
            userPrompts.Add("Can you explain this concept?");
            userPrompts.Add("I need help understanding this.");
            userPrompts.Add("What are your thoughts on this?");
            userPrompts.Add("Could you provide some examples?");
            
            // Base responses for all categories
            aiResponses.Add("I'd be happy to explain this concept. It involves several key principles...");
            aiResponses.Add("That's an interesting question. Let me break it down for you...");
            aiResponses.Add("Based on current understanding, there are multiple perspectives on this topic...");
            aiResponses.Add("Here are some examples that might help illustrate the concept better...");
            
            // Add category-specific prompts
            switch (category?.ToLower())
            {
                case "technical":
                    userPrompts.Add("How do I implement this algorithm?");
                    userPrompts.Add("What's the best practice for this development scenario?");
                    userPrompts.Add("Can you help debug this code?");
                    aiResponses.Add("When implementing this algorithm, you should consider the following steps: First, initialize your data structures...");
                    aiResponses.Add("The best practice in this scenario would be to follow the principle of separation of concerns...");
                    aiResponses.Add("Looking at your code, I notice a few potential issues. First, check the loop condition...");
                    break;
                    
                case "creative":
                    userPrompts.Add("Can you suggest some creative ideas for my project?");
                    userPrompts.Add("How can I make my writing more engaging?");
                    userPrompts.Add("I need help with brainstorming.");
                    aiResponses.Add("Here are some creative approaches you might consider for your project: 1. Combine unexpected elements...");
                    aiResponses.Add("To make your writing more engaging, try employing sensory details, varying sentence structure, and creating tension through...");
                    aiResponses.Add("Let's brainstorm some possibilities. Starting with your core concept, we could expand in these directions...");
                    break;
                    
                case "research":
                    userPrompts.Add("What are the latest developments in this field?");
                    userPrompts.Add("Can you summarize this research paper?");
                    userPrompts.Add("What methodology would be appropriate for this study?");
                    aiResponses.Add("Recent developments in this field include breakthroughs in methodologies and new theoretical frameworks...");
                    aiResponses.Add("The research paper explores the relationship between several key variables. Their findings suggest that...");
                    aiResponses.Add("For this study, I would recommend a mixed-methods approach. Quantitative data could be collected through surveys, while qualitative insights...");
                    break;
                    
                default:
                    // General category or undefined - already have base prompts
                    break;
            }
            
            // If the title contains specific keywords, add relevant prompts
            string titleLower = title?.ToLower() ?? "";
            if (titleLower.Contains("ai") || titleLower.Contains("machine learning"))
            {
                userPrompts.Add("How do neural networks function?");
                userPrompts.Add("What's the difference between supervised and unsupervised learning?");
                aiResponses.Add("Neural networks consist of layers of interconnected nodes or 'neurons' that process input data through weighted connections...");
                aiResponses.Add("Supervised learning involves training models on labeled data, where the desired output is known, whereas unsupervised learning works with unlabeled data...");
            }
            else if (titleLower.Contains("programming") || titleLower.Contains("code"))
            {
                userPrompts.Add("What design pattern should I use for this problem?");
                userPrompts.Add("How can I optimize this algorithm?");
                aiResponses.Add("For this problem, the Observer pattern might be most appropriate since you need to maintain consistency across multiple components...");
                aiResponses.Add("To optimize the algorithm, consider using a more efficient data structure such as a hash map instead of repeated linear searches...");
            }
            
            return (userPrompts, aiResponses);
        }
    }
}
