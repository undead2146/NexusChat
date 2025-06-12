using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexusChat.Core.Models;
using NexusChat.Services.Interfaces;
using NexusChat.Data.Interfaces;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace NexusChat.Core.ViewModels
{
    public partial class ConversationsSidebarViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly IConversationRepository _conversationRepository;
        private readonly IChatService _chatService;

        [ObservableProperty]
        private ObservableCollection<Conversation> conversations = new();

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private bool hasError;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private bool showNoConversations;

        [ObservableProperty]
        private Conversation? selectedConversation;

        [ObservableProperty]
        private bool isRefreshing;

        public event Action<Conversation>? ConversationSelected;
        public event Action<Conversation>? ConversationCreated;
        public event Action<Conversation>? ConversationDeleted;

        public ConversationsSidebarViewModel(
            INavigationService navigationService,
            IConversationRepository conversationRepository,
            IChatService chatService)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
            _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
            
            Title = "Conversations";
        }

        [RelayCommand]
        private async Task NewConversation()
        {
            try
            {
                IsLoading = true;
                HasError = false;
                
                var newConversation = new Conversation 
                { 
                    Title = "New Chat",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    LastMessage = string.Empty,
                    UserId = GetCurrentUserId()
                };
                
                // Save to database
                await _conversationRepository.AddAsync(newConversation, CancellationToken.None);
                
                // Add to local collection
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Conversations.Insert(0, newConversation);
                    SelectedConversation = newConversation;
                    ShowNoConversations = false;
                });
                
                // Notify parent components
                ConversationSelected?.Invoke(newConversation);
                ConversationCreated?.Invoke(newConversation);
                
                Debug.WriteLine($"Created new conversation: {newConversation.Id}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating new conversation: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    HasError = true;
                    ErrorMessage = $"Failed to create new conversation: {ex.Message}";
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task OpenConversation(Conversation conversation)
        {
            try
            {
                if (conversation == null) return;

                // Update selection state in UI
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    SelectedConversation = conversation;
                    
                    foreach (var conv in Conversations)
                    {
                        conv.IsSelected = conv.Id == conversation.Id;
                    }
                });
                
                // Update last accessed time in database
                conversation.LastAccessedAt = DateTime.Now;
                await _conversationRepository.UpdateAsync(conversation, CancellationToken.None);
                
                // Notify parent component about conversation selection
                ConversationSelected?.Invoke(conversation);
                
                Debug.WriteLine($"Selected conversation: {conversation.Title}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening conversation: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    HasError = true;
                    ErrorMessage = $"Failed to open conversation: {ex.Message}";
                });
            }
        }

        [RelayCommand]
        private async Task DeleteConversation(Conversation conversation)
        {
            try
            {
                if (conversation == null) return;

                bool confirmed = await Shell.Current.DisplayAlert(
                    "Delete Conversation",
                    $"Are you sure you want to delete '{conversation.Title}'?\n\nThis action cannot be undone.",
                    "Delete",
                    "Cancel");

                if (!confirmed) return;

                IsLoading = true;
                
                // Delete from database
                await _conversationRepository.DeleteAsync(conversation, CancellationToken.None);
                
                // Remove from local collection
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Conversations.Remove(conversation);
                    
                    // Update selection if this was the selected conversation
                    if (SelectedConversation?.Id == conversation.Id)
                    {
                        SelectedConversation = Conversations.FirstOrDefault();
                        if (SelectedConversation != null)
                        {
                            SelectedConversation.IsSelected = true;
                            ConversationSelected?.Invoke(SelectedConversation);
                        }
                    }
                    
                    ShowNoConversations = Conversations.Count == 0;
                });
                
                // Notify parent components
                ConversationDeleted?.Invoke(conversation);
                
                Debug.WriteLine($"Deleted conversation: {conversation.Title}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting conversation: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    HasError = true;
                    ErrorMessage = $"Failed to delete conversation: {ex.Message}";
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task RefreshConversations()
        {
            await LoadConversations(forceRefresh: true);
        }

        [RelayCommand]
        private async Task EditConversationTitle(Conversation conversation)
        {
            try
            {
                if (conversation == null) return;

                string newTitle = await Shell.Current.DisplayPromptAsync(
                    "Edit Title",
                    "Enter a new title for this conversation:",
                    "Save",
                    "Cancel",
                    conversation.Title,
                    maxLength: 100);

                if (string.IsNullOrWhiteSpace(newTitle) || newTitle == conversation.Title)
                    return;

                // Update in database
                conversation.Title = newTitle.Trim();
                conversation.UpdatedAt = DateTime.Now;
                await _conversationRepository.UpdateAsync(conversation, CancellationToken.None);
                
                Debug.WriteLine($"Updated conversation title to: {newTitle}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating conversation title: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    HasError = true;
                    ErrorMessage = $"Failed to update conversation title: {ex.Message}";
                });
            }
        }

        public async Task LoadConversations(bool forceRefresh = false)
        {
            try
            {
                if (IsLoading && !forceRefresh) return;
                
                IsLoading = true;
                IsRefreshing = forceRefresh;
                HasError = false;
                ErrorMessage = string.Empty;

                var userId = GetCurrentUserId();
                var conversations = await _conversationRepository.GetByUserIdAsync(userId);
                
                // Sort by last updated/accessed time, then by creation time
                var sortedConversations = conversations
                    .OrderByDescending(c => c.LastAccessedAt ?? c.UpdatedAt)
                    .ThenByDescending(c => c.CreatedAt)
                    .ToList();

                // Update last message preview for each conversation
                await UpdateLastMessagePreviews(sortedConversations);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Conversations.Clear();
                    
                    foreach (var conversation in sortedConversations)
                    {
                        Conversations.Add(conversation);
                    }
                    
                    ShowNoConversations = Conversations.Count == 0;
                    
                    // Restore selection if it exists
                    if (SelectedConversation != null)
                    {
                        var existingSelected = Conversations.FirstOrDefault(c => c.Id == SelectedConversation.Id);
                        if (existingSelected != null)
                        {
                            existingSelected.IsSelected = true;
                            SelectedConversation = existingSelected;
                        }
                    }
                });

                Debug.WriteLine($"Loaded {Conversations.Count} conversations from database");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading conversations: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    HasError = true;
                    ErrorMessage = $"Failed to load conversations: {ex.Message}";
                    ShowNoConversations = Conversations.Count == 0;
                });
            }
            finally
            {
                IsLoading = false;
                IsRefreshing = false;
            }
        }

        private async Task UpdateLastMessagePreviews(List<Conversation> conversations)
        {
            try
            {
                foreach (var conversation in conversations)
                {
                    var lastMessage = await _chatService.GetLastMessageAsync(conversation.Id);
                    if (lastMessage != null)
                    {
                        conversation.LastMessage = TruncateMessage(lastMessage.Content);
                        conversation.UpdatedAt = lastMessage.Timestamp;
                    }
                    else
                    {
                        conversation.LastMessage = "No messages yet";
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating last message previews: {ex.Message}");
            }
        }

        private string TruncateMessage(string message, int maxLength = 100)
        {
            if (string.IsNullOrEmpty(message))
                return "No messages yet";
                
            if (message.Length <= maxLength)
                return message;
                
            return message.Substring(0, maxLength - 3) + "...";
        }

        private int GetCurrentUserId()
        {
            return 1;
        }

        public async Task HandleConversationUpdated(Conversation updatedConversation)
        {
            try
            {
                var existingConversation = Conversations.FirstOrDefault(c => c.Id == updatedConversation.Id);
                if (existingConversation != null)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        var index = Conversations.IndexOf(existingConversation);
                        if (index >= 0)
                        {
                            // Update properties
                            existingConversation.Title = updatedConversation.Title;
                            existingConversation.LastMessage = updatedConversation.LastMessage;
                            existingConversation.UpdatedAt = updatedConversation.UpdatedAt;
                            
                            // Move to top if it's the most recent
                            if (index > 0)
                            {
                                Conversations.RemoveAt(index);
                                Conversations.Insert(0, existingConversation);
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error handling conversation update: {ex.Message}");
            }
        }

        public override async Task InitializeAsync()
        {
            await LoadConversations();
        }
    }
}
