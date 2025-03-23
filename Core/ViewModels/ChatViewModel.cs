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
            EditTitleCommand = new AsyncRelayCommand(EditConversationTitleAsync); // Add the edit title command
            
            // Create dummy AI model for testing
            CurrentAIModel = new AIModel
            {
                Id = 1,
                ModelName = "GPT-4 Turbo",
                ProviderName = "OpenAI",
                IsAvailable = true,
                MaxTokens = 4096,
                DefaultTemperature = 0.7f  // Add F suffix to fix the float error
            };
            
            // Initialize cancellation token source
            _cancellationTokenSource = new CancellationTokenSource();
        }
        
        /// <summary>
        /// Initialize the ViewModel
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                IsBusy = true;
                
                // Ensure database is initialized first
                await Task.Run(async () => {
                    try 
                    {
                        await _messageRepository.EnsureDatabaseAsync();
                        Debug.WriteLine("Database initialized successfully");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error initializing database: {ex.Message}");
                        StatusMessage = "Error initializing database";
                    }
                });
                
                // Typically this would load the conversation ID from navigation parameters
                // For now, we'll just load the most recent conversation or create a new one
                if (CurrentConversation == null)
                {
                    int conversationId = 1; // Default for testing
                    
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
                
                // Load messages for the conversation
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
        /// Send a message to the AI
        /// </summary>
        private async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(MessageText) || IsBusy)
                return;
            
            // Cancel any ongoing AI operations
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            
            try
            {
                IsBusy = true;
                
                // Ensure we have a valid conversation
                if (CurrentConversation == null || CurrentConversation.Id <= 0)
                {
                    await InitializeAsync();
                    if (CurrentConversation == null || CurrentConversation.Id <= 0)
                    {
                        throw new InvalidOperationException("Failed to create conversation");
                    }
                }
                
                // Get and validate the user's message text
                string userMessageText = MessageText?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(userMessageText))
                {
                    Debug.WriteLine("Empty message text, not sending");
                    return;
                }
                
                // Clear the input field immediately for better UX
                MessageText = string.Empty;
                
                // Create user message with proper validation
                var userMessage = new Message
                {
                    ConversationId = CurrentConversation.Id,
                    Content = userMessageText,
                    IsAI = false,
                    Timestamp = DateTime.Now,
                    Status = "Sent",
                    IsNew = true
                };
                
                // Log message for debugging
                Debug.WriteLine($"Adding user message: {userMessage.Content}");
                
                // Important: Add synchronously to collection on UI thread
                // FIXED: Use the correct method InvokeOnMainThreadAsync instead of BeginInvokeOnMainThreadAsync
                await MainThread.InvokeOnMainThreadAsync(() => 
                {
                    // Add to collection
                    Messages.Add(userMessage);
                    
                    // Force notification of collection change
                    OnPropertyChanged(nameof(Messages));
                    
                    Debug.WriteLine("User message added to collection - sending notification");
                });
                
                // Save to database - do this after UI update
                try
                {
                    var savedMessage = await _messageRepository.AddAsync(userMessage);
                    userMessage.Id = savedMessage.Id;
                    Debug.WriteLine($"Saved user message with ID: {userMessage.Id}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error saving user message: {ex.Message}");
                }
                
                // Show AI typing indicator
                IsAITyping = true;
                
                try
                {
                    // Get AI response with proper error handling
                    string aiResponseText = await _aiService.SendMessageAsync(
                        userMessageText, 
                        _cancellationTokenSource.Token);
                    
                    // Validate AI response
                    if (string.IsNullOrWhiteSpace(aiResponseText))
                    {
                        throw new Exception("AI returned empty response");
                    }
                    
                    Debug.WriteLine($"Received AI response: {aiResponseText.Substring(0, Math.Min(50, aiResponseText.Length))}...");
                    
                    // Hide typing indicator before adding message
                    IsAITyping = false;
                    
                    // Create AI response message
                    var aiResponse = new Message
                    {
                        ConversationId = CurrentConversation.Id,
                        Content = aiResponseText,
                        IsAI = true,
                        Timestamp = DateTime.Now,
                        TokensUsed = Message.EstimateTokens(aiResponseText),
                        Status = "Delivered",
                        IsNew = true
                    };
                    
                    // Important: Add synchronously to collection on UI thread
                    // FIXED: Use the correct method InvokeOnMainThreadAsync instead of BeginInvokeOnMainThreadAsync
                    await MainThread.InvokeOnMainThreadAsync(() => 
                    {
                        // Add to collection
                        Messages.Add(aiResponse);
                        
                        // Force notification of collection change
                        OnPropertyChanged(nameof(Messages));
                        
                        Debug.WriteLine("AI message added to UI collection - sending notification");
                    });
                    
                    // Save to database - do this after UI update
                    try
                    {
                        var savedAiMessage = await _messageRepository.AddAsync(aiResponse);
                        aiResponse.Id = savedAiMessage.Id;
                        
                        // Update conversation
                        CurrentConversation.UpdatedAt = DateTime.Now;
                        CurrentConversation.TotalTokensUsed += aiResponse.TokensUsed;
                        
                        if (string.IsNullOrEmpty(CurrentConversation.Title) || 
                            CurrentConversation.Title == "New Chat" ||
                            CurrentConversation.Title == "Temporary Chat")
                        {
                            // Generate title from first message
                            CurrentConversation.Title = GenerateTitle(userMessageText);
                            OnPropertyChanged(nameof(CurrentConversation));
                        }
                        
                        await _conversationRepository.UpdateAsync(CurrentConversation);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error saving AI message: {ex.Message}");
                    }
                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine("AI request was canceled");
                    IsAITyping = false;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error getting AI response: {ex.Message}");
                    
                    // Hide typing indicator
                    IsAITyping = false;
                    
                    // Add error message to UI
                    var errorMessage = new Message
                    {
                        ConversationId = CurrentConversation.Id,
                        Content = "Sorry, I encountered an error processing your request. Please try again.",
                        IsAI = true,
                        Timestamp = DateTime.Now,
                        Status = "Error"
                    };
                    
                    await MainThread.InvokeOnMainThreadAsync(() => 
                    {
                        Messages.Add(errorMessage);
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SendMessageAsync: {ex.Message}");
                StatusMessage = $"Error: {ex.Message}";
                
                // Hide typing indicator if it's showing
                IsAITyping = false;
            }
            finally
            {
                IsBusy = false;
                // Update can send message state
                (SendMessageCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            }
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
                
                var messages = await _messageRepository.GetByConversationIdAsync(CurrentConversation.Id);
                Messages.Clear();
                
                foreach (var message in messages)
                {
                    Messages.Add(message);
                }
                
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
                
            bool confirm = await Shell.Current.DisplayAlert(
                "Clear Conversation",
                "Are you sure you want to clear all messages? This cannot be undone.",
                "Clear",
                "Cancel");
                
            if (!confirm)
                return;
                
            try
            {
                IsBusy = true;
                
                await _messageRepository.DeleteByConversationIdAsync(CurrentConversation.Id);
                Messages.Clear();
                
                // Reset conversation properties
                CurrentConversation.Title = "New Chat";
                CurrentConversation.UpdatedAt = DateTime.Now;
                CurrentConversation.TotalTokensUsed = 0;
                
                await _conversationRepository.UpdateAsync(CurrentConversation);
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
            // Implementation would depend on platform-specific sharing capabilities
            await Shell.Current.DisplayAlert("Share", "Sharing functionality not implemented", "OK");
        }
        
        /// <summary>
        /// Delete the conversation
        /// </summary>
        private async Task DeleteConversationAsync()
        {
            if (CurrentConversation == null)
                return;
                
            bool confirm = await Shell.Current.DisplayAlert(
                "Delete Conversation",
                "Are you sure you want to delete this conversation? This cannot be undone.",
                "Delete",
                "Cancel");
                
            if (!confirm)
                return;
                
            try
            {
                IsBusy = true;
                
                // Delete all messages first
                await _messageRepository.DeleteByConversationIdAsync(CurrentConversation.Id);
                
                // Delete the conversation
                await _conversationRepository.DeleteAsync(CurrentConversation.Id);
                
                // Navigate back
                await GoBackAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting conversation: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", "Failed to delete conversation", "OK");
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
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
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
    }
}
