using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexusChat.Data;
using NexusChat.Models;
using Bogus;
using System.Linq;
using System.Collections.Generic;

namespace NexusChat.ViewModels
{
    /// <summary>
    /// ViewModel for model testing and random data generation
    /// </summary>
    public partial class ModelTestingViewModel : ObservableObject
    {
        private readonly DatabaseService _databaseService;
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool _isBusy;

        [ObservableProperty]
        private string _logOutput = "Select an option to generate random data...";

        [ObservableProperty]
        private int _userCount = 10;

        [ObservableProperty]
        private int _conversationCount = 15;

        [ObservableProperty]
        private int _messageCount = 30;

        [ObservableProperty]
        private ObservableCollection<string> _generationResults = new();

        /// <summary>
        /// Gets whether the ViewModel is not busy
        /// </summary>
        public bool IsNotBusy => !IsBusy;
        
        /// <summary>
        /// Command to generate random users
        /// </summary>
        public IAsyncRelayCommand GenerateUsersCommand { get; }
        
        /// <summary>
        /// Command to generate random conversations
        /// </summary>
        public IAsyncRelayCommand GenerateConversationsCommand { get; }
        
        /// <summary>
        /// Command to generate random messages
        /// </summary>
        public IAsyncRelayCommand GenerateMessagesCommand { get; }
        
        /// <summary>
        /// Command to clear all generated data
        /// </summary>
        public IAsyncRelayCommand ClearLogCommand { get; }
        
        /// <summary>
        /// Command to navigate back
        /// </summary>
        public IAsyncRelayCommand GoBackCommand { get; }
        
        /// <summary>
        /// Initializes a new instance of the ModelTestingViewModel class
        /// </summary>
        /// <param name="databaseService">Database service for data access</param>
        public ModelTestingViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            
            GenerateUsersCommand = new AsyncRelayCommand(GenerateRandomUsers);
            GenerateConversationsCommand = new AsyncRelayCommand(GenerateRandomConversations);
            GenerateMessagesCommand = new AsyncRelayCommand(GenerateRandomMessages);
            ClearLogCommand = new AsyncRelayCommand(ClearLog);
            GoBackCommand = new AsyncRelayCommand(GoBack);
        }

        /// <summary>
        /// Constructor for design-time support
        /// </summary>
        public ModelTestingViewModel() : this(new DatabaseService())
        {
        }

        /// <summary>
        /// Generates random user data
        /// </summary>
        private async Task GenerateRandomUsers()
        {
            if (IsBusy) return;
            
            try
            {
                IsBusy = true;
                LogMessage($"Generating {UserCount} random users...");
                var sw = Stopwatch.StartNew(); // Track time for performance analysis
                
                // Initialize the database
                await _databaseService.Initialize();
                
                // Store results to update UI in batch at the end
                var resultsToDisplay = new List<string>();
                
                // Run the CPU-intensive generation on a background thread
                var users = await Task.Run(() => {
                    // Create a Faker for User - use lower work factor for BCrypt in development
                    // Standard is 10, lower values are faster but less secure
                    const int BCRYPT_DEV_WORK_FACTOR = 4; 
                    
                    var faker = new Faker<User>()
                        .CustomInstantiator(f => new User(
                            f.Internet.UserName(),
                            BCrypt.Net.BCrypt.HashPassword(f.Internet.Password(), workFactor: BCRYPT_DEV_WORK_FACTOR)
                        ))
                        .RuleFor(u => u.DisplayName, f => f.Name.FullName())
                        .RuleFor(u => u.Email, f => f.Internet.Email())
                        .RuleFor(u => u.AvatarPath, f => f.Internet.Avatar())
                        .RuleFor(u => u.DateCreated, f => f.Date.Past(2))
                        .RuleFor(u => u.LastLogin, f => f.Date.Recent(30))
                        .RuleFor(u => u.PreferredTheme, f => f.PickRandom("Light", "Dark", "System"))
                        .RuleFor(u => u.IsEmailVerified, f => f.Random.Bool(0.7f))
                        .RuleFor(u => u.IsActive, f => f.Random.Bool(0.9f));
                    
                    return faker.Generate(UserCount);
                });
                
                LogMessage($"Generated {users.Count} users in memory in {sw.ElapsedMilliseconds}ms. Saving to database...");
                
                // Use transaction for faster inserts
                await _databaseService.Database.RunInTransactionAsync(tran => {
                    foreach (var user in users)
                    {
                        try
                        {
                            tran.Insert(user);
                            resultsToDisplay.Add($"Created user: {user.Username} ({user.DisplayName})");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error saving user {user.Username}: {ex.Message}");
                        }
                    }
                });
                
                // Update the UI once with all results
                await MainThread.InvokeOnMainThreadAsync(() => {
                    foreach (var result in resultsToDisplay)
                    {
                        GenerationResults.Add(result);
                    }
                });
                
                sw.Stop();
                LogMessage($"Successfully created {resultsToDisplay.Count} out of {UserCount} users in {sw.ElapsedMilliseconds}ms total.");
            }
            catch (Exception ex)
            {
                LogMessage($"Error generating users: {ex.Message}");
                Debug.WriteLine($"Error in GenerateRandomUsers: {ex}");
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        /// <summary>
        /// Generates random conversation data
        /// </summary>
        private async Task GenerateRandomConversations()
        {
            if (IsBusy) return;
            
            try
            {
                IsBusy = true;
                LogMessage($"Generating {ConversationCount} random conversations...");
                
                // Initialize the database
                await _databaseService.Initialize();
                
                // Get existing users
                var users = await _databaseService.Database.Table<User>().ToListAsync();
                if (users.Count == 0)
                {
                    LogMessage("No users found in database. Please generate users first.");
                    return;
                }
                
                // Run intensive generation on background thread
                var conversations = await Task.Run(() => {
                    // Prepare possible categories and model ids
                    var categories = new[] { "General", "Work", "Personal", "Creative", "Technical", "Education", "Entertainment" };
                    var modelIds = Enumerable.Range(1, 5).ToList(); // Assuming model IDs 1-5
    
                    // Create a Faker for Conversation
                    var faker = new Faker<Conversation>()
                        .CustomInstantiator(f => new Conversation(
                            f.PickRandom(users).Id,
                            f.Lorem.Sentence(3, 2),
                            f.PickRandom(modelIds)
                        ))
                        .RuleFor(c => c.Category, f => f.PickRandom(categories))
                        .RuleFor(c => c.CreatedAt, f => f.Date.Past(1))
                        .RuleFor(c => c.UpdatedAt, (f, c) => f.Date.Between(c.CreatedAt, DateTime.UtcNow))
                        .RuleFor(c => c.IsFavorite, f => f.Random.Bool(0.3f))
                        .RuleFor(c => c.Summary, f => f.Lorem.Paragraph())
                        .RuleFor(c => c.TotalTokensUsed, f => f.Random.Number(100, 5000));
                    
                    return faker.Generate(ConversationCount);
                });
                
                // Save to database
                int savedCount = 0;
                foreach (var conversation in conversations)
                {
                    try
                    {
                        await _databaseService.Database.InsertAsync(conversation);
                        var user = users.FirstOrDefault(u => u.Id == conversation.UserId);
                        await MainThread.InvokeOnMainThreadAsync(() => {
                            GenerationResults.Add($"Created conversation: \"{conversation.Title}\" for {user?.Username ?? "Unknown"}");
                        });
                        savedCount++;
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Error saving conversation \"{conversation.Title}\": {ex.Message}");
                    }
                }
                
                LogMessage($"Successfully created {savedCount} out of {ConversationCount} conversations.");
            }
            catch (Exception ex)
            {
                LogMessage($"Error generating conversations: {ex.Message}");
                Debug.WriteLine($"Error in GenerateRandomConversations: {ex}");
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        /// <summary>
        /// Generates random message data
        /// </summary>
        private async Task GenerateRandomMessages()
        {
            if (IsBusy) return;
            
            try
            {
                IsBusy = true;
                LogMessage($"Generating {MessageCount} random messages...");
                
                // Initialize the database
                await _databaseService.Initialize();
                
                // Get existing conversations
                var conversations = await _databaseService.Database.Table<Conversation>().ToListAsync();
                if (conversations.Count == 0)
                {
                    LogMessage("No conversations found in database. Please generate conversations first.");
                    return;
                }
                
                // Run intensive generation on background thread
                var messages = await Task.Run(() => {
                    // Message types
                    var messageTypes = new[] { "Text", "Image", "File", "Audio", "System" };
                    var statuses = new[] { "Sent", "Delivered", "Read", "Failed", "Sending" };
                    
                    // Create a Faker for user messages
                    var userFaker = new Faker<Message>()
                        .CustomInstantiator(f => new Message(
                            f.PickRandom(conversations).Id,
                            f.Lorem.Paragraph(f.Random.Int(1, 3)),
                            false
                        ))
                        .RuleFor(m => m.Timestamp, f => f.Date.Recent(7))
                        .RuleFor(m => m.TokensUsed, f => f.Random.Number(5, 30))
                        .RuleFor(m => m.MessageType, f => f.PickRandom("Text"))
                        .RuleFor(m => m.Status, f => f.PickRandom(statuses));
                    
                    // Create a Faker for AI messages
                    var aiFaker = new Faker<Message>()
                        .CustomInstantiator(f => new Message(
                            f.PickRandom(conversations).Id,
                            f.Lorem.Paragraphs(f.Random.Int(1, 5)),
                            true
                        ))
                        .RuleFor(m => m.Timestamp, f => f.Date.Recent(7))
                        .RuleFor(m => m.TokensUsed, f => f.Random.Number(20, 500))
                        .RuleFor(m => m.MessageType, f => f.PickRandom("Text"))
                        .RuleFor(m => m.Status, f => "Delivered")
                        .RuleFor(m => m.RawResponse, f => $"{{\"choices\":[{{\"message\":{{\"content\":\"{f.Lorem.Paragraph().Replace("\"", "\\\"")}\"}}}}]}}");
                    
                    // Generate messages (alternating user and AI)
                    var result = new List<Message>();
                    for (int i = 0; i < MessageCount; i++)
                    {
                        if (i % 2 == 0)
                            result.Add(userFaker.Generate());
                        else
                            result.Add(aiFaker.Generate());
                    }
                    return result;
                });
                
                // Save to database
                int savedCount = 0;
                foreach (var message in messages)
                {
                    try
                    {
                        await _databaseService.Database.InsertAsync(message);
                        
                        // Update conversation with token count
                        var conversation = conversations.FirstOrDefault(c => c.Id == message.ConversationId);
                        if (conversation != null)
                        {
                            conversation.AddTokens(message.TokensUsed);
                            await _databaseService.Database.UpdateAsync(conversation);
                        }
                        
                        await MainThread.InvokeOnMainThreadAsync(() => {
                            GenerationResults.Add($"Created {(message.IsAI ? "AI" : "User")} message in conversation {message.ConversationId}");
                        });
                        savedCount++;
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Error saving message: {ex.Message}");
                    }
                }
                
                LogMessage($"Successfully created {savedCount} out of {MessageCount} messages.");
            }
            catch (Exception ex)
            {
                LogMessage($"Error generating messages: {ex.Message}");
                Debug.WriteLine($"Error in GenerateRandomMessages: {ex}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Clears the log output
        /// </summary>
        private Task ClearLog()
        {
            LogOutput = "Log cleared.";
            GenerationResults.Clear();
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Navigates back
        /// </summary>
        private Task GoBack()
        {
            return Shell.Current.GoToAsync("..");
        }
        
        /// <summary>
        /// Adds a message to the log output
        /// </summary>
        private void LogMessage(string message)
        {
            MainThread.BeginInvokeOnMainThread(() => {
                LogOutput = $"{DateTime.Now:HH:mm:ss} - {message}\n{LogOutput}";
            });
            Debug.WriteLine(message);
        }
    }
}

