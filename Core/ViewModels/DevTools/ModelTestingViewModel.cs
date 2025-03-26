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
using Microsoft.Maui.Controls;
using System.Threading;

namespace NexusChat.Core.ViewModels.DevTools
{
    /// <summary>
    /// ViewModel for model testing and random data generation
    /// </summary>
    public partial class ModelTestingViewModel : ObservableObject
    {
        private readonly DatabaseService _databaseService;
        private readonly TestDataGenerator _testDataGenerator;
        private CancellationTokenSource _cancellationTokenSource;

        // Using partial modifier for ObservableProperty to ensure property change notifications
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool _isBusy;

        [ObservableProperty]
        private string _logOutput = "Welcome to Model Testing\nUse the buttons below to generate test data.";

        // Database statistics properties - shows current counts in database
        [ObservableProperty]
        private int _dbUserCount;

        [ObservableProperty]
        private int _dbConversationCount;

        [ObservableProperty]
        private int _dbMessageCount;

        // Generation count properties - for user input on how many to generate
        [ObservableProperty]
        private int _generateUserCount = 2;

        [ObservableProperty]
        private int _generateConversationCount = 5;

        [ObservableProperty]
        private int _generateMessageCount = 20;

        // Collection for displaying generated entities
        [ObservableProperty]
        private ObservableCollection<object> _generatedEntities = new ObservableCollection<object>();

        // Legacy property for backward compatibility
        [ObservableProperty] 
        private ObservableCollection<string> _generationResults = new ObservableCollection<string>();

        [ObservableProperty]
        private ObservableCollection<AIModel> _availableModels;

        [ObservableProperty]
        private AIModel _selectedModel;

        [ObservableProperty]
        private string _testPrompt = "Explain what an AI language model is in 50 words or less.";

        [ObservableProperty]
        private string _testResult;

        [ObservableProperty]
        private bool _isTesting;

        /// <summary>
        /// Gets whether the ViewModel is not busy
        /// </summary>
        public bool IsNotBusy => !IsBusy;

        /// <summary>
        /// Initializes a new instance of the ModelTestingViewModel class
        /// </summary>
        /// <param name="databaseService">Database service for data access</param>
        public ModelTestingViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _testDataGenerator = new TestDataGenerator(databaseService);
            _availableModels = new ObservableCollection<AIModel>();

            // Initialize commands
            GenerateUsersCommand = new AsyncRelayCommand(GenerateRandomUsers, () => IsNotBusy);
            GenerateConversationsCommand = new AsyncRelayCommand(GenerateRandomConversations, () => IsNotBusy);
            GenerateMessagesCommand = new AsyncRelayCommand(GenerateRandomMessages, () => IsNotBusy);
            ClearLogCommand = new AsyncRelayCommand(ClearLog);
            GoBackCommand = new AsyncRelayCommand(GoBack);
            TestModelCommand = new AsyncRelayCommand(TestModelAsync, CanTestModel);
            RefreshCommand = new AsyncRelayCommand(RefreshCounts);
            CancelGenerationCommand = new AsyncRelayCommand(CancelGeneration, () => IsBusy);

            // Initialize counts
            RefreshCounts();
        }

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
        /// Command to refresh database statistics
        /// </summary>
        public IAsyncRelayCommand RefreshCommand { get; }
        
        /// <summary>
        /// Command to cancel an ongoing generation process
        /// </summary>
        public IAsyncRelayCommand CancelGenerationCommand { get; }

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
            RefreshCommand = new AsyncRelayCommand(() => Task.CompletedTask);
            CancelGenerationCommand = new AsyncRelayCommand(() => Task.CompletedTask);
        }

        /// <summary>
        /// Generates random user data
        /// </summary>
        private async Task GenerateRandomUsers()
        {
            if (IsBusy) return;
            
            try
            {
                SetupCancellationToken();
                IsBusy = true;
                LogMessage($"Generating {GenerateUserCount} random users...");
                
                // Clear previous generation display
                await ClearGenerationDisplay();
                
                // Use TestDataGenerator to generate users
                var users = await _testDataGenerator.GenerateUsersAsync(
                    GenerateUserCount, 
                    GeneratedEntities, 
                    _cancellationTokenSource.Token);
                
                // Update generation results for backward compatibility
                await MainThread.InvokeOnMainThreadAsync(() => 
                {
                    GenerationResults.Add($"Successfully generated {users.Count} users.");
                    
                    // Add indicator if more than what's shown in GeneratedEntities
                    if (users.Count > GeneratedEntities.Count)
                    {
                        GenerationResults.Add($"...and {users.Count - GeneratedEntities.Count} more");
                    }
                });
                
                LogMessage($"Successfully generated {users.Count} users.");
                await RefreshCounts();
            }
            catch (OperationCanceledException)
            {
                LogMessage("User generation was canceled.");
            }
            catch (Exception ex)
            {
                LogMessage($"Error generating users: {ex.Message}");
                Debug.WriteLine($"Error in GenerateRandomUsers: {ex}");
            }
            finally
            {
                await CleanupOperation();
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
                SetupCancellationToken();
                IsBusy = true;
                LogMessage($"Generating {GenerateConversationCount} random conversations...");
                
                // Clear previous generation display
                await ClearGenerationDisplay();
                
                // Use TestDataGenerator to generate conversations
                var conversations = await _testDataGenerator.GenerateConversationsAsync(
                    GenerateConversationCount, 
                    GeneratedEntities, 
                    _cancellationTokenSource.Token);
                
                // Update generation results for backward compatibility
                await MainThread.InvokeOnMainThreadAsync(() => 
                {
                    GenerationResults.Add($"Successfully generated {conversations.Count} conversations.");
                    
                    if (conversations.Count > GeneratedEntities.Count)
                    {
                        GenerationResults.Add($"...and {conversations.Count - GeneratedEntities.Count} more");
                    }
                });
                
                LogMessage($"Successfully generated {conversations.Count} conversations.");
                await RefreshCounts();
            }
            catch (OperationCanceledException)
            {
                LogMessage("Conversation generation was canceled.");
            }
            catch (InvalidOperationException ex)
            {
                LogMessage(ex.Message);
                Debug.WriteLine($"Operation Error: {ex}");
            }
            catch (Exception ex)
            {
                LogMessage($"Error generating conversations: {ex.Message}");
                Debug.WriteLine($"Error in GenerateRandomConversations: {ex}");
            }
            finally
            {
                await CleanupOperation();
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
                SetupCancellationToken();
                IsBusy = true;
                LogMessage($"Generating {GenerateMessageCount} messages across conversations...");
                
                // Clear previous generation results (but not entities)
                GenerationResults.Clear();
                
                // Use TestDataGenerator to generate messages
                var messages = await _testDataGenerator.GenerateMessagesAsync(
                    GenerateMessageCount, 
                    GeneratedEntities, 
                    _cancellationTokenSource.Token);
                
                // Get message counts per conversation for display
                var messagesByConversation = messages.GroupBy(m => m.ConversationId)
                    .ToDictionary(g => g.Key, g => g.Count());
                
                // Update generation results for backward compatibility
                await MainThread.InvokeOnMainThreadAsync(() => 
                {
                    foreach (var kvp in messagesByConversation)
                    {
                        GenerationResults.Add($"Created {kvp.Value} messages in conversation {kvp.Key}");
                    }
                    
                    GenerationResults.Add($"Total: {messages.Count} messages created");
                });
                
                LogMessage($"Successfully created {messages.Count} messages across {messagesByConversation.Count} conversations.");
                await RefreshCounts();
            }
            catch (OperationCanceledException)
            {
                LogMessage("Message generation was canceled.");
            }
            catch (InvalidOperationException ex)
            {
                LogMessage(ex.Message);
                Debug.WriteLine($"Operation Error: {ex}");
            }
            catch (Exception ex)
            {
                LogMessage($"Error generating messages: {ex.Message}");
                Debug.WriteLine($"Error in GenerateRandomMessages: {ex}");
            }
            finally
            {
                await CleanupOperation();
            }
        }

        /// <summary>
        /// Clears the log output and generated items
        /// </summary>
        private Task ClearLog()
        {
            LogOutput = "Log cleared.";
            GenerationResults.Clear();
            GeneratedEntities.Clear();
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Clears only the generation display, not the log
        /// </summary>
        private async Task ClearGenerationDisplay()
        {
            await MainThread.InvokeOnMainThreadAsync(() => 
            {
                GenerationResults.Clear();
                GeneratedEntities.Clear();
            });
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
            if (_databaseService == null) return;
            
            bool wasBusy = IsBusy;
            if (!wasBusy) IsBusy = true;
            
            try
            {
                await _databaseService.Initialize();
                
                // Update database statistics (not generation counts)
                DbUserCount = await _databaseService.Database.Table<User>().CountAsync();
                DbConversationCount = await _databaseService.Database.Table<Conversation>().CountAsync();
                DbMessageCount = await _databaseService.Database.Table<Message>().CountAsync();
                
                // Load available models from database
                var dbModels = await _databaseService.Database.Table<AIModel>().ToListAsync();
                
                // Create default models if none exist
                if (dbModels.Count == 0)
                {
                    dbModels = await _testDataGenerator.EnsureDefaultModelsExistAsync();
                }
                
                await MainThread.InvokeOnMainThreadAsync(() => 
                {
                    AvailableModels.Clear();
                    foreach (var model in dbModels)
                    {
                        AvailableModels.Add(model);
                    }
                    
                    if (AvailableModels.Count > 0 && SelectedModel == null)
                    {
                        SelectedModel = AvailableModels[0];
                    }
                });
                
                LogMessage("Database statistics refreshed.");
            }
            catch (Exception ex)
            {
                LogMessage($"Error refreshing counts: {ex.Message}");
                Debug.WriteLine($"Error refreshing counts: {ex}");
            }
            finally
            {
                if (!wasBusy) IsBusy = false;
            }
        }

        /// <summary>
        /// Initialize the ViewModel
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                await _databaseService.Initialize();
                
                // Load available models, creating defaults if none exist
                var dbModels = await _databaseService.Database.Table<AIModel>().ToListAsync();
                
                if (dbModels.Count == 0)
                {
                    dbModels = await _testDataGenerator.EnsureDefaultModelsExistAsync();
                }
                
                await MainThread.InvokeOnMainThreadAsync(() => 
                {
                    AvailableModels.Clear();
                    
                    foreach (var model in dbModels)
                    {
                        AvailableModels.Add(model);
                    }
                    
                    if (AvailableModels.Count > 0)
                    {
                        SelectedModel = AvailableModels[0];
                    }
                });
                
                await RefreshCounts();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing ModelTestingViewModel: {ex.Message}");
                LogMessage($"Initialization error: {ex.Message}");
            }
        }

        /// <summary>
        /// Clean up any resources used by the ViewModel
        /// </summary>
        public void Cleanup()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        /// <summary>
        /// Checks if model testing can be performed
        /// </summary>
        private bool CanTestModel()
        {
            return SelectedModel != null && 
                   !string.IsNullOrWhiteSpace(TestPrompt) &&
                   !IsTesting;
        }

        /// <summary>
        /// Tests the selected model
        /// </summary>
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
        
        /// <summary>
        /// Sets up a new cancellation token source for a generation operation
        /// </summary>
        private void SetupCancellationToken()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
        }
        
        /// <summary>
        /// Cancels the current generation operation
        /// </summary>
        private Task CancelGeneration()
        {
            _cancellationTokenSource?.Cancel();
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Cleans up after an operation completes
        /// </summary>
        private async Task CleanupOperation()
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            
            await MainThread.InvokeOnMainThreadAsync(() => IsBusy = false);
        }
    }
}

