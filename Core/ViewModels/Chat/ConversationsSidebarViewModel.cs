using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
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

            // Register for conversation change messages
            WeakReferenceMessenger.Default.Register<ConversationsChangedMessage>(this, async (r, m) =>
            {
                Debug.WriteLine($"ConversationsSidebarViewModel: Received ConversationsChangedMessage - {m.Reason}");
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await LoadConversations(forceRefresh: true);
                });
            });
            
            // Load conversations in background without blocking constructor
            Task.Run(async () => await LoadConversations());
        }

        [RelayCommand]
        private async Task NewConversation()
        {
            try
            {
                // Signal to ChatPage that a new conversation needs to be created by ChatViewModel.
                // Pass null to indicate this is a "create new" request.
                // ChatPage will then call ChatViewModel.InitializeNewConversationAsync().
                ConversationCreated?.Invoke(null); 
                Debug.WriteLine("ConversationsSidebarViewModel: Requested new conversation creation via ConversationCreated event with null.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ConversationsSidebarViewModel.NewConversation command: {ex.Message}");
                // Optionally, display an error to the user if appropriate for the sidebar context.
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    HasError = true;
                    ErrorMessage = "Failed to initiate new chat.";
                });
            }
        }

        [RelayCommand]
        private async Task OpenConversation(Conversation conversation)
        {
            try
            {
                if (conversation == null) return;

                // Verify the conversation still exists in the database
                var existingConversation = await _conversationRepository.GetByIdAsync(conversation.Id);
                if (existingConversation == null)
                {
                    Debug.WriteLine($"Conversation {conversation.Id} no longer exists, removing from list");
                    
                    // Remove from local collection
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        Conversations.Remove(conversation);
                        ShowNoConversations = Conversations.Count == 0;
                        
                        // If this was the selected conversation, clear selection
                        if (SelectedConversation?.Id == conversation.Id)
                        {
                            SelectedConversation = null;
                        }
                    });
                    
                    // Refresh the list to ensure consistency
                    await LoadConversations(forceRefresh: true);
                    return;
                }

                // Update selection state in UI
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    SelectedConversation = existingConversation;
                    
                    foreach (var conv in Conversations)
                    {
                        conv.IsSelected = conv.Id == existingConversation.Id;
                    }
                });
                
                // Update last accessed time in database
                existingConversation.LastAccessedAt = DateTime.Now;
                await _conversationRepository.UpdateAsync(existingConversation, CancellationToken.None);
                
                // Notify parent component about conversation selection
                ConversationSelected?.Invoke(existingConversation);
                
                Debug.WriteLine($"Selected conversation: {existingConversation.Title}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening conversation: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    HasError = true;
                    ErrorMessage = $"Failed to open conversation: {ex.Message}";
                });
                
                // Refresh the conversations list in case of database inconsistency
                await LoadConversations(forceRefresh: true);
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
                Debug.WriteLine($"Deleting conversation from sidebar: {conversation.Title} (ID: {conversation.Id})");
                
                // Store the deleted conversation ID for comparison
                var deletedConversationId = conversation.Id;
                var wasSelected = SelectedConversation?.Id == deletedConversationId;
                
                // Delete from database
                await _conversationRepository.DeleteAsync(conversation.Id, CancellationToken.None);
                
                // Remove from local collection
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    var convToRemove = Conversations.FirstOrDefault(c => c.Id == conversation.Id);
                    if (convToRemove != null)
                    {
                        Conversations.Remove(convToRemove);
                    }
                    ShowNoConversations = Conversations.Count == 0;
                });
                
                // Handle selection logic
                if (wasSelected)
                {
                    // If the deleted conversation was selected, select the most recent one
                    var mostRecentConversation = Conversations
                        .OrderByDescending(c => c.LastAccessedAt ?? c.UpdatedAt)
                        .ThenByDescending(c => c.CreatedAt)
                        .FirstOrDefault();
                    
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        if (mostRecentConversation != null)
                        {
                            // Clear all selections first
                            foreach (var conv in Conversations)
                            {
                                conv.IsSelected = false;
                            }
                            
                            // Select the most recent conversation
                            mostRecentConversation.IsSelected = true;
                            SelectedConversation = mostRecentConversation;
                            
                            Debug.WriteLine($"Selected most recent conversation: {mostRecentConversation.Title}");
                        }
                        else
                        {
                            SelectedConversation = null; // No conversations left
                        }
                    });
                    
                    // Notify parent components about the deletion and new selection
                    ConversationDeleted?.Invoke(conversation); // Notify original deletion
                    
                    if (mostRecentConversation != null)
                    {
                        ConversationSelected?.Invoke(mostRecentConversation); // Notify new selection
                    }
                    else
                    {
                        // Optionally, notify that no conversation is selected if your design requires it.
                        // For now, ConversationSelected expects a Conversation, so we can't pass null.
                        // If ChatPage needs to know no conversation is selected, it can check if SelectedConversation is null
                        // after this event, or a new event type could be introduced.
                    }
                }
                else
                {
                    // Just notify about the deletion
                    ConversationDeleted?.Invoke(conversation);
                }
                
                Debug.WriteLine($"Conversation deleted from sidebar: {conversation.Title}");
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

                Debug.WriteLine($"Loading conversations (forceRefresh: {forceRefresh})");

                var userId = GetCurrentUserId();
                var conversations = await _conversationRepository.GetByUserIdAsync(userId);
                
                Debug.WriteLine($"Loading conversations for user {userId}: found {conversations.Count()} conversations");
                
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
                        else
                        {
                            // The selected conversation no longer exists, clear selection
                            SelectedConversation = null;
                        }
                    }
                });

                Debug.WriteLine($"Loaded {Conversations.Count} conversations from database. ShowNoConversations: {ShowNoConversations}");
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
                    
                    // Ensure title is not empty
                    if (string.IsNullOrWhiteSpace(conversation.Title))
                    {
                        conversation.Title = "New Chat";
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
