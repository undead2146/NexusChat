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
        private readonly IAIProviderFactory _providerFactory;

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
            INavigationService navigationService,
            IAIProviderFactory providerFactory)
        {
            _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
            _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
            _modelRepository = modelRepository;
            _AIModelManager = AIModelManager ?? throw new ArgumentNullException(nameof(AIModelManager));
            _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
            
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
                
                // Load current AI model first
                await LoadCurrentModelAsync();
                
                Title = conversation.Title ?? "New Chat";
                
                // Clear messages first to show empty state immediately
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Messages.Clear();
                    HasMessages = false;
                    OnPropertyChanged(nameof(Messages));
                    OnPropertyChanged(nameof(HasMessages));
                });
                
                // Ensure UI updates
                OnPropertyChanged(nameof(CurrentModelName));
                OnPropertyChanged(nameof(CurrentConversation));
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(HasConversation));
                
                Debug.WriteLine($"Chat initialized with title: {Title}, model: {CurrentModelName}");
                
                // Load messages progressively in background after UI is ready
                _ = Task.Run(async () => await LoadMessagesProgressively(conversationId));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing ChatViewModel: {ex.Message}");
                HasError = true;
                ErrorMessage = $"Error loading conversation: {ex.Message}";
            }
        }
        
        /// <summary>
        /// Loads messages progressively to avoid UI lag
        /// </summary>
        private async Task LoadMessagesProgressively(int conversationId)
        {
            try
            {
                Debug.WriteLine($"Starting progressive message loading for conversation {conversationId}");
                
                var allMessages = await _messageRepository.GetByConversationIdAsync(conversationId, 100, 0);
                
                if (allMessages.Count == 0)
                {
                    Debug.WriteLine("No messages found for conversation");
                    return;
                }
                
                Debug.WriteLine($"Found {allMessages.Count} messages to load progressively");
                
                // Load messages one by one with longer delays for visual effect
                const int delayMs = 200; // Increased delay to make progression more visible
                
                foreach (var message in allMessages)
                {
                    // Ensure loaded messages have proper status
                    if (message.IsAI && string.IsNullOrEmpty(message.Status))
                    {
                        message.Status = "complete";
                    }
                    else if (!message.IsAI && string.IsNullOrEmpty(message.Status))
                    {
                        message.Status = "sent";
                    }
                    
                    // Add message individually to UI thread
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        Messages.Add(message);
                        HasMessages = Messages.Count > 0;
                        
                        // Force immediate UI update
                        OnPropertyChanged(nameof(Messages));
                        OnPropertyChanged(nameof(HasMessages));
                        
                        // Scroll to the newly added message
                        ScrollTarget = message;
                        OnPropertyChanged(nameof(ScrollTarget));
                        
                        Debug.WriteLine($"Progressively loaded message: '{message.Content?.Substring(0, Math.Min(50, message.Content?.Length ?? 0))}...', IsAI: {message.IsAI}");
                    });
                    
                    // Force a layout update
                    await Task.Delay(10);
                    
                    // Delay before next message (except for the last one)
                    if (allMessages.IndexOf(message) < allMessages.Count - 1)
                    {
                        await Task.Delay(delayMs);
                    }
                }
                
                Debug.WriteLine($"Progressive loading completed. Total messages loaded: {Messages.Count}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in progressive message loading: {ex.Message}");
                
                // Fallback to regular loading if progressive loading fails
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await LoadMessagesAsync();
                });
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
        /// Sends the current message to the AI
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

            await EnsureConversationExistsAsync();
            
            if (CurrentConversation == null)
            {
                Debug.WriteLine("No conversation available after ensuring conversation exists");
                return;
            }
                
            try
            {
                IsMessageSending = true;
                Debug.WriteLine($"Sending message: {Message}");
                
                // Store the message text and clear input 
                string messageText = Message.Trim();
                Message = string.Empty; // Clear input field instantly
                
                // Create user message
                var userMessage = new Message
                {
                    ConversationId = CurrentConversation.Id,
                    Content = messageText,
                    AuthorType = "user",
                    IsUserMessage = true,
                    Status = "sent",
                    SentAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Timestamp = DateTime.UtcNow
                };
                
                // Add user message to UI 
                Messages.Add(userMessage);
                HasMessages = Messages.Count > 0;
                ScrollTarget = userMessage;
                Debug.WriteLine($"Added user message to UI instantly: '{userMessage.Content}', IsAI: {userMessage.IsAI}");
                
                // Force immediate property notifications
                OnPropertyChanged(nameof(Messages));
                OnPropertyChanged(nameof(HasMessages));
                OnPropertyChanged(nameof(ScrollTarget));
                
                // Update conversation title if needed (also instant)
                if (CurrentConversation.Title == "New Chat" && !string.IsNullOrWhiteSpace(messageText))
                {
                    string newTitle = messageText.Length > 30 ? messageText.Substring(0, 30).Trim() + "..." : messageText.Trim();
                    CurrentConversation.Title = newTitle;
                    Title = newTitle;
                    
                    OnPropertyChanged(nameof(Title));
                    OnPropertyChanged(nameof(CurrentConversation));
                    Debug.WriteLine($"Updated conversation title instantly to: {newTitle}");
                }
                
                // Create AI message with thinking state
                var aiMessage = new Message
                {
                    ConversationId = CurrentConversation.Id,
                    Content = "",
                    AuthorType = "ai",
                    IsUserMessage = false,
                    Status = "thinking",
                    SentAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Timestamp = DateTime.UtcNow,
                    ModelName = _AIModelManager.CurrentModel?.ModelName ?? "",
                    ProviderName = _AIModelManager.CurrentModel?.ProviderName ?? ""
                };
                
                // Add thinking AI message instantly
                Messages.Add(aiMessage);
                HasMessages = Messages.Count > 0;
                ScrollTarget = aiMessage;
                IsThinking = true;
                Debug.WriteLine($"Added thinking AI message to UI: Status='{aiMessage.Status}'");
                
                OnPropertyChanged(nameof(Messages));
                OnPropertyChanged(nameof(HasMessages));
                OnPropertyChanged(nameof(ScrollTarget));
                
                // Now handle all background operations without blocking UI
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Save user message to database
                        try
                        {
                            userMessage.Id = await _messageRepository.AddAsync(userMessage);
                            Debug.WriteLine($"Saved user message to database: ID={userMessage.Id}");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error saving user message to database: {ex.Message}");
                        }
                        
                        // Get AI response
                        try
                        {
                            var aiService = await GetCurrentAIServiceAsync();
                            if (aiService == null)
                            {
                                aiMessage.Content = "No AI service is configured. Please select a model.";
                                aiMessage.Status = "error";
                            }
                            else
                            {
                                string aiResponse = await aiService.SendMessageAsync(messageText, _cts.Token);
                                aiMessage.Content = aiResponse;
                                aiMessage.Status = "complete";
                            }
                            
                            // Update AI message in UI
                            await MainThread.InvokeOnMainThreadAsync(() =>
                            {
                                var index = Messages.IndexOf(aiMessage);
                                if (index >= 0)
                                {
                                    Messages.RemoveAt(index);
                                    Messages.Insert(index, aiMessage);
                                    ScrollTarget = aiMessage;
                                    
                                    OnPropertyChanged(nameof(Messages));
                                    OnPropertyChanged(nameof(ScrollTarget));
                                }
                                
                                Debug.WriteLine($"Updated AI message in UI: Content='{aiMessage.Content}', Status='{aiMessage.Status}'");
                            });
                            
                            // Save AI message to database
                            try
                            {
                                aiMessage.Id = await _messageRepository.AddAsync(aiMessage);
                                Debug.WriteLine($"Saved AI message to database: ID={aiMessage.Id}");
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error saving AI message to database: {ex.Message}");
                            }
                            
                            // Update conversation in database
                            try
                            {
                                CurrentConversation.UpdatedAt = DateTime.UtcNow;
                                await _conversationRepository.UpdateAsync(CurrentConversation, CancellationToken.None);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error updating conversation: {ex.Message}");
                            }
                            
                            Debug.WriteLine("AI response received and displayed");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error getting AI response: {ex.Message}");
                            
                            aiMessage.Content = $"Error: {ex.Message}";
                            aiMessage.Status = "error";
                            aiMessage.IsError = true;
                            
                            await MainThread.InvokeOnMainThreadAsync(() =>
                            {
                                var index = Messages.IndexOf(aiMessage);
                                if (index >= 0)
                                {
                                    Messages.RemoveAt(index);
                                    Messages.Insert(index, aiMessage);
                                    OnPropertyChanged(nameof(Messages));
                                }
                            });
                            
                            await MainThread.InvokeOnMainThreadAsync(async () =>
                            {
                                await Shell.Current.DisplayAlert("Error", "Failed to get AI response. Please try again.", "OK");
                            });
                        }
                        finally
                        {
                            await MainThread.InvokeOnMainThreadAsync(() =>
                            {
                                IsThinking = false;
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in background processing: {ex.Message}");
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            IsThinking = false;
                        });
                    }
                    finally
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            IsMessageSending = false;
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SendMessage: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                IsMessageSending = false;
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
                
                // Remove the old AI message from UI immediately
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Messages.Remove(message);
                    OnPropertyChanged(nameof(Messages));
                });
                
                // Delete from database in background
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _messageRepository.DeleteAsync(message.Id, CancellationToken.None);
                        Debug.WriteLine($"Deleted message {message.Id} from database");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error deleting message from database: {ex.Message}");
                    }
                });
                
                // Create new thinking AI message
                var newAiMessage = new Message
                {
                    ConversationId = CurrentConversation.Id,
                    Content = "",
                    AuthorType = "ai",
                    IsUserMessage = false,
                    Status = "thinking",
                    SentAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Timestamp = DateTime.UtcNow,
                    ModelName = _AIModelManager.CurrentModel?.ModelName ?? "",
                    ProviderName = _AIModelManager.CurrentModel?.ProviderName ?? ""
                };
                
                // Add thinking message to UI
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Messages.Add(newAiMessage);
                    ScrollTarget = newAiMessage;
                    OnPropertyChanged(nameof(Messages));
                    OnPropertyChanged(nameof(ScrollTarget));
                });
                
                // Get AI response
                try
                {
                    var aiService = await GetCurrentAIServiceAsync();
                    if (aiService != null)
                    {
                        string aiResponse = await aiService.SendMessageAsync(userMessage.Content, _cts.Token);
                        
                        // Update AI message with response
                        newAiMessage.Content = aiResponse;
                        newAiMessage.Status = "complete";
                        
                        // Update UI
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            var index = Messages.IndexOf(newAiMessage);
                            if (index >= 0)
                            {
                                Messages.RemoveAt(index);
                                Messages.Insert(index, newAiMessage);
                                ScrollTarget = newAiMessage;
                                OnPropertyChanged(nameof(Messages));
                                OnPropertyChanged(nameof(ScrollTarget));
                            }
                        });
                        
                        // Save to database in background
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                newAiMessage.Id = await _messageRepository.AddAsync(newAiMessage);
                                Debug.WriteLine($"Saved regenerated AI message: ID={newAiMessage.Id}");
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error saving regenerated message: {ex.Message}");
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error regenerating AI response: {ex.Message}");
                    
                    // Update message with error
                    newAiMessage.Content = $"Error regenerating response: {ex.Message}";
                    newAiMessage.Status = "error";
                    
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        var index = Messages.IndexOf(newAiMessage);
                        if (index >= 0)
                        {
                            Messages.RemoveAt(index);
                            Messages.Insert(index, newAiMessage);
                            OnPropertyChanged(nameof(Messages));
                        }
                    });
                }
                
                // Update conversation timestamp in background
                _ = Task.Run(async () =>
                {
                    try
                    {
                        CurrentConversation.UpdatedAt = DateTime.UtcNow;
                        await _conversationRepository.UpdateAsync(CurrentConversation, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error updating conversation: {ex.Message}");
                    }
                });
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
        /// Gets the current AI service based on the selected model
        /// </summary>
        private async Task<IAIProviderService?> GetCurrentAIServiceAsync()
        {
            await Task.Delay(1);
            
            var currentModel = _AIModelManager.CurrentModel;
            if (currentModel == null)
            {
                Debug.WriteLine("No current model selected");
                return null;
            }

            try
            {
                return await _providerFactory.GetProviderForModelAsync(currentModel.ProviderName, currentModel.ModelName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting AI service: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Edits the title of the current conversation
        /// </summary>
        [RelayCommand]
        public async Task EditConversationTitle()
        {
            Debug.WriteLine("EditConversationTitle command executed");
            
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
                    "Enter a new title for this conversation:",
                    "Save",
                    "Cancel",
                    currentTitle,
                    maxLength: 100);
                    
                if (!string.IsNullOrWhiteSpace(result) && result != currentTitle)
                {
                    CurrentConversation.Title = result.Trim();
                    CurrentConversation.UpdatedAt = DateTime.UtcNow;
                    Title = result.Trim();
                    
                    // Update UI immediately
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        OnPropertyChanged(nameof(Title));
                        OnPropertyChanged(nameof(CurrentConversation));
                    });
                    
                    // Save to database
                    await _conversationRepository.UpdateAsync(CurrentConversation, CancellationToken.None);
                    
                    Debug.WriteLine($"Updated conversation title to: {result.Trim()}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error editing conversation title: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", "Failed to update conversation title.", "OK");
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
                    "Edit Title",
                    "Clear Chat",
                    "Delete Conversation",
                    "Change AI Model");
                    
                switch (result)
                {
                    case "Edit Title":
                        await EditConversationTitle();
                        break;
                    case "Clear Chat":
                        await ClearChatAsync();
                        break;
                    case "Delete Conversation":
                        await DeleteConversationAsync();
                        break;
                    case "Change AI Model":
                        await ChangeModelAsync();
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing menu: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Changes the AI model for this chat by showing favorite models
        /// </summary>
        [RelayCommand]
        public async Task ChangeModelAsync()
        {
            Debug.WriteLine("ChangeModelAsync command executed");
            try
            {
                // Get favorite models from the model manager
                var favoriteModels = await _modelRepository?.GetFavoriteModelsAsync();
                
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
        /// Event that fires when a conversation needs to be refreshed in sidebar
        /// </summary>
        public event Action? SidebarRefreshRequested;
        
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
                    var conversationToDelete = CurrentConversation;
                    Debug.WriteLine($"Deleting conversation: {conversationToDelete.Title} (ID: {conversationToDelete.Id})");
                    
                    // Delete the conversation from database
                    await _conversationRepository.DeleteAsync(conversationToDelete.Id, CancellationToken.None);
                    
                    // Request sidebar refresh to remove the deleted conversation
                    SidebarRefreshRequested?.Invoke();
                    
                    // Clear current conversation and messages
                    CurrentConversation = null;
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        Messages.Clear();
                        HasMessages = false;
                        HasConversation = false;
                        _currentOffset = 0;
                        HasMoreMessages = false;
                        ShowLoadMoreButton = false;
                    });
                    
                    // Try to load the most recent conversation
                    await LoadMostRecentConversationAsync();
                    
                    Debug.WriteLine("Conversation deleted and switched to most recent conversation");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error deleting conversation: {ex.Message}");
                    await Shell.Current.DisplayAlert("Error", "Failed to delete conversation. Please try again.", "OK");
                }
            }
        }
        
        /// <summary>
        /// Loads the most recent conversation for the current user
        /// </summary>
        private async Task LoadMostRecentConversationAsync()
        {
            try
            {
                const int currentUserId = 1; // Default user ID
                var recentConversations = await _conversationRepository.GetByUserIdAsync(currentUserId);
                
                if (recentConversations.Any())
                {
                    // Get the most recent conversation
                    var mostRecent = recentConversations
                        .OrderByDescending(c => c.UpdatedAt)
                        .FirstOrDefault();
                    
                    if (mostRecent != null)
                    {
                        Debug.WriteLine($"Loading most recent conversation: {mostRecent.Title} (ID: {mostRecent.Id})");
                        await InitializeAsync(mostRecent.Id);
                        return;
                    }
                }
                
                // No conversations found, create a new one
                Debug.WriteLine("No conversations found, creating new conversation");
                await InitializeNewConversationAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading most recent conversation: {ex.Message}");
                // Fallback to creating a new conversation
                try
                {
                    await InitializeNewConversationAsync();
                }
                catch (Exception fallbackEx)
                {
                    Debug.WriteLine($"Error creating fallback conversation: {fallbackEx.Message}");
                    // If all else fails, navigate back to main page
                    await GoBack();
                }
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
                
                // Clear any navigation stack issues by going to root
                await Shell.Current.GoToAsync("//MainPage");
                Debug.WriteLine("Navigation to MainPage succeeded");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Navigation failed: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Try alternative navigation methods
                try
                {
                    await Application.Current.MainPage.Navigation.PopAsync();
                    Debug.WriteLine("Alternative PopAsync succeeded");
                }
                catch (Exception popEx)
                {
                    Debug.WriteLine($"PopAsync also failed: {popEx.Message}");
                    
                    // Last resort - try to navigate using the navigation service
                    try
                    {
                        await _navigationService.NavigateToAsync("MainPage");
                        Debug.WriteLine("NavigationService navigation succeeded");
                    }
                    catch (Exception navEx)
                    {
                        Debug.WriteLine($"NavigationService also failed: {navEx.Message}");
                    }
                }
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
        public async void LoadConversation(Conversation conversation)
        {
            try
            {
                Debug.WriteLine($"Loading conversation: {conversation.Title} (ID: {conversation.Id})");
                
                // Clear messages immediately for instant feedback
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Messages.Clear();
                    HasMessages = false;
                    OnPropertyChanged(nameof(Messages));
                    OnPropertyChanged(nameof(HasMessages));
                });
                
                // Update basic conversation info immediately
                CurrentConversation = conversation;
                Title = conversation.Title ?? "New Chat";
                HasConversation = true;
                
                // Update UI properties immediately
                OnPropertyChanged(nameof(CurrentConversation));
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(HasConversation));
                
                // Load messages progressively in background
                _ = Task.Run(async () => await LoadMessagesProgressively(conversation.Id));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading conversation: {ex.Message}");
            }
        }
    }
}
