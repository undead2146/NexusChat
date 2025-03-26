using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexusChat.Core.Models;
using NexusChat.Data.Repositories;
using NexusChat.Services.Interfaces;
using Microsoft.Maui.Controls;

namespace NexusChat.Core.ViewModels
{
    /// <summary>
    /// ViewModel for the chat interaction page
    /// </summary>
    public partial class ChatViewModel : ObservableObject
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IConversationRepository _conversationRepository;
        private readonly IAIService _aiService;
        private CancellationTokenSource _cancellationTokenSource;
        
        [ObservableProperty]
        private ObservableCollection<Message> _messages = new();
        
        [ObservableProperty]
        private string _messageText;
        
        [ObservableProperty]
        private Conversation _currentConversation;
        
        [ObservableProperty]
        private AIModel _currentAIModel;
        
        [ObservableProperty]
        private bool _isBusy;
        
        [ObservableProperty]
        private bool _isAITyping;
        
        [ObservableProperty]
        private bool _showScrollToBottom;

        [ObservableProperty] 
        private string _statusMessage;
        
        /// <summary>
        /// Gets if the ViewModel is not busy
        /// </summary>
        public bool IsNotBusy => !IsBusy;
        
        /// <summary>
        /// Gets if the user can send a message
        /// </summary>
        public bool CanSendMessage => !string.IsNullOrWhiteSpace(MessageText) && !IsBusy;
        
        /// <summary>
        /// Command to send a message
        /// </summary>
        public IAsyncRelayCommand SendMessageCommand { get; }
        
        /// <summary>
        /// Command to refresh messages
        /// </summary>
        public IAsyncRelayCommand RefreshCommand { get; }
        
        /// <summary>
        /// Command to navigate back
        /// </summary>
        public IAsyncRelayCommand GoBackCommand { get; }
        
        /// <summary>
        /// Command to show chat options
        /// </summary>
        public IAsyncRelayCommand ChatOptionsCommand { get; }
        
        /// <summary>
        /// Command to handle entry focus
        /// </summary>
        public IRelayCommand EntryFocusedCommand { get; }
        
        /// <summary>
        /// Command to scroll to bottom of chat
        /// </summary>
        public IAsyncRelayCommand ScrollToBottomCommand { get; }
        
        /// <summary>
        /// Command to change the AI model
        /// </summary>
        public IAsyncRelayCommand ChangeModelCommand { get; }
        
        /// <summary>
        /// Command to handle attachment button
        /// </summary>
        public IAsyncRelayCommand AttachmentCommand { get; }
        
        /// <summary>
        /// Command to use a suggested prompt
        /// </summary>
        public IRelayCommand<string> UsePromptSuggestionCommand { get; }
        
        /// <summary>
        /// Command to edit the conversation title
        /// </summary>
        public IAsyncRelayCommand EditTitleCommand { get; }

        /// <summary>
        /// Initialize a new instance of ChatViewModel
        /// </summary>
        public ChatViewModel(IMessageRepository messageRepository, IConversationRepository conversationRepository, IAIService aiService)
        {
            _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
            _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
            _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
            
            // Initialize commands
            SendMessageCommand = new AsyncRelayCommand(SendMessageAsync, () => CanSendMessage);
            RefreshCommand = new AsyncRelayCommand(RefreshMessagesAsync);
            GoBackCommand = new AsyncRelayCommand(GoBackAsync);
            ChatOptionsCommand = new AsyncRelayCommand(ShowChatOptionsAsync);
            EntryFocusedCommand = new RelayCommand(OnEntryFocused);
            ScrollToBottomCommand = new AsyncRelayCommand(ScrollToBottomAsync);
            ChangeModelCommand = new AsyncRelayCommand(ChangeModelAsync);
            AttachmentCommand = new AsyncRelayCommand(ShowAttachmentOptionsAsync);
            UsePromptSuggestionCommand = new RelayCommand<string>(UsePromptSuggestion);
            EditTitleCommand = new AsyncRelayCommand(EditConversationTitleAsync); 
            
            // Create dummy AI model for testing
            CurrentAIModel = new AIModel
            {
                Id = 1,
                ModelName = "GPT-4 Turbo",
                ProviderName = "OpenAI",
                IsAvailable = true,
                MaxTokens = 4096,
                DefaultTemperature = 0.7f  
            };
            
            // Initialize cancellation token source
            _cancellationTokenSource = new CancellationTokenSource();
        }
        
        /// <summary>
        /// Initialize the ViewModel
        /// </summary>
        public async Task InitializeAsync()
        {
            if (IsBusy) return;
            
            try
            {
                IsBusy = true;
                
                // Run database initialization in a background task
                await Task.Run(async () => {
                    try 
                    {
                        await _messageRepository.EnsureDatabaseAsync();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error initializing database: {ex.Message}");
                        StatusMessage = "Error initializing database";
                    }
                });
                
                // Typically this would load the conversation ID from navigation parameters
                if (CurrentConversation == null)
                {
                    int conversationId = 1; // Default for testing
                    await LoadOrCreateConversationAsync(conversationId);
                }
                
                // Load messages for the conversation asynchronously
                await RefreshMessagesAsync();
                
                // Generate title if it's still the default - look at first user message
                await EnsureConversationHasTitle();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing chat: {ex.Message}");
                StatusMessage = "Error initializing chat";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Loads an existing conversation or creates a new one
        /// </summary>
        private async Task LoadOrCreateConversationAsync(int conversationId)
        {
            try
            {
                Debug.WriteLine("Attempting to load conversation");
                CurrentConversation = await _conversationRepository.GetByIdAsync(conversationId);
                
                if (CurrentConversation == null)
                {
                    Debug.WriteLine("No conversation found, creating new one");
                    // Create a new conversation
                    CurrentConversation = new Conversation
                    {
                        UserId = 1, // Current user ID
                        Title = "New Chat",
                        ModelId = CurrentAIModel?.Id ?? 1,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now,
                        TotalTokensUsed = 0
                    };
                    
                    // Save the new conversation
                    await _conversationRepository.AddAsync(CurrentConversation);
                    Debug.WriteLine($"Created new conversation with ID: {CurrentConversation.Id}");
                }
                else
                {
                    Debug.WriteLine($"Loaded existing conversation with ID: {CurrentConversation.Id}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading/creating conversation: {ex.Message}");
                StatusMessage = "Error loading conversation";
                
                // Create a local conversation object so the UI can still function
                CurrentConversation = new Conversation
                {
                    Id = 0, // Temporary ID
                    UserId = 1,
                    Title = "Temporary Chat",
                    ModelId = 1,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
            }
        }
        
        /// <summary>
        /// Send a message to the AI
        /// </summary>
        private async Task SendMessageAsync()
        {
            // First check if we can send
            if (string.IsNullOrWhiteSpace(MessageText) || IsBusy)
                return;
            
            // Immediately grab a copy of the message text and clear the input
            string userMessageText = MessageText?.Trim() ?? "";
            MessageText = string.Empty;
            
            // Generate a unique message ID
            int messageId = (int)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 100000); 
            
            // Create user message object
            var userMessage = new Message
            {
                Id = messageId,
                ConversationId = CurrentConversation?.Id ?? 0,
                Content = userMessageText,
                IsAI = false,
                Timestamp = DateTime.Now,
                Status = "Sent"
            };
            
            // Add user message to UI collection IMMEDIATELY - don't await anything before this
            Messages.Add(userMessage);
            
            // Mark busy state AFTER adding the user message for instant feedback
            IsBusy = true;
            
            // NOW we can start the AI response process in the background
            _ = Task.Run(async () => 
            {
                try
                {
                    Debug.WriteLine("Starting SendMessageAsync background task");
                    
                    // Do token cleanup without waiting
                    CancelAndDisposeTokenSource();
                    
                    // Create a new token source
                    _cancellationTokenSource = new CancellationTokenSource();
                    var cancellationToken = _cancellationTokenSource.Token;
                    
                    // Clean up any existing typing messages before adding new ones
                    await CleanupTypingMessagesAsync();
                    
                    // Set typing indicator
                    await MainThread.InvokeOnMainThreadAsync(() => IsAITyping = true);
                    
                    // Create typing placeholder message with different ID
                    var typingMessage = new Message
                    {
                        Id = messageId + 1, // Ensure unique ID
                        ConversationId = CurrentConversation?.Id ?? 0,
                        Content = "Typing...",
                        IsAI = true,
                        Timestamp = DateTime.Now,
                        Status = "Typing"
                    };
                    
                    // Add typing message to UI
                    await MainThread.InvokeOnMainThreadAsync(() => Messages.Add(typingMessage));
                    
                    // Save user message to database without awaiting
                    _ = Task.Run(async () => 
                    {
                        try 
                        {
                            if (CurrentConversation?.Id > 0)
                            {
                                await _messageRepository.AddAsync(userMessage);
                            }
                        } 
                        catch (Exception ex) 
                        {
                            Debug.WriteLine($"Error saving user message: {ex.Message}");
                        }
                    });
                    
                    // Get AI response with timeout protection
                    string aiResponseText = null;
                    try
                    {
                        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        timeoutCts.CancelAfter(TimeSpan.FromSeconds(60)); // 1 minute timeout
                        
                        aiResponseText = await _aiService.SendMessageAsync(userMessageText, timeoutCts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.WriteLine("AI request was canceled");
                        if (cancellationToken.IsCancellationRequested) return;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"AI response error: {ex.Message}");
                    }
                    
                    // Remove typing message
                    await MainThread.InvokeOnMainThreadAsync(() => 
                    {
                        CleanupTypingMessages();
                        IsAITyping = false;
                    });
                    
                    // If we have a response, show it
                    if (!string.IsNullOrEmpty(aiResponseText))
                    {
                        // Create AI response message
                        var aiResponse = new Message
                        {
                            Id = messageId + 2,
                            ConversationId = CurrentConversation?.Id ?? 0,
                            Content = aiResponseText,
                            IsAI = true,
                            Timestamp = DateTime.Now,
                            TokensUsed = Message.EstimateTokens(aiResponseText),
                            Status = "Delivered"
                        };
                        
                        // Add AI message to UI
                        await MainThread.InvokeOnMainThreadAsync(() => Messages.Add(aiResponse));
                        
                        // Save AI message and update conversation
                        _ = Task.Run(async () => 
                        {
                            try 
                            {
                                if (CurrentConversation?.Id > 0)
                                {
                                    await _messageRepository.AddAsync(aiResponse);
                                    
                                    CurrentConversation.UpdatedAt = DateTime.Now;
                                    CurrentConversation.TotalTokensUsed += aiResponse.TokensUsed;
                                    
                                    // Update title if needed
                                    if (string.IsNullOrEmpty(CurrentConversation.Title) || 
                                        CurrentConversation.Title == "New Chat" ||
                                        CurrentConversation.Title == "Temporary Chat")
                                    {
                                        CurrentConversation.Title = GenerateTitle(userMessageText);
                                        await MainThread.InvokeOnMainThreadAsync(() => 
                                            OnPropertyChanged(nameof(CurrentConversation)));
                                    }
                                    
                                    await _conversationRepository.UpdateAsync(CurrentConversation);
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error saving AI response: {ex.Message}");
                            }
                        });
                    }
                    else
                    {
                        // If no response, add an error message
                        var errorMessage = new Message
                        {
                            Id = messageId + 2,
                            ConversationId = CurrentConversation?.Id ?? 0,
                            Content = "Sorry, I couldn't generate a response. Please try again.",
                            IsAI = true,
                            Timestamp = DateTime.Now,
                            Status = "Error"
                        };
                        
                        await MainThread.InvokeOnMainThreadAsync(() => Messages.Add(errorMessage));
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in async message processing: {ex}");
                }
                finally
                {
                    // Reset busy state
                    await MainThread.InvokeOnMainThreadAsync(() => 
                    {
                        IsBusy = false;
                        (SendMessageCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
                    });
                }
            });
        }
        
        /// <summary>
        /// Refresh the messages from the repository
        /// </summary>
        private async Task RefreshMessagesAsync()
        {
            if (CurrentConversation == null || CurrentConversation.Id <= 0)
                return;
                
            try
            {
                IsBusy = true;
                
                // Use ConfigureAwait(false) to avoid forcing continuation on the UI thread
                var messages = await _messageRepository.GetByConversationIdAsync(CurrentConversation.Id)
                    .ConfigureAwait(false);
                
                // Switch back to UI thread for collection updates
                await MainThread.InvokeOnMainThreadAsync(() => 
                {
                    Messages.Clear();
                    foreach (var message in messages)
                    {
                        Messages.Add(message);
                    }
                });
                
                // Ensure the conversation title is set properly after refreshing messages
                await EnsureConversationHasTitle();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error refreshing messages: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        /// <summary>
        /// Clean up empty messages from the collection
        /// </summary>
        public void CleanupEmptyMessages()
        {
            var emptyMessages = Messages.Where(m => string.IsNullOrWhiteSpace(m?.Content)).ToList();
            foreach (var message in emptyMessages)
            {
                Messages.Remove(message);
            }
        }
        
        /// <summary>
        /// Navigate back to the previous page
        /// </summary>
        private async Task GoBackAsync()
        {
            try
            {
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Navigation error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Show chat options menu
        /// </summary>
        private async Task ShowChatOptionsAsync()
        {
            try
            {
                string action = await Shell.Current.DisplayActionSheet(
                    "Chat Options",
                    "Cancel",
                    null,
                    "Clear Conversation",
                    "Share Conversation",
                    "Delete Conversation");
                    
                switch (action)
                {
                    case "Clear Conversation":
                        await ClearConversationAsync();
                        break;
                    case "Share Conversation":
                        await ShareConversationAsync();
                        break;
                    case "Delete Conversation":
                        await DeleteConversationAsync();
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing options: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Clear all messages in the conversation
        /// </summary>
        private async Task ClearConversationAsync()
        {
            if (CurrentConversation == null)
                return;
            
            try
            {
                bool confirm = await Shell.Current.DisplayAlert(
                    "Clear Conversation",
                    "Are you sure you want to clear all messages? This cannot be undone.",
                    "Clear",
                    "Cancel");
                    
                if (!confirm)
                    return;
                
                IsBusy = true;
                StatusMessage = "Clearing conversation...";
                
                // Ensure we're on the UI thread for the UI operations
                await MainThread.InvokeOnMainThreadAsync(async () => {
                    try
                    {
                        // First, clear the collection (this updates UI immediately)
                        Messages.Clear();
                        
                        // Then try to delete from database - handle errors gracefully
                        if (CurrentConversation.Id > 0)
                        {
                            try 
                            {
                                // Check if database is functioning by first ensuring it's initialized
                                await _messageRepository.EnsureDatabaseAsync();
                                
                                // Try delete operation (will return row count or 0 if error/table doesn't exist)
                                int rowsDeleted = await _messageRepository.DeleteByConversationIdAsync(CurrentConversation.Id);
                                Debug.WriteLine($"Cleared {rowsDeleted} messages from database");
                                
                                // Reset conversation properties - always do this for UI consistency
                                CurrentConversation.Title = "New Chat";
                                CurrentConversation.UpdatedAt = DateTime.Now;
                                CurrentConversation.TotalTokensUsed = 0;
                                
                                // Try to update conversation
                                await _conversationRepository.UpdateAsync(CurrentConversation);
                                
                                // Notify property change regardless
                                OnPropertyChanged(nameof(CurrentConversation));
                                Debug.WriteLine("Conversation cleared successfully");
                            }
                            catch (Exception dbEx)
                            {
                                Debug.WriteLine($"Error in ClearConversationAsync database operations: {dbEx}");
                                // Messages are already cleared from UI, so just show a warning
                                await Shell.Current.DisplayAlert("Warning", 
                                    "Messages were cleared from view but there was a problem updating the database.", 
                                    "OK");
                            }
                        }
                        
                        StatusMessage = "Conversation cleared";
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in ClearConversationAsync: {ex}");
                        await Shell.Current.DisplayAlert("Error", "Failed to clear conversation", "OK");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error clearing conversation: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", "Failed to clear conversation", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Share the conversation
        /// </summary>
        private async Task ShareConversationAsync()
        {
            await Shell.Current.DisplayAlert("Share", "Sharing not implemented yet", "OK");
        }

        /// <summary>
        /// Delete the conversation
        /// </summary>
        private async Task DeleteConversationAsync()
        {
            if (CurrentConversation == null)
                return;
            
            try
            {
                bool confirm = await Shell.Current.DisplayAlert(
                    "Delete Conversation",
                    "Are you sure you want to delete this conversation? This cannot be undone.",
                    "Delete",
                    "Cancel");
                    
                if (!confirm)
                    return;
                
                IsBusy = true;
                StatusMessage = "Deleting conversation...";
                
                // First clear UI for immediate feedback
                await MainThread.InvokeOnMainThreadAsync(() => Messages.Clear());
                
                // Then try database operations with better error handling
                if (CurrentConversation.Id > 0)
                {
                    try
                    {
                        // First make sure database is properly initialized
                        await _messageRepository.EnsureDatabaseAsync();
                        await _conversationRepository.EnsureDatabaseAsync();
                        
                        // First delete all messages - returns row count or 0 if error/no table
                        int messagesDeleted = await _messageRepository.DeleteByConversationIdAsync(CurrentConversation.Id);
                        Debug.WriteLine($"Deleted {messagesDeleted} messages for conversation {CurrentConversation.Id}");
                        
                        // Even if message deletion has issues, try to delete the conversation
                        try
                        {
                            // Fix the error on line 667 by explicitly casting the repository method
                            // Use the int-returning method directly from the ConversationRepository instead of through the interface
                            int deleteResult = await (_conversationRepository as ConversationRepository).DeleteAsync(CurrentConversation.Id);
                            Debug.WriteLine($"Conversation delete result: {deleteResult} rows affected");
                        }
                        catch (Exception conEx)
                        {
                            Debug.WriteLine($"Error deleting conversation: {conEx.Message}");
                            // Still proceed with navigation
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Database error: {ex.Message}");
                        await Shell.Current.DisplayAlert("Error", 
                            "There was a problem deleting the conversation data. The conversation will still be removed from your view.", 
                            "OK");
                    }
                }
                
                Debug.WriteLine("Conversation deleted successfully");
                
                // Navigate back regardless of database operations
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in delete conversation: {ex}");
                
                // Still try to navigate back even if deletion failed
                try
                {
                    await Shell.Current.DisplayAlert("Error", "An unexpected error occurred.", "OK");
                    await Shell.Current.GoToAsync("..");
                }
                catch
                {
                    Debug.WriteLine("Failed to navigate back after error");
                }
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        /// <summary>
        /// Handle entry focus event
        /// </summary>
        private void OnEntryFocused()
        {
            // This could be used to scroll to bottom when the user focuses the entry
            ShowScrollToBottom = false;
        }
        
        /// <summary>
        /// Scroll to the bottom of the message list
        /// </summary>
        private async Task ScrollToBottomAsync()
        {
            ShowScrollToBottom = false;
            await Task.Delay(10); // Give time for UI to update
        }
        
        /// <summary>
        /// Change the AI model
        /// </summary>
        private async Task ChangeModelAsync()
        {
            // This would typically show a model selection dialog
            await Shell.Current.DisplayAlert("Model Selection", "Model selection not implemented", "OK");
        }
        
        /// <summary>
        /// Show attachment options
        /// </summary>
        private async Task ShowAttachmentOptionsAsync()
        {
            try
            {
                string action = await Shell.Current.DisplayActionSheet(
                    "Attach",
                    "Cancel",
                    null,
                    "Take Photo",
                    "Choose from Gallery",
                    "Browse Files",
                    "Record Audio");
                    
                // Handle the selected action
                // Implementation would depend on platform-specific file handling
                if (action != "Cancel" && !string.IsNullOrEmpty(action))
                {
                    await Shell.Current.DisplayAlert("Attachment", $"{action} not implemented", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing attachment options: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Use a suggested prompt
        /// </summary>
        private void UsePromptSuggestion(string suggestion)
        {
            if (string.IsNullOrEmpty(suggestion))
                return;
                
            MessageText = suggestion;
            (SendMessageCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
        }
        
        /// <summary>
        /// Generate a title from the first message
        /// </summary>
        private string GenerateTitle(string message)
        {
            try
            {
                // Simplified algorithm: take first few words or first sentence
                string title = message.Split('.')[0]; // First sentence
                
                // Limit length
                if (title.Length > 30)
                {
                    title = title.Substring(0, 27) + "...";
                }
                
                return title;
            }
            catch
            {
                return "New Chat";
            }
        }
        
        /// <summary>
        /// Clean up resources
        /// </summary>
        public void Cleanup()
        {
            Debug.WriteLine("ChatViewModel cleanup");
            try
            {
                // Get reference and null out field to prevent double-cleanup
                var cts = _cancellationTokenSource;
                _cancellationTokenSource = null;
                
                // Cancel any pending operations
                if (cts != null)
                {
                    try
                    {
                        cts.Cancel();
                        cts.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error disposing CTS: {ex.Message}");
                    }
                }
                
                // Cleanup any typing messages
                _ = MainThread.InvokeOnMainThreadAsync(() => {
                    CleanupTypingMessages();
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during ViewModel cleanup: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Ensures the conversation has a proper title based on content
        /// </summary>
        private async Task EnsureConversationHasTitle()
        {
            if (CurrentConversation == null)
                return;
                
            // Check if title is default or empty
            if (string.IsNullOrWhiteSpace(CurrentConversation.Title) || 
                CurrentConversation.Title == "New Chat" || 
                CurrentConversation.Title == "Temporary Chat")
            {
                try
                {
                    // Find the first user message
                    var messages = await _messageRepository.GetByConversationIdAsync(CurrentConversation.Id, 5);
                    var firstUserMessage = messages.FirstOrDefault(m => !m.IsAI);
                    
                    if (firstUserMessage != null)
                    {
                        CurrentConversation.Title = GenerateTitle(firstUserMessage.Content);
                        await _conversationRepository.UpdateAsync(CurrentConversation);
                        Debug.WriteLine($"Updated conversation title to: {CurrentConversation.Title}");
                        OnPropertyChanged(nameof(CurrentConversation));
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error ensuring conversation title: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Opens a dialog to edit the conversation title
        /// </summary>
        private async Task EditConversationTitleAsync()
        {
            if (CurrentConversation == null)
                return;
                
            try
            {
                // Show prompt to edit title
                string result = await Shell.Current.DisplayPromptAsync(
                    "Edit Title", 
                    "Enter a new title for this conversation:",
                    initialValue: CurrentConversation.Title);
                    
                if (!string.IsNullOrEmpty(result) && result != CurrentConversation.Title)
                {
                    CurrentConversation.Title = result;
                    await _conversationRepository.UpdateAsync(CurrentConversation);
                    OnPropertyChanged(nameof(CurrentConversation));
                    Debug.WriteLine($"Updated conversation title to: {result}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error editing conversation title: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", "Failed to update conversation title", "OK");
            }
        }

        // Property change handlers to update computed properties
        partial void OnIsBusyChanged(bool value)
        {
            OnPropertyChanged(nameof(IsNotBusy));
            OnPropertyChanged(nameof(CanSendMessage));
            (SendMessageCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
        }
        
        partial void OnMessageTextChanged(string value)
        {
            OnPropertyChanged(nameof(CanSendMessage));
            (SendMessageCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
        }

        partial void OnMessagesChanged(ObservableCollection<Message> value)
        {
            // Force notification whenever the Messages collection changes
            OnPropertyChanged(nameof(Messages));
        }

        /// <summary>
        /// Remove any typing indicator messages from the collection (synchronous version)
        /// </summary>
        private void CleanupTypingMessages()
        {
            try
            {
                var typingMessages = Messages.Where(m => m?.Status == "Typing").ToList();
                foreach (var msg in typingMessages)
                {
                    Debug.WriteLine($"Removing typing message ID: {msg.Id}");
                    Messages.Remove(msg);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error cleaning up typing messages: {ex.Message}");
            }
        }

        /// <summary>
        /// Remove any typing indicator messages from the collection (async version)
        /// </summary>
        private async Task CleanupTypingMessagesAsync()
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(() => {
                    CleanupTypingMessages();
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CleanupTypingMessagesAsync: {ex.Message}");
            }
        }

        /// <summary>
        /// Cancels and disposes the cancellation token source
        /// </summary>
        private void CancelAndDisposeTokenSource()
        {
            var cts = Interlocked.Exchange(ref _cancellationTokenSource, null);
            if (cts != null)
            {
                try 
                {
                    cts.Cancel();
                    // Dispose on background thread after a short delay
                    Task.Delay(200).ContinueWith(_ => cts.Dispose());
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error disposing cancellation token: {ex.Message}");
                }
            }
        }
    }
}
