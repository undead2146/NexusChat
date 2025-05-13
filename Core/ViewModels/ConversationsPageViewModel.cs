using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexusChat.Core.Models;
using NexusChat.Data.Interfaces;
using NexusChat.Services.Interfaces;

namespace NexusChat.Core.ViewModels
{
    /// <summary>
    /// ViewModel for the ConversationsPage
    /// </summary>
    public partial class ConversationsPageViewModel : BaseViewModel
    {
        private readonly IConversationRepository _conversationRepository;
        private readonly INavigationService _navigationService;
        
        [ObservableProperty]
        private ObservableCollection<Conversation> _conversations = new ObservableCollection<Conversation>();
        
        [ObservableProperty]
        private bool _isRefreshing;
        
        [ObservableProperty]
        private Conversation _selectedConversation;
        
        [ObservableProperty]
        private string _searchQuery;
        
        /// <summary>
        /// Creates a new instance of ConversationsPageViewModel
        /// </summary>
        public ConversationsPageViewModel(
            IConversationRepository conversationRepository,
            INavigationService navigationService)
        {
            _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            
            Title = "Conversations";
            
            // Filter conversations when search query changes
            this.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(SearchQuery))
                {
                    FilterConversationsAsync().ConfigureAwait(false);
                }
            };
        }

        /// <summary>
        /// Called when the view appears
        /// </summary>
        public async Task OnAppearing()
        {
            // Force reload conversations when page appears
            await LoadConversationsAsync();
        }

        /// <summary>
        /// Loads all conversations
        /// </summary>
        [RelayCommand]
        private async Task LoadConversationsAsync()
        {
            try
            {
                IsRefreshing = true;
                IsBusy = true;
                
                var conversations = await _conversationRepository.GetAllAsync();
                
                // Log for debugging
                Debug.WriteLine($"Loaded {conversations.Count} conversations from repository");
                
                await FilterAndDisplayConversations(conversations);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading conversations: {ex.Message}");
            }
            finally
            {
                IsRefreshing = false;
                IsBusy = false;
            }
        }
        
        /// <summary>
        /// Refreshes the conversations list
        /// </summary>
        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadConversationsAsync();
        }
        
        /// <summary>
        /// Filters conversations based on the search query
        /// </summary>
        private async Task FilterConversationsAsync()
        {
            try
            {
                var allConversations = await _conversationRepository.GetAllAsync();
                await FilterAndDisplayConversations(allConversations);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error filtering conversations: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Filters and displays conversations
        /// </summary>
        private async Task FilterAndDisplayConversations(List<Conversation> allConversations)
        {
            await MainThread.InvokeOnMainThreadAsync(() => {
                // Filter conversations if there's a search query
                var filteredConversations = string.IsNullOrWhiteSpace(SearchQuery)
                    ? allConversations
                    : allConversations.Where(c => 
                        (c.Title?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (c.ModelName?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (c.ProviderName?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ?? false)
                    ).ToList();
                
                // Sort by most recently updated
                var sortedConversations = filteredConversations.OrderByDescending(c => c.UpdatedAt).ToList();
                
                // Update the observable collection
                Conversations.Clear();
                foreach (var conversation in sortedConversations)
                {
                    Conversations.Add(conversation);
                }
            });
        }
        
        /// <summary>
        /// Creates a new conversation
        /// </summary>
        [RelayCommand]
        private async Task NewConversationAsync()
        {
            try
            {
                Debug.WriteLine("Creating new conversation");
                
                // Navigate to chat page to start a new conversation
                await Shell.Current.GoToAsync("//chat");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating new conversation: {ex.Message}");
                
                // Try alternative navigation
                try {
                    await _navigationService.NavigateToAsync("chat");
                }
                catch (Exception navEx) {
                    Debug.WriteLine($"Alternative navigation failed: {navEx.Message}");
                }
            }
        }
        
        /// <summary>
        /// Selects a conversation
        /// </summary>
        [RelayCommand]
        private async Task SelectConversationAsync(Conversation conversation)
        {
            if (conversation == null)
                return;
            
            try
            {
                // Navigate to the chat page with the selected conversation
                var parameters = new Dictionary<string, object> {
                    { "conversationId", conversation.Id }
                };
                
                await _navigationService.NavigateToAsync("///chat", parameters);
                
                // Clear selection
                SelectedConversation = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error selecting conversation: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Deletes a conversation
        /// </summary>
        [RelayCommand]
        private async Task DeleteConversationAsync(Conversation conversation)
        {
            if (conversation == null)
                return;
            
            try
            {
                bool confirm = await Shell.Current.DisplayAlert(
                    "Delete Conversation",
                    $"Are you sure you want to delete '{conversation.Title}'?",
                    "Delete",
                    "Cancel");
                
                if (confirm)
                {
                    await _conversationRepository.DeleteAsync(conversation.Id);
                    
                    // Refresh the list
                    await LoadConversationsAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting conversation: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Navigates to the models page
        /// </summary>
        [RelayCommand]
        private async Task NavigateToModelsAsync()
        {
            try
            {
                Debug.WriteLine("Navigating to models page");
                await Shell.Current.GoToAsync("//models");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error navigating to models page: {ex.Message}");
                
                // Try alternative navigation
                try {
                    await _navigationService.NavigateToAsync("models");
                }
                catch (Exception navEx) {
                    Debug.WriteLine($"Alternative navigation failed: {navEx.Message}");
                }
            }
        }
        
        /// <summary>
        /// Navigates to the settings page
        /// </summary>
        [RelayCommand]
        private async Task NavigateToSettingsAsync()
        {
            try
            {
                Debug.WriteLine("Navigating to settings page");
                await Shell.Current.GoToAsync("//settings");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error navigating to settings page: {ex.Message}");
                
                // Try alternative navigation
                try {
                    await _navigationService.NavigateToAsync("settings");
                }
                catch (Exception navEx) {
                    Debug.WriteLine($"Alternative navigation failed: {navEx.Message}");
                }
            }
        }
    }
}
