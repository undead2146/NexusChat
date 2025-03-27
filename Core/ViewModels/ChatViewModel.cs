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
using NexusChat.Helpers;
using NexusChat.Services;
using NexusChat.Services.Interfaces;
using Microsoft.Maui.Controls;

namespace NexusChat.Core.ViewModels
{
    /// <summary>
    /// ViewModel for the chat interaction page
    /// </summary>
    public partial class ChatViewModel : ObservableObject
    {
        private readonly ChatService _chatService;
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
        
        // Pagination properties
        [ObservableProperty]
        private bool _isLoadingMore;
        
        [ObservableProperty]
        private bool _canLoadMore = true;
        
        private int _pageSize = 5;
        private int _currentOffset = 0;
        
        /// <summary>
        /// Gets if the ViewModel is not busy
        /// </summary>
        public bool IsNotBusy => !IsBusy;
        
        /// <summary>
        /// Gets if the user can send a message
        /// </summary>
        public bool CanSendMessage => !string.IsNullOrWhiteSpace(MessageText) && !IsBusy;

        /// <summary>
        /// Gets whether the load more button should be shown (available and not currently loading)
        /// </summary>
        public bool ShowLoadMoreButton => CanLoadMore && !IsLoadingMore;

        #region Commands
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
        /// Command to load more messages
        /// </summary>
        public IAsyncRelayCommand LoadMoreMessagesCommand { get; }
        #endregion

        /// <summary>
        /// Initialize a new instance of ChatViewModel
        /// </summary>
        public ChatViewModel(IMessageRepository messageRepository, IConversationRepository conversationRepository, IAIService aiService)
        {
            // Create ChatService with dependencies
            _chatService = new ChatService(messageRepository, conversationRepository, aiService);
            
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
            LoadMoreMessagesCommand = new AsyncRelayCommand(LoadMoreMessagesAsync, () => CanLoadMore && !IsLoadingMore);
            
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
                
                // Typically this would load the conversation ID from navigation parameters
                if (CurrentConversation == null)
                {
                    int conversationId = 1; // Default for testing
                    CurrentConversation = await _chatService.LoadOrCreateConversationAsync(conversationId, CurrentAIModel?.Id ?? 1);
                }
                
                // Reset pagination state
                _currentOffset = 0;
                
                // Explicitly set to true initially so button is clickable
                CanLoadMore = true;
                OnPropertyChanged(nameof(ShowLoadMoreButton));
                
                // Load initial set of messages asynchronously
                await LoadInitialMessagesAsync();
                
                // Generate title if it's still the default - look at first user message
                await EnsureConversationHasTitleAsync();
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
        /// Loads the initial set of messages (most recent ones)
        /// </summary>
        private async Task LoadInitialMessagesAsync()
        {
            try
            {
                if (CurrentConversation?.Id <= 0) return;
                
                // Set loading flag to avoid double-loading
                IsLoadingMore = true;
                
                try
                {
                    // Get message count first to know if more can be loaded
                    int totalMessageCount = await _chatService.GetMessageCountAsync(CurrentConversation.Id);
                    
                    // Load initial set of messages from the end of the list
                    var initialMessages = await _chatService.GetMessageAsync(
                        CurrentConversation.Id, 
                        _pageSize,
                        Math.Max(0, totalMessageCount - _pageSize)); // Offset from end
                    
                    // Update UI on main thread
                    await MainThread.InvokeOnMainThreadAsync(() => 
                    {
                        Messages.Clear();
                        foreach (var message in initialMessages)
                        {
                            Messages.Add(message);
                        }
                    });
                    
                    // Update pagination state
                    _currentOffset = Math.Max(0, totalMessageCount - initialMessages.Count);
                    CanLoadMore = _currentOffset > 0;
                    
                    // Only notify command can execute changed if it exists
                    if (LoadMoreMessagesCommand is AsyncRelayCommand cmd)
                    {
                        cmd.NotifyCanExecuteChanged();

                    }
                    
                    Debug.WriteLine($"Loaded {initialMessages.Count} initial messages. More available? {CanLoadMore}, offset={_currentOffset}");
                    
                    // Update status message to inform the user if there are more messages
                    if (CanLoadMore)
                    {
                        StatusMessage = $"{_currentOffset} more messages available";
                        await Task.Delay(2000); // Show the status briefly
                        StatusMessage = string.Empty;
                    }
                }
                finally
                {
                    // Always reset loading state
                    IsLoadingMore = false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading initial messages: {ex.Message}");
                StatusMessage = "Error loading messages";
                IsLoadingMore = false;
            }
        }
        
        /// <summary>
        /// Loads more messages (older ones) when scrolling up
        /// </summary>
        private async Task LoadMoreMessagesAsync()
        {
            if (IsLoadingMore || !CanLoadMore || CurrentConversation?.Id <= 0)
                return;
            
            try
            {
                IsLoadingMore = true;
                StatusMessage = "Loading more messages...";
                
                // Add brief delay to allow UI to show loading indicator
                await Task.Delay(200);
                
                // Calculate how many more messages to load
                int offset = Math.Max(0, _currentOffset - _pageSize);
                int limit = _currentOffset - offset;
                
                Debug.WriteLine($"Loading more messages: offset={offset}, limit={limit}, current offset={_currentOffset}");
                
                // Fetch older messages
                var olderMessages = await _chatService.GetMessageAsync(
                    CurrentConversation.Id,
                    limit,
                    offset);
                
                if (olderMessages.Count > 0)
                {
                    // Save the height of the first visible message to restore scroll position
                    int firstVisibleIndex = 0; // Will be set by the View
                    
                    // Add older messages at the beginning to preserve order
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        // Insert at beginning (maintaining chronological order)
                        for (int i = olderMessages.Count - 1; i >= 0; i--)
                        {
                            Messages.Insert(0, olderMessages[i]);
                        }
                    });
                    
                    // Update pagination state
                    _currentOffset = offset;
                    CanLoadMore = _currentOffset > 0;
                    
                    // Notify UI about the scroll position (View will handle this)
                    StatusMessage = $"Loaded {olderMessages.Count} more messages";
                    
                    // Add info about remaining messages
                    if (CanLoadMore)
                    {
                        StatusMessage += $" ({_currentOffset} more available)";
                    }
                }
                else
                {
                    CanLoadMore = false;
                    StatusMessage = "No more messages to load";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading more messages: {ex.Message}");
                StatusMessage = "Failed to load more messages";
            }
            finally
            {
                await Task.Delay(300); // Small delay to prevent rapid firing
                IsLoadingMore = false;
                (LoadMoreMessagesCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            }
        }
        
        /// <summary>
        /// Refresh messages from the repository
        /// </summary>
        private async Task RefreshMessagesAsync()
        {
            if (CurrentConversation == null || CurrentConversation.Id <= 0)
                return;
                
            try
            {
                IsBusy = true;
                
                // Get total message count
                int totalMessageCount = await _chatService.GetMessageCountAsync(CurrentConversation.Id);
                
                // Load most recent messages
                var messages = await _chatService.GetMessageAsync(
                    CurrentConversation.Id,
                    _pageSize,
                    Math.Max(0, totalMessageCount - _pageSize));
                
                // Update UI on main thread
                await MainThread.InvokeOnMainThreadAsync(() => 
                {
                    Messages.Clear();
                    foreach (var message in messages)
                    {
                        Messages.Add(message);
                    }
                });
                
                // Update pagination state
                _currentOffset = totalMessageCount - messages.Count;
                CanLoadMore = _currentOffset > 0;
                (LoadMoreMessagesCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
                
                // Ensure title is set properly
                await EnsureConversationHasTitleAsync();
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
        /// Clean up empty messages
        /// </summary>
        public void CleanupEmptyMessages() => MessageOperations.CleanupEmptyMessages(Messages);
        
        /// <summary>
        /// Navigate back
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
                
                // First clear UI immediately
                await MainThread.InvokeOnMainThreadAsync(() => Messages.Clear());
                
                // Then try to clear from database
                if (CurrentConversation.Id > 0)
                {
                    try
                    {
                        // Delete messages
                        int rowsDeleted = await _chatService.ClearConversationMessagesAsync(CurrentConversation.Id);
                        Debug.WriteLine($"Cleared {rowsDeleted} messages from database");
                        
                        // Reset conversation properties
                        CurrentConversation.Title = "New Chat";
                        CurrentConversation.UpdatedAt = DateTime.Now;
                        CurrentConversation.TotalTokensUsed = 0;
                        
                        // Update conversation
                        await _chatService.UpdateConversationAsync(CurrentConversation);
                        OnPropertyChanged(nameof(CurrentConversation));
                    }
                    catch (Exception dbEx)
                    {
                        Debug.WriteLine($"Error clearing messages in database: {dbEx.Message}");
                        await Shell.Current.DisplayAlert("Warning", 
                            "Messages were cleared from view but there was a problem updating the database.", 
                            "OK");
                    }
                }
                
                StatusMessage = "Conversation cleared";
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
                
                // Clear UI first for immediate feedback
                await MainThread.InvokeOnMainThreadAsync(() => Messages.Clear());
                
                // Delete from database
                if (CurrentConversation.Id > 0)
                {
                    try
                    {
                        // Use ChatService to delete
                        bool success = await _chatService.DeleteConversationAsync(CurrentConversation.Id);
                        if (!success)
                        {
                            Debug.WriteLine("Failed to delete conversation from database");
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
        private void OnEntryFocused() => ShowScrollToBottom = false;
        
        /// <summary>
        /// Scroll to bottom of the message list
        /// </summary>
        private async Task ScrollToBottomAsync()
        {
            ShowScrollToBottom = false;
            await Task.Delay(10); // Small delay for UI update
        }
        
        /// <summary>
        /// Change AI model
        /// </summary>
        private async Task ChangeModelAsync()
        {
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
        /// Makes sure the conversation has a meaningful title
        /// </summary>
        private async Task EnsureConversationHasTitleAsync()
        {
            if (CurrentConversation == null)
                return;
                
            if (string.IsNullOrWhiteSpace(CurrentConversation.Title) || 
                CurrentConversation.Title == "New Chat" || 
                CurrentConversation.Title == "Temporary Chat")
            {
                try
                {
                    // Find first user message
                    var messages = await _chatService.GetMessagesAsync(CurrentConversation.Id, 5);
                    var firstUserMessage = messages.FirstOrDefault(m => !m.IsAI);
                    
                    if (firstUserMessage != null)
                    {
                        CurrentConversation.Title = _chatService.GenerateTitleFromMessage(firstUserMessage.Content);
                        await _chatService.UpdateConversationAsync(CurrentConversation);
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
        /// Edit conversation title
        /// </summary>
        private async Task EditConversationTitleAsync()
        {
            if (CurrentConversation == null)
                return;
                
            try
            {
                string result = await Shell.Current.DisplayPromptAsync(
                    "Edit Title", 
                    "Enter a new title for this conversation:",
                    initialValue: CurrentConversation.Title);
                    
                if (!string.IsNullOrEmpty(result) && result != CurrentConversation.Title)
                {
                    CurrentConversation.Title = result;
                    await _chatService.UpdateConversationAsync(CurrentConversation);
                    OnPropertyChanged(nameof(CurrentConversation));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error editing conversation title: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", "Failed to update conversation title", "OK");
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
                // Cancel ongoing operations
                CancelAndDisposeTokenSource();
                
                // Clean up UI elements
                _ = MainThread.InvokeOnMainThreadAsync(() => {
                    MessageOperations.CleanupTypingMessages(Messages);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during ViewModel cleanup: {ex.Message}");
            }
        }
        
        // Property change handlers
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
        
        partial void OnCanLoadMoreChanged(bool value)
        {
            OnPropertyChanged(nameof(ShowLoadMoreButton));

            // Also update the command's CanExecute state
            if (LoadMoreMessagesCommand is AsyncRelayCommand cmd)
            {
                cmd.NotifyCanExecuteChanged();
            }
        }

        partial void OnIsLoadingMoreChanged(bool value)
        {
            OnPropertyChanged(nameof(ShowLoadMoreButton));
            
            // Also update the command's CanExecute state
            if (LoadMoreMessagesCommand is AsyncRelayCommand cmd)
            {
                cmd.NotifyCanExecuteChanged();
            }
        }

        partial void OnMessagesChanged(ObservableCollection<Message> value)
        {
            // Force notification whenever the Messages collection changes
            OnPropertyChanged(nameof(Messages));
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
        
        /// <summary>
        /// Gets the total count of messages in the conversation
        /// </summary>
        public async Task<int> GetMessageCountAsync()
        {
            if (CurrentConversation?.Id <= 0)
                return 0;
                
            try
            {
                return await _chatService.GetMessageCountAsync(CurrentConversation.Id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting message count: {ex.Message}");
                return 0;
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
            
            // Grab message text and clear input field for immediate feedback
            string userMessageText = MessageText?.Trim() ?? "";
            MessageText = string.Empty;
            
            // Create user message
            var userMessage = MessageOperations.CreateUserMessage(
                userMessageText, 
                CurrentConversation?.Id ?? 0);
            
            // Add user message to UI collection immediately
            Messages.Add(userMessage);
            
            // New message sent means there's one more message
            _currentOffset++;
            
            // Mark busy state AFTER adding the user message
            IsBusy = true;
            
            // Start AI response process in the background
            _ = Task.Run(async () => 
            {
                try
                {
                    // Set up cancellation
                    CancelAndDisposeTokenSource();
                    _cancellationTokenSource = new CancellationTokenSource();
                    var cancellationToken = _cancellationTokenSource.Token;
                    
                    // Clean up any existing typing indicators
                    await MessageOperations.CleanupTypingMessagesAsync(Messages);
                    
                    // Set typing indicator
                    await MainThread.InvokeOnMainThreadAsync(() => IsAITyping = true);
                    
                    // Add typing indicator message
                    var typingMessage = MessageOperations.CreateTypingMessage(
                        CurrentConversation?.Id ?? 0,
                        userMessage.Id);
                    
                    await MainThread.InvokeOnMainThreadAsync(() => Messages.Add(typingMessage));
                    
                    // Save user message to database (fire and forget)
                    _ = Task.Run(async () => 
                    {
                        if (CurrentConversation?.Id > 0)
                        {
                            await _chatService.SaveMessageAsync(userMessage);
                        }
                    });
                    
                    // Get AI response
                    string aiResponseText = await _chatService.GetAIResponseAsync(
                        userMessageText, 
                        cancellationToken);
                    
                    // Remove typing indicator
                    await MainThread.InvokeOnMainThreadAsync(() => 
                    {
                        MessageOperations.CleanupTypingMessages(Messages);
                        IsAITyping = false;
                    });
                    
                    if (!string.IsNullOrEmpty(aiResponseText))
                    {
                        // Create and add AI message
                        var aiResponse = MessageOperations.CreateAIMessage(
                            aiResponseText,
                            CurrentConversation?.Id ?? 0,
                            userMessage.Id);
                        
                        await MainThread.InvokeOnMainThreadAsync(() => Messages.Add(aiResponse));
                        
                        // Save AI message and update conversation (fire and forget)
                        _ = Task.Run(async () => 
                        {
                            try 
                            {
                                if (CurrentConversation?.Id > 0)
                                {
                                    await _chatService.SaveMessageAsync(aiResponse);
                                    
                                    CurrentConversation.UpdatedAt = DateTime.Now;
                                    CurrentConversation.TotalTokensUsed += aiResponse.TokensUsed;
                                    
                                    // Update title if needed
                                    if (string.IsNullOrEmpty(CurrentConversation.Title) || 
                                        CurrentConversation.Title == "New Chat" ||
                                        CurrentConversation.Title == "Temporary Chat")
                                    {
                                        CurrentConversation.Title = _chatService.GenerateTitleFromMessage(userMessageText);
                                        await MainThread.InvokeOnMainThreadAsync(() => 
                                            OnPropertyChanged(nameof(CurrentConversation)));
                                    }
                                    
                                    await _chatService.UpdateConversationAsync(CurrentConversation);
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
                        // Add error message if no response
                        var errorMessage = MessageOperations.CreateErrorMessage(
                            CurrentConversation?.Id ?? 0,
                            userMessage.Id);
                        
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
    }
}
