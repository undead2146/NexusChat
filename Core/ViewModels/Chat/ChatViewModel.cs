using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexusChat.Core.Models;
using NexusChat.Services.Interfaces;
using Microsoft.Maui.Controls;
using NexusChat.Data.Interfaces;
using System.Collections.Generic;
using System.Text;
using NexusChat.Helpers;

namespace NexusChat.Core.ViewModels
{
    /// <summary>
    /// ViewModel for the chat interaction page
    /// </summary>
    public partial class ChatViewModel : BaseViewModel
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IConversationRepository _conversationRepository;
        private readonly IAIModelRepository? _modelRepository;        
        
        private readonly IAIModelManager _AIModelManager;

        
        private readonly IChatService _chatService;
        private readonly INavigationService _navigationService;

        [ObservableProperty]
        private Conversation? _currentConversation;
        
        [ObservableProperty]
        private ObservableCollection<Message> _messages = new ObservableCollection<Message>();

        [ObservableProperty]
        private string _message = string.Empty;
        
        [ObservableProperty]
        private bool _isMessageSending;
        
        [ObservableProperty]
        private bool _isAITyping;
        
        [ObservableProperty]
        private bool _isThinking;
        
        [ObservableProperty]
        private bool _isLoadingMoreMessages;
        
        [ObservableProperty]
        private bool _hasConversation;
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsEmpty))]
        private bool _hasMessages;
        
        [ObservableProperty]
        private bool _showScrollToBottom;
        
        [ObservableProperty]
        private bool _showLoadMoreButton;
        
        [ObservableProperty]
        private bool _hasError;
        
        [ObservableProperty]
        private string _errorMessage = string.Empty;
        
        [ObservableProperty]
        private string _currentModelName = string.Empty;

        [ObservableProperty]
        private bool _enableStreaming = true;

        [ObservableProperty]
        private bool _hasMoreMessages;
        
        [ObservableProperty]
        private object? _scrollTarget;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsSidebarVisible))]
        private bool _isSidebarOpen;

        // Remove the separate IsSidebarVisible property and use IsSidebarOpen for both
        public bool IsSidebarVisible => IsSidebarOpen;
        
        public bool IsEmpty => !HasMessages;
        public bool IsNotLoadingMore => !IsLoadingMoreMessages;
        public bool CanSendMessage => !string.IsNullOrWhiteSpace(Message) && !IsMessageSending;
        
        private CancellationTokenSource _cts;
        private const int PageSize = 20;
        private int _currentOffset = 0;
        
        /// <summary>
        /// Creates a new instance of ChatViewModel
        /// </summary>
        public ChatViewModel(
            IMessageRepository messageRepository,
            IConversationRepository conversationRepository,
            IAIModelRepository? modelRepository,
            IAIModelManager AIModelManager,
            IChatService chatService,
            INavigationService navigationService)
        {
            _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
            _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
            _modelRepository = modelRepository;
            _AIModelManager = AIModelManager ?? throw new ArgumentNullException(nameof(AIModelManager));
            _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            
            Title = "Chat";
            _cts = new CancellationTokenSource();
            
            // Initialize default values for non-nullable properties
            _currentConversation = null;
            _errorMessage = string.Empty;
            _currentModelName = string.Empty;
            _scrollTarget = null;
        }
        
        /// <summary>
        /// Ensures a conversation exists, creating one if needed
        /// </summary>
        public async Task EnsureConversationExistsAsync()
        {
            if (CurrentConversation == null)
            {
                Debug.WriteLine("No conversation exists, creating new conversation");
                await InitializeNewConversationAsync();
            }
        }
        
        /// <summary>
        /// Initializes the ViewModel with the specified conversation ID
        /// </summary>
        public async Task InitializeAsync(int conversationId)
        {
            try
            {
                HasError = false;
                Debug.WriteLine($"Initializing ChatViewModel with conversation ID: {conversationId}");
                
                // Load conversation
                var conversation = await _conversationRepository.GetByIdAsync(conversationId);
                if (conversation == null)
                {
                    Debug.WriteLine($"Conversation not found: {conversationId}");
                    HasError = true;
                    ErrorMessage = "Conversation not found";
                    return;
                }
                
                Debug.WriteLine($"Loaded conversation: {conversation.Id}, Title: {conversation.Title ?? "null"}");
                
                CurrentConversation = conversation;
                HasConversation = true;
                
                // Load initial page of messages
                await LoadMessagesAsync();
                
                // Load current AI model
                await LoadCurrentModelAsync();
                
                Title = conversation.Title ?? "New Chat";
                
                // Ensure UI updates
                OnPropertyChanged(nameof(CurrentModelName));
                OnPropertyChanged(nameof(CurrentConversation));
                
                Debug.WriteLine($"Chat initialized with title: {Title}, model: {CurrentModelName}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing ChatViewModel: {ex.Message}");
                HasError = true;
                ErrorMessage = $"Error loading conversation: {ex.Message}";
            }
        }
        
        /// <summary>
        /// Creates a new conversation
        /// </summary>
        public async Task InitializeNewConversationAsync(string title = null)
        {
            try
            {
                HasError = false;
                
                string conversationTitle = title ?? "New Chat";
                Debug.WriteLine($"Creating new conversation with title: {conversationTitle}");
                
                // Create new conversation with minimal required fields only
                var conversation = new Conversation
                {
                    Title = conversationTitle,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    UserId = 1 // Default user ID
                };
                
                // Don't set ModelName or ProviderName until we're sure schema is updated
                
                try {
                    // Try to add the conversation
                    var id = await _conversationRepository.AddAsync(conversation);
                    conversation.Id = id;
                    
                    Debug.WriteLine($"Created conversation with ID: {id}");
                    
                    // Now we can safely set model info
                    if (_AIModelManager.CurrentModel != null)
                    {
                        conversation.ModelName = _AIModelManager.CurrentModel.ModelName;
                        conversation.ProviderName = _AIModelManager.CurrentModel.ProviderName;
                        await _conversationRepository.UpdateAsync(conversation, CancellationToken.None);
                    }
                }
                catch (Exception ex) {
                    Debug.WriteLine($"Error during conversation creation: {ex.Message}");
                    // If there was a schema error, try again with just required fields
                    if (ex.Message.Contains("no column named")) {
                        Debug.WriteLine("Trying again with minimal fields");
                        conversation.ModelName = null;
                        conversation.ProviderName = null;
                        var id = await _conversationRepository.AddAsync(conversation);
                        conversation.Id = id;
                    }
                    else {
                        throw; // Re-throw if it's not a schema issue
                    }
                }
                
                CurrentConversation = conversation;
                HasConversation = true;
                
                Messages.Clear();
                HasMessages = false;
                _currentOffset = 0;
                
                await LoadCurrentModelAsync();
                
                Title = conversation.Title;
                
                OnPropertyChanged(nameof(CurrentConversation));
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(CurrentModelName));
                OnPropertyChanged(nameof(HasConversation));
                
                Debug.WriteLine($"New chat created with ID: {conversation.Id}, title: {Title}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating new conversation: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                HasError = true;
                ErrorMessage = $"Error creating conversation: {ex.Message}";
            }
        }
        
        /// <summary>
        /// Loads messages for the current conversation
        /// </summary>
        private async Task LoadMessagesAsync(int offset = 0, bool isLoadingMore = false)
        {
            if (CurrentConversation == null) return;
            
            try
            {
                if (isLoadingMore)
                {
                    IsLoadingMoreMessages = true;
                }
                
                var messages = await _messageRepository.GetByConversationIdAsync(
                    CurrentConversation.Id, 
                    PageSize, 
                    offset);
                
                var totalCount = await _messageRepository.GetMessageCountForConversationAsync(CurrentConversation.Id);
                HasMoreMessages = totalCount > offset + messages.Count;
                ShowLoadMoreButton = HasMoreMessages;
                
                if (offset == 0)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        Messages.Clear();
                        foreach (var message in messages)
                        {
                            Messages.Add(message);
                        }
                        HasMessages = Messages.Count > 0;
                    });
                }
                else
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        for (int i = messages.Count - 1; i >= 0; i--)
                        {
                            Messages.Insert(0, messages[i]);
                        }
                        HasMessages = Messages.Count > 0;
                    });
                }
                
                _currentOffset = offset + messages.Count;
                
                if (messages.Count > 0 && CurrentConversation != null)
                {
                    CurrentConversation.UpdatedAt = DateTime.Now;
                    await _conversationRepository.UpdateAsync(CurrentConversation, CancellationToken.None);
                }
                
                if (offset == 0 && messages.Count > 0)
                {
                    ScrollTarget = Messages.Last();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading messages: {ex.Message}");
                HasError = true;
                ErrorMessage = $"Error loading messages: {ex.Message}";
            }
            finally
            {
                if (isLoadingMore)
                {
                    IsLoadingMoreMessages = false;
                }
            }
        }
        
        /// <summary>
        /// Sends the current message to the AI - With additional logging
        /// </summary>
        [RelayCommand]
        public async Task SendMessage()
        {
            Debug.WriteLine("SendMessage command executed");
            
            if (!CanSendMessage || string.IsNullOrWhiteSpace(Message))
            {
                Debug.WriteLine("Cannot send message: Either CanSendMessage is false or Message is empty");
                return;
            }
                
            try
            {
                Debug.WriteLine($"Sending message: {Message}");
                
                // Store the message locally to avoid issues if cleared during sending
                string messageText = Message.Trim();
                Message = string.Empty;
                
                // Create user message
                var userMessage = await _chatService.SendMessageAsync(CurrentConversation.Id, messageText);
                
                // Add to the messages collection
                await AddMessageToCollectionAsync(userMessage);
                
                // Set thinking status
                IsThinking = true;
                
                try
                {
                    // Get AI response
                    await _chatService.SendAIMessageAsync(CurrentConversation.Id, messageText, _cts.Token);
                    
                    // Refresh messages to get the AI response
                    await RefreshMessagesAsync();
                    
                    // Scroll to the bottom
                    await ScrollToBottomAsync();
                    
                    Debug.WriteLine("AI response received and displayed");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error getting AI response: {ex.Message}");
                    await Shell.Current.DisplayAlert("Error", "Failed to get AI response. Please try again.", "OK");
                }
                finally
                {
                    IsThinking = false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SendMessage: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Adds a message to the collection
        /// </summary>
        private async Task AddMessageToCollectionAsync(Message message)
        {
            if (message == null) return;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Messages.Add(message);
                HasMessages = Messages.Count > 0;
                ScrollTarget = message;
            });
        }

        /// <summary>
        /// Refreshes the messages from the database
        /// </summary>
        private async Task RefreshMessagesAsync()
        {
            if (CurrentConversation == null) return;

            try
            {
                // Get the latest messages
                var messages = await _messageRepository.GetByConversationIdAsync(
                    CurrentConversation.Id,
                    PageSize,
                    0);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Messages.Clear();
                    foreach (var message in messages)
                    {
                        Messages.Add(message);
                    }
                    
                    HasMessages = Messages.Count > 0;
                    _currentOffset = messages.Count;
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error refreshing messages: {ex.Message}");
            }
        }

        /// <summary>
        /// Scrolls to the bottom of the messages
        /// </summary>
        private async Task ScrollToBottomAsync()
        {
            if (Messages.Count > 0)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ScrollTarget = Messages.Last();
                    ShowScrollToBottom = false;
                });
            }
        }
        
        /// <summary>
        /// Regenerates the last AI message
        /// </summary>
        [RelayCommand]
        public async Task RegenerateMessage(Message message)
        {
            Debug.WriteLine("RegenerateMessage command executed");
            if (message == null || !message.IsAI || IsMessageSending)
                return;
                
            if (CurrentConversation == null)
            {
                Debug.WriteLine("No active conversation");
                return;
            }
            
            try
            {
                var messageIndex = Messages.IndexOf(message);
                int userMessageIndex = messageIndex - 1;
                
                if (userMessageIndex < 0 || userMessageIndex >= Messages.Count)
                {
                    Debug.WriteLine("No user message found to regenerate from");
                    return;
                }
                
                var userMessage = Messages[userMessageIndex];
                if (userMessage == null || userMessage.IsAI)
                {
                    Debug.WriteLine("Invalid user message found");
                    return;
                }
                
                IsMessageSending = true;
                IsThinking = true;
                
                await _messageRepository.DeleteAsync(message.Id, CancellationToken.None);
                Messages.Remove(message);
                
                var aiResponse = await _chatService.SendAIMessageAsync(
                    CurrentConversation.Id,
                    userMessage.Content, 
                    _cts.Token);
                
                if (aiResponse != null)
                {
                    var aiMessage = new Message
                    {
                        Content = aiResponse.Content,
                        IsAI = true,
                        ConversationId = CurrentConversation.Id,
                        SentAt = DateTime.Now,
                        Status = "Regenerated"
                    };
                    
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        Messages.Add(aiMessage);
                        ScrollTarget = aiMessage;
                    });
                    
                    aiMessage.Id = await _messageRepository.AddAsync(aiMessage);
                    
                    CurrentConversation.UpdatedAt = DateTime.Now;
                    await _conversationRepository.UpdateAsync(CurrentConversation, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in RegenerateMessage: {ex.Message}");
            }
            finally
            {
                IsMessageSending = false;
                IsThinking = false;
            }
        }
        
        /// <summary>
        /// Updates the status of a message
        /// </summary>
        private async Task UpdateMessageStatusAsync(Message message, string newStatus)
        {
            if (message == null) return;
            
            try
            {
                message.Status = newStatus;
                await _messageRepository.UpdateAsync(message, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating message status: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Edits the title of the current conversation
        /// </summary>
        [RelayCommand]
        public async Task EditTitle()
        {
            Debug.WriteLine("EditTitle command executed");
            
            await EnsureConversationExistsAsync();
            
            if (CurrentConversation == null)
            {
                Debug.WriteLine("Cannot edit title: No conversation");
                return;
            }
            
            try
            {
                string currentTitle = CurrentConversation.Title ?? "New Chat";
                
                string result = await Shell.Current.DisplayPromptAsync(
                    "Edit Title", 
                    "Enter a new title for this conversation",
                    initialValue: currentTitle);
                    
                if (!string.IsNullOrWhiteSpace(result))
                {
                    CurrentConversation.Title = result;
                    await _conversationRepository.UpdateAsync(CurrentConversation, CancellationToken.None);
                    OnPropertyChanged(nameof(CurrentConversation));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error editing title: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Loads more messages
        /// </summary>
        [RelayCommand]
        private async Task LoadMoreMessagesAsync()
        {
            if (IsLoadingMoreMessages || !HasMoreMessages) return;
            
            try
            {
                int newOffset = Math.Max(0, _currentOffset - PageSize);
                
                if (newOffset == _currentOffset) return;
                
                await LoadMessagesAsync(newOffset, true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading more messages: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Scrolls to the bottom of the message list
        /// </summary>
        [RelayCommand]
        public async Task ScrollToBottom()
        {
            Debug.WriteLine("ScrollToBottom command executed");
            if (Messages.Count > 0)
            {
                ScrollTarget = Messages.Last();
            }
        }
        
        /// <summary>
        /// Shows the options menu
        /// </summary>
        [RelayCommand]
        public async Task ShowMenu()
        {
            Debug.WriteLine("ShowMenu command executed");
            
            await EnsureConversationExistsAsync();
            
            if (CurrentConversation == null)
            {
                Debug.WriteLine("Cannot show menu: No conversation");
                return;
            }
            
            try
            {
                string result = await Shell.Current.DisplayActionSheet(
                    "Options",
                    "Cancel",
                    null,
                    "Clear Chat",
                    "Delete Conversation",
                    "Change AI Model");
                    
                switch (result)
                {
                    case "Clear Chat":
                        await ClearChatAsync();
                        break;
                    case "Delete Conversation":
                        await DeleteConversationAsync();
                        break;
                    case "Change AI Model":
                        await ChangeModel();
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing menu: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Clears all messages in the current chat
        /// </summary>
        private async Task ClearChatAsync()
        {
            if (CurrentConversation == null) return;
            
            bool confirm = await Shell.Current.DisplayAlert(
                "Clear Chat", 
                "Are you sure you want to delete all messages in this chat?", 
                "Clear", 
                "Cancel");
                
            if (confirm)
            {
                try
                {
                    await _messageRepository.DeleteByConversationIdAsync(CurrentConversation.Id, CancellationToken.None);
                    
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        Messages.Clear();
                        HasMessages = false;
                        _currentOffset = 0;
                        HasMoreMessages = false;
                        ShowLoadMoreButton = false;
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error clearing chat: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Deletes the current conversation
        /// </summary>
        private async Task DeleteConversationAsync()
        {
            if (CurrentConversation == null) return;
            
            bool confirm = await Shell.Current.DisplayAlert(
                "Delete Conversation", 
                "Are you sure you want to delete this conversation? This cannot be undone.", 
                "Delete", 
                "Cancel");
                
            if (confirm)
            {
                try
                {
                    await _conversationRepository.DeleteAsync(CurrentConversation.Id, CancellationToken.None);
                    
                    await GoBack();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error deleting conversation: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Changes the AI model for this chat by showing favorite models
        /// </summary>
        [RelayCommand]
        public async Task ChangeModel()
        {
            Debug.WriteLine("ChangeModel command executed");
            try
            {
                // Get favorite models from the model manager
                var favoriteModels = await _modelRepository.GetFavoriteModelsAsync();
                
                if (favoriteModels == null || !favoriteModels.Any())
                {
                    await Shell.Current.DisplayAlert("No Favorites", 
                        "You don't have any favorite models. Please go to Models page to set favorites.", 
                        "OK");
                    return;
                }
                
                // Create a list of model names for the action sheet
                var modelNames = favoriteModels.Select(m => $"{m.ProviderName}/{m.ModelName}").ToList();
                
                // Show action sheet to select model
                string result = await Shell.Current.DisplayActionSheet(
                    "Select Model", 
                    "Cancel", 
                    null,
                    modelNames.ToArray());
                    
                if (!string.IsNullOrWhiteSpace(result) && result != "Cancel")
                {
                    // Find the selected model
                    var parts = result.Split('/');
                    if (parts.Length == 2)
                    {
                        var providerName = parts[0];
                        var modelName = parts[1];
                        
                        var selectedModel = favoriteModels.FirstOrDefault(
                            m => m.ProviderName == providerName && m.ModelName == modelName);
                            
                        if (selectedModel != null)
                        {
                            // Update current conversation model
                            if (CurrentConversation != null)
                            {
                                CurrentConversation.ModelName = selectedModel.ModelName;
                                CurrentConversation.ProviderName = selectedModel.ProviderName;
                                await _conversationRepository.UpdateAsync(CurrentConversation, CancellationToken.None);
                            }
                            
                            // Update model manager
                            await _AIModelManager.SetCurrentModelAsync(selectedModel);
                            
                            // Update UI
                            CurrentModelName = selectedModel.ModelName;
                            
                            // Display confirmation
                            await Shell.Current.DisplayAlert("Model Changed", 
                                $"Now using {selectedModel.ProviderName}/{selectedModel.ModelName}", 
                                "OK");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error changing model: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                await Shell.Current.DisplayAlert("Error", "Failed to change model.", "OK");
            }
        }
        
        /// <summary>
        /// Toggles the sidebar open/closed
        /// </summary>
        [RelayCommand]
        public void ToggleSidebar()
        {
            Debug.WriteLine("ToggleSidebar command executed");
            IsSidebarOpen = !IsSidebarOpen;
            Debug.WriteLine($"Sidebar is now {(IsSidebarOpen ? "open" : "closed")}");
        }

        /// <summary>
        /// Navigates back to the previous page
        /// </summary>
        [RelayCommand]
        public async Task GoBack()
        {
            Debug.WriteLine("GoBack command executed");
            try
            {
                if (_cts != null && !_cts.IsCancellationRequested)
                {
                    _cts.Cancel();
                }
                
                try
                {
                    // First try direct navigation
                    await Shell.Current.Navigation.PopAsync();
                    Debug.WriteLine("Navigation.PopAsync succeeded");
                }
                catch (Exception ex) 
                {
                    Debug.WriteLine($"Navigation.PopAsync failed: {ex.Message}, trying GoToAsync");
                    // Fallback to GoToAsync
                    await Shell.Current.GoToAsync("..");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"All navigation methods failed: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// Handle visibility changes and housekeeping
        /// </summary>
        public void OnAppearing()
        {
            _cts = new CancellationTokenSource();
        }
        
        /// <summary>
        /// Cleanup when page disappears
        /// </summary>
        public void OnDisappearing()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        /// <summary>
        /// Loads the current AI model
        /// </summary>
        private async Task LoadCurrentModelAsync()
        {
            await Task.Delay(1); // Make method truly async
            
            try
            {
                var currentModel = _AIModelManager.CurrentModel;
                if (currentModel != null)
                {
                    CurrentModelName = currentModel.ModelName ?? "Default Model";
                    Debug.WriteLine($"Current model loaded: {CurrentModelName}");
                }
                else
                {
                    CurrentModelName = "Default Model";
                    Debug.WriteLine("No model found, using default");
                }
                
                OnPropertyChanged(nameof(CurrentModelName));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading current model: {ex.Message}");
                CurrentModelName = "Unknown Model";
            }
        }

        /// <summary>
        /// Loads a specific conversation
        /// </summary>
        public void LoadConversation(Conversation conversation)
        {
            try
            {
                CurrentConversation = conversation;
                Title = conversation.Title;
                
                // Load messages for this conversation
                LoadMessagesForConversation(conversation.Id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading conversation: {ex.Message}");
            }
        }

        /// <summary>
        /// Clears the current conversation
        /// </summary>
        public void ClearCurrentConversation()
        {
            CurrentConversation = null;
            Messages.Clear();
            Title = "Chat";
            HasMessages = false;
            HasConversation = false;
        }

        /// <summary>
        /// Loads messages for a specific conversation
        /// </summary>
        private async void LoadMessagesForConversation(int conversationId)
        {
            try
            {
                var conversationMessages = await _chatService.GetMessagesAsync(conversationId);
                
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Messages.Clear();
                    foreach (var msg in conversationMessages)
                    {
                        Messages.Add(msg);
                    }
                    HasMessages = Messages.Count > 0;
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading messages: {ex.Message}");
            }
        }
    }
}
