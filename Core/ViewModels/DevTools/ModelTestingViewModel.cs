using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Bogus;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexusChat.Core.Models;
using NexusChat.Data.Context;
using NexusChat.Helpers;
using System.Collections.ObjectModel;

namespace NexusChat.Core.ViewModels.DevTools
{
    /// <summary>
    /// ViewModel for model testing and random data generation
    /// </summary>
    public partial class ModelTestingViewModel : ObservableObject
    {
        private readonly DatabaseService _databaseService;
        private readonly Random _random = new Random();

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _logOutput = "Welcome to Model Testing\nUse the buttons below to generate test data.";

        [ObservableProperty]
        private int _userCount;

        [ObservableProperty]
        private int _conversationCount;

        [ObservableProperty]
        private int _messageCount;

        [ObservableProperty]
        private List<string> _generationResults = new List<string>();

        [ObservableProperty]
        private ObservableCollection<AIModel> _availableModels;

        [ObservableProperty]
        private AIModel _selectedModel;

        [ObservableProperty]
        private string _testPrompt;

        [ObservableProperty]
        private string _testResult;

        [ObservableProperty]
        private bool _isTesting;

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
        /// Command to test the selected model
        /// </summary>
        public IAsyncRelayCommand TestModelCommand { get; }

        /// <summary>
        /// Initializes a new instance of the ModelTestingViewModel class
        /// </summary>
        /// <param name="databaseService">Database service for data access</param>
        public ModelTestingViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _availableModels = new ObservableCollection<AIModel>();
            _testPrompt = "Explain what an AI language model is in 50 words or less.";

            GenerateUsersCommand = new AsyncRelayCommand(GenerateRandomUsers);
            GenerateConversationsCommand = new AsyncRelayCommand(GenerateRandomConversations);
            GenerateMessagesCommand = new AsyncRelayCommand(GenerateRandomMessages);
            ClearLogCommand = new AsyncRelayCommand(ClearLog);
            GoBackCommand = new AsyncRelayCommand(GoBack);
            TestModelCommand = new AsyncRelayCommand(TestModelAsync, CanTestModel);

            // Initialize counts
            RefreshCounts();
        }

        /// <summary>
        /// Constructor for design-time support
        /// </summary>
        public ModelTestingViewModel()
        {
            GenerateUsersCommand = new AsyncRelayCommand(() => Task.CompletedTask);
            GenerateConversationsCommand = new AsyncRelayCommand(() => Task.CompletedTask);
            GenerateMessagesCommand = new AsyncRelayCommand(() => Task.CompletedTask);
            ClearLogCommand = new AsyncRelayCommand(() => Task.CompletedTask);
            GoBackCommand = new AsyncRelayCommand(() => Task.CompletedTask);
            TestModelCommand = new AsyncRelayCommand(() => Task.CompletedTask);
        }

        /// <summary>
        /// Generates random user data
        /// </summary>
        private async Task GenerateRandomUsers()
        {
            if (IsBusy) return;
            
            IsBusy = true;
            LogMessage("Generating random users...");
            
            try
            {
                await _databaseService.Initialize();
                
                // Create fake data generator
                var faker = new Faker<User>()
                    .RuleFor(u => u.Username, f => CleanUsername(f.Internet.UserName()))
                    .RuleFor(u => u.PasswordHash, f => BCrypt.Net.BCrypt.HashPassword("password123"))
                    .RuleFor(u => u.DisplayName, f => f.Name.FullName())
                    .RuleFor(u => u.Email, f => f.Internet.Email())
                    .RuleFor(u => u.DateCreated, f => f.Date.Past(1))
                    .RuleFor(u => u.LastLogin, f => f.Date.Recent())
                    .RuleFor(u => u.PreferredTheme, f => f.Random.ArrayElement(new[] { "Light", "Dark", "System" }))
                    .RuleFor(u => u.IsEmailVerified, f => f.Random.Bool(0.7f))
                    .RuleFor(u => u.IsActive, f => f.Random.Bool(0.9f));
                
                // Generate 10 users
                var users = faker.Generate(10);
                
                // Save to database
                await _databaseService.Database.InsertAllAsync(users);
                
                LogMessage($"Successfully generated {users.Count} users.");
                await RefreshCounts();
            }
            catch (Exception ex)
            {
                LogMessage($"Error generating users: {ex.Message}");
                Debug.WriteLine($"Error: {ex}");
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
            
            IsBusy = true;
            LogMessage("Generating random conversations...");
            
            try
            {
                await _databaseService.Initialize();
                
                // Ensure we have users first
                var userCount = await _databaseService.Database.Table<User>().CountAsync();
                
                if (userCount == 0)
                {
                    LogMessage("No users found. Please generate users first.");
                    IsBusy = false;
                    return;
                }
                
                // Get all user IDs
                var userIds = new List<int>();
                var users = await _databaseService.Database.Table<User>().ToListAsync();
                foreach (var user in users)
                {
                    userIds.Add(user.Id);
                }
                
                // Create model definition
                var model1 = new AIModel
                {
                    ProviderName = "OpenAI",
                    ModelName = "GPT-4",
                    Description = "Advanced language model for general purpose AI tasks",
                    MaxTokens = 8192,
                    DefaultTemperature = 0.7f,
                    IsAvailable = true
                };
                
                var model2 = new AIModel
                {
                    ProviderName = "Anthropic",
                    ModelName = "Claude-3",
                    Description = "Advanced reasoning and conversation AI model",
                    MaxTokens = 100000,
                    DefaultTemperature = 0.5f,
                    IsAvailable = true
                };
                
                // Save models if not exist
                var existingModels = await _databaseService.Database.Table<AIModel>().ToListAsync();
                if (existingModels.Count == 0)
                {
                    await _databaseService.Database.InsertAsync(model1);
                    await _databaseService.Database.InsertAsync(model2);
                    LogMessage("Created AI models.");
                }
                
                // Get model IDs
                var modelIds = new List<int>();
                var models = await _databaseService.Database.Table<AIModel>().ToListAsync();
                foreach (var model in models)
                {
                    modelIds.Add(model.Id);
                }
                
                if (modelIds.Count == 0)
                {
                    LogMessage("No AI models found. Creating default models.");
                    await _databaseService.Database.InsertAsync(model1);
                    await _databaseService.Database.InsertAsync(model2);
                    models = await _databaseService.Database.Table<AIModel>().ToListAsync();
                    foreach (var model in models)
                    {
                        modelIds.Add(model.Id);
                    }
                }
                
                // Create fake data generator
                var faker = new Faker<Conversation>()
                    .RuleFor(c => c.UserId, f => f.PickRandom(userIds))
                    .RuleFor(c => c.Title, f => f.Lorem.Sentence(3, 3).TrimEnd('.'))
                    .RuleFor(c => c.ModelId, f => f.PickRandom(modelIds))
                    .RuleFor(c => c.CreatedAt, f => f.Date.Past(1))
                    .RuleFor(c => c.UpdatedAt, (f, c) => f.Date.Between(c.CreatedAt, DateTime.Now))
                    .RuleFor(c => c.IsFavorite, f => f.Random.Bool(0.3f))
                    .RuleFor(c => c.Category, f => f.Random.ArrayElement(new[] { "General", "Work", "Personal", "Creative", "Technical" }))
                    .RuleFor(c => c.Summary, f => f.Lorem.Paragraph())
                    .RuleFor(c => c.TotalTokensUsed, f => f.Random.Number(100, 5000));
                
                // Generate conversations
                var conversations = faker.Generate(20);
                
                // Save to database
                await _databaseService.Database.InsertAllAsync(conversations);
                
                LogMessage($"Successfully generated {conversations.Count} conversations.");
                await RefreshCounts();
            }
            catch (Exception ex)
            {
                LogMessage($"Error generating conversations: {ex.Message}");
                Debug.WriteLine($"Error: {ex}");
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
            
            IsBusy = true;
            LogMessage($"Generating {MessageCount} random messages...");
            
            try
            {
                await _databaseService.Initialize();
                
                // Ensure we have conversations first
                var convoCount = await _databaseService.Database.Table<Conversation>().CountAsync();
                
                if (convoCount == 0)
                {
                    LogMessage("No conversations found. Please generate conversations first.");
                    IsBusy = false;
                    return;
                }
                
                // Get all conversation IDs
                var conversations = await _databaseService.Database.Table<Conversation>().ToListAsync();
                
                // Create test data generator
                var generator = new TestDataGenerator(_databaseService);
                int savedCount = 0;
                
                // Generate messages for each conversation
                foreach (var conversation in conversations)
                {
                    // Determine how many messages to create for this conversation
                    int messagesToCreate = MessageCount / conversations.Count;
                    if (messagesToCreate == 0) messagesToCreate = 1;
                    
                    var messages = await generator.GenerateMessagesAsync(conversation.Id, messagesToCreate);
                    savedCount += messages.Count;
                    
                    // Update conversation tokens
                    int totalTokens = messages.Where(m => m.IsAI).Sum(m => m.TokensUsed);
                    conversation.TotalTokensUsed += totalTokens;
                    await _databaseService.Database.UpdateAsync(conversation);
                    
                    await MainThread.InvokeOnMainThreadAsync(() => {
                        GenerationResults.Add($"Created {messages.Count} messages in conversation {conversation.Id}");
                    });
                }
                
                LogMessage($"Successfully created {savedCount} messages.");
                await RefreshCounts();
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
            return Task.CompletedTask;
        }

        /// <summary>
        /// Navigates back
        /// </summary>
        private async Task GoBack()
        {
            await Shell.Current.GoToAsync("..");
        }

        /// <summary>
        /// Adds a message to the log output
        /// </summary>
        private void LogMessage(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string logEntry = $"[{timestamp}] {message}";
            
            // Append to log, keeping max 100 lines
            var lines = LogOutput.Split('\n').ToList();
            lines.Add(logEntry);
            
            // Keep max 100 lines
            if (lines.Count > 100)
            {
                lines = lines.Skip(lines.Count - 100).ToList();
            }
            
            LogOutput = string.Join('\n', lines);
        }

        /// <summary>
        /// Refreshes the counts from the database
        /// </summary>
        private async Task RefreshCounts()
        {
            try
            {
                await _databaseService.Initialize();
                
                UserCount = await _databaseService.Database.Table<User>().CountAsync();
                ConversationCount = await _databaseService.Database.Table<Conversation>().CountAsync();
                MessageCount = await _databaseService.Database.Table<Message>().CountAsync();
            }
            catch (Exception ex)
            {
                LogMessage($"Error refreshing counts: {ex.Message}");
            }
        }

        // Helper to clean usernames (replacement for the missing CleanUsername extension)
        private string CleanUsername(string username)
        {
            if (string.IsNullOrEmpty(username))
                return "user";

            // Remove special characters, replace spaces with underscores
            var cleaned = new string(username.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '.').ToArray());
            
            // Ensure it's not empty after cleaning
            if (string.IsNullOrEmpty(cleaned))
                return "user";
            
            return cleaned;
        }

        /// <summary>
        /// Initialize the ViewModel
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                // Load available models
                AvailableModels.Clear();
                
                // For now add a dummy model
                AvailableModels.Add(new AIModel
                {
                    Id = 1,
                    ProviderName = "DummyProvider",
                    ModelName = "Dummy Model v1",
                    Description = "A test model for development purposes",
                    MaxTokens = 2048,
                    DefaultTemperature = 0.7f,
                    IsAvailable = true
                });
                
                if (AvailableModels.Count > 0)
                {
                    SelectedModel = AvailableModels[0];
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing ModelTestingViewModel: {ex.Message}");
            }
        }

        /// <summary>
        /// Clean up any resources used by the ViewModel
        /// </summary>
        public void Cleanup()
        {
            // Clean up any resources or event handlers
            // Currently nothing to clean up
        }

        private bool CanTestModel()
        {
            return SelectedModel != null && 
                   !string.IsNullOrWhiteSpace(TestPrompt) &&
                   !IsTesting;
        }

        private async Task TestModelAsync()
        {
            if (SelectedModel == null || string.IsNullOrWhiteSpace(TestPrompt))
                return;
                
            try
            {
                IsTesting = true;
                TestResult = "Testing...";
                
                // Simulate a delay
                await Task.Delay(1500);
                
                // For now, return a dummy result
                TestResult = $"This is a simulated response from {SelectedModel.ModelName}.\n\n" +
                             "An AI language model is a computational system trained on vast text data to " +
                             "recognize, predict, and generate human language. It can answer questions, " +
                             "write content, translate languages, and simulate conversations without true understanding.";
            }
            catch (Exception ex)
            {
                TestResult = $"Error: {ex.Message}";
                Debug.WriteLine($"Error testing model: {ex}");
            }
            finally
            {
                IsTesting = false;
            }
        }
    }
}

