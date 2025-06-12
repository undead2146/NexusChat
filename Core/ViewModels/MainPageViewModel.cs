using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexusChat.Core.Models;
using NexusChat.Helpers;
using NexusChat.Services;
using NexusChat.Services.Interfaces;
using Microsoft.Maui.Controls;
using NexusChat.Views.Pages;
using NexusChat.Data.Interfaces;
using System.Windows.Input;

namespace NexusChat.Core.ViewModels
{
    public partial class MainPageViewModel : BaseViewModel, IDisposable
    {
        private readonly INavigationService _navigationService;
        private readonly IAIModelRepository _modelRepository;
        private readonly IConversationRepository _conversationRepository;
        private bool _isNavigating;
        private bool _isThemeEventSubscribed = false;
        private bool _isInitialized = false;
        private bool _isLoadingData = false;

        [ObservableProperty]
        private string _themeIconText = "\uf185";
        
        [ObservableProperty]
        private string _currentThemeText = "Light";
        
        [ObservableProperty]
        private ObservableCollection<FavoriteModelItem> _favoriteModels = new();
        
        [ObservableProperty]
        private ObservableCollection<RecentConversationItem> _recentConversations = new();
        
        [ObservableProperty]
        private bool _hasFavoriteModels = false;

        [ObservableProperty]
        private bool _hasRecentConversations = false;

        [ObservableProperty]
        private bool _isDarkTheme = false;

        public IRelayCommand ToggleThemeCommand { get; }
        public IRelayCommand<FavoriteModelItem?> SelectModelCommand { get; }
        public IRelayCommand<RecentConversationItem?> SelectConversationCommand { get; }
        public IAsyncRelayCommand ModelsCommand { get; }
        public IAsyncRelayCommand StartNewChatCommand { get; }
        public IAsyncRelayCommand ViewChatsCommand { get; }

        public bool ShowEmptyState => !HasFavoriteModels && !HasRecentConversations;

        public MainPageViewModel(INavigationService navigationService, IAIModelRepository modelRepository, IConversationRepository conversationRepository)
        {
            try 
            {
                _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
                _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
                _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
                
                // Initialize commands
                ToggleThemeCommand = new RelayCommand(HandleToggleTheme);
                SelectModelCommand = new RelayCommand<FavoriteModelItem?>(HandleModelSelected);
                SelectConversationCommand = new RelayCommand<RecentConversationItem?>(HandleConversationSelected);
                ModelsCommand = new AsyncRelayCommand(HandleShowModels);
                StartNewChatCommand = new AsyncRelayCommand(HandleStartNewChat);
                ViewChatsCommand = new AsyncRelayCommand(HandleViewChats);
                
                // Set default values
                IsDarkTheme = false;
                ThemeIconText = "\uf185";
                CurrentThemeText = "Light";
                
                // Load data asynchronously
                LoadDataAsync().FireAndForget();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in MainPageViewModel constructor: {ex.Message}");
                // Set defaults
                IsDarkTheme = false;
                ThemeIconText = "\uf185";
                CurrentThemeText = "Light";
            }
        }

        /// <summary>
        /// Refreshes all data when page appears
        /// </summary>
        public async Task RefreshDataAsync()
        {
            if (_isLoadingData) return;
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            if (_isLoadingData) return;
            
            try
            {
                _isLoadingData = true;
                
                await LoadFavoriteModelsAsync();
                await LoadRecentConversationsAsync();
                
                // Update empty state after both loads complete
                OnPropertyChanged(nameof(ShowEmptyState));
                
                // Initialize theme after data load
                if (!_isInitialized)
                {
                    InitializeTheme();
                }
            }
            finally
            {
                _isLoadingData = false;
            }
        }

        private void InitializeTheme()
        {
            if (_isInitialized) return;
            
            try
            {
                _isInitialized = true;
                UpdateThemeProperties();
                
                if (!_isThemeEventSubscribed)
                {
                    ThemeManager.ThemeChanged += OnThemeChanged;
                    _isThemeEventSubscribed = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in InitializeTheme: {ex.Message}");
            }
        }

        private void UpdateThemeProperties()
        {
            try
            {
                bool isDark = ThemeManager.IsDarkTheme;
                
                IsDarkTheme = isDark;
                ThemeIconText = isDark ? "\uf186" : "\uf185";
                CurrentThemeText = isDark ? "Dark" : "Light";
                
                OnPropertyChanged(nameof(IsDarkTheme));
                OnPropertyChanged(nameof(ThemeIconText));
                OnPropertyChanged(nameof(CurrentThemeText));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating theme properties: {ex.Message}");
            }
        }
        
        private void OnThemeChanged(object? sender, bool isDark)
        {
            try
            {
                MainThread.BeginInvokeOnMainThread(() => {
                    IsDarkTheme = isDark;
                    ThemeIconText = isDark ? "\uf186" : "\uf185";
                    CurrentThemeText = isDark ? "Dark" : "Light";
                    
                    OnPropertyChanged(nameof(IsDarkTheme));
                    OnPropertyChanged(nameof(ThemeIconText));
                    OnPropertyChanged(nameof(CurrentThemeText));
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in theme change handler: {ex.Message}");
            }
        }
        
        private void HandleToggleTheme()
        {
            try
            {
                ThemeManager.ToggleTheme();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error toggling theme: {ex.Message}");
            }
        }
        
        private void HandleModelSelected(FavoriteModelItem? model)
        {
            if (model == null) return;
            
            try
            {
                Debug.WriteLine($"Selected model: {model.Name}");
                MainThread.BeginInvokeOnMainThread(async () => {
                    await HandleStartNewChat();
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error selecting model: {ex.Message}");
            }
        }

        private void HandleConversationSelected(RecentConversationItem? conversation)
        {
            if (conversation == null) return;
            
            try
            {
                Debug.WriteLine($"Selected conversation: {conversation.Title}");
                MainThread.BeginInvokeOnMainThread(async () => {
                    // Get the full conversation from database
                    var fullConversation = await _conversationRepository.GetConversationByIdAsync(conversation.Id);
                    
                    if (fullConversation != null)
                    {
                        // Navigate to existing conversation with absolute route
                        var parameters = new Dictionary<string, object>
                        {
                            { "conversation", fullConversation },
                            { "conversationId", conversation.Id }
                        };
                        
                        await Shell.Current.GoToAsync("//MainPage/ChatPage", parameters);
                        Debug.WriteLine($"Navigating to existing conversation: {fullConversation.Title} (ID: {fullConversation.Id})");
                    }
                    else
                    {
                        Debug.WriteLine($"Conversation with ID {conversation.Id} not found in database");
                        await DisplayAlert("Error", "The selected conversation could not be found.", "OK");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error selecting conversation: {ex.Message}");
            }
        }
        
        private async Task LoadFavoriteModelsAsync()
        {
            try
            {
                Debug.WriteLine("Starting LoadFavoriteModelsAsync");
                
                // Clear on UI thread
                await MainThread.InvokeOnMainThreadAsync(() => {
                    FavoriteModels.Clear();
                });
                
                var favorites = await _modelRepository.GetFavoriteModelsAsync();
                Debug.WriteLine($"Retrieved {favorites?.Count() ?? 0} favorites from repository");
                
                if (favorites != null && favorites.Any())
                {
                    var favoriteItems = new List<FavoriteModelItem>();
                    
                    foreach (var model in favorites.Take(6))
                    {
                        favoriteItems.Add(new FavoriteModelItem
                        {
                            Id = model.Id,
                            Name = model.ModelName ?? "Unknown Model",
                            Provider = model.ProviderName ?? "Unknown Provider",
                            ColorHex = GetColorForProvider(model.ProviderName)
                        });
                    }
                    
                    // Add items on UI thread
                    await MainThread.InvokeOnMainThreadAsync(() => {
                        FavoriteModels.Clear();
                        foreach (var item in favoriteItems)
                        {
                            FavoriteModels.Add(item);
                            Debug.WriteLine($"Added favorite model: {item.Name} from {item.Provider}");
                        }
                        
                        HasFavoriteModels = FavoriteModels.Count > 0;
                        OnPropertyChanged(nameof(HasFavoriteModels));
                        OnPropertyChanged(nameof(ShowEmptyState));
                    });
                }
                else
                {
                    await MainThread.InvokeOnMainThreadAsync(() => {
                        HasFavoriteModels = false;
                        OnPropertyChanged(nameof(HasFavoriteModels));
                        OnPropertyChanged(nameof(ShowEmptyState));
                    });
                }
                
                Debug.WriteLine($"LoadFavoriteModelsAsync completed. Count: {FavoriteModels.Count}, HasFavoriteModels: {HasFavoriteModels}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading favorite models: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(() => {
                    HasFavoriteModels = false;
                    OnPropertyChanged(nameof(HasFavoriteModels));
                    OnPropertyChanged(nameof(ShowEmptyState));
                });
            }
        }

        private async Task LoadRecentConversationsAsync()
        {
            try
            {
                Debug.WriteLine("Starting LoadRecentConversationsAsync");
                
                await MainThread.InvokeOnMainThreadAsync(() => {
                    RecentConversations.Clear();
                });
                
                var conversations = await _conversationRepository.GetRecentAsync(5);
                Debug.WriteLine($"Retrieved {conversations?.Count ?? 0} conversations from repository");
                
                if (conversations != null && conversations.Any())
                {
                    var conversationItems = new List<RecentConversationItem>();
                    
                    foreach (var conversation in conversations)
                    {
                        conversationItems.Add(new RecentConversationItem
                        {
                            Id = conversation.Id,
                            Title = conversation.Title ?? "Untitled Conversation",
                            UpdatedAt = conversation.UpdatedAt
                        });
                    }
                    
                    await MainThread.InvokeOnMainThreadAsync(() => {
                        RecentConversations.Clear();
                        foreach (var item in conversationItems)
                        {
                            RecentConversations.Add(item);
                            Debug.WriteLine($"Added conversation: {item.Title}");
                        }
                        
                        HasRecentConversations = RecentConversations.Count > 0;
                        OnPropertyChanged(nameof(HasRecentConversations));
                        OnPropertyChanged(nameof(ShowEmptyState));
                    });
                }
                else
                {
                    await MainThread.InvokeOnMainThreadAsync(() => {
                        HasRecentConversations = false;
                        OnPropertyChanged(nameof(HasRecentConversations));
                        OnPropertyChanged(nameof(ShowEmptyState));
                    });
                }
                
                Debug.WriteLine($"LoadRecentConversationsAsync completed. Count: {RecentConversations.Count}, HasRecentConversations: {HasRecentConversations}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading recent conversations: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(() => {
                    HasRecentConversations = false;
                    OnPropertyChanged(nameof(HasRecentConversations));
                    OnPropertyChanged(nameof(ShowEmptyState));
                });
            }
        }

        private string GetColorForProvider(string? provider)
        {
            return provider?.ToLowerInvariant() switch
            {
                "groq" => "#5E35B1",
                "openrouter" => "#1976D2",
                "anthropic" => "#00796B",
                "openai" => "#388E3C",
                "azure" => "#0078D4",
                _ => "#607D8B"
            };
        }

        private async Task HandleShowModels()
        {
            try
            {
                await Shell.Current.GoToAsync("AIModelsPage");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error navigating to models: {ex.Message}");
            }
        }
        
        private async Task HandleStartNewChat()
        {
            try
            {
                var newConversation = new Conversation
                {
                    Title = "New Chat",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                
                var parameters = new Dictionary<string, object>
                {
                    { "conversation", newConversation }
                };
                
                // Use absolute navigation to avoid route conflicts
                await Shell.Current.GoToAsync("//MainPage/ChatPage", parameters);
                Debug.WriteLine("Navigation to ChatPage with new conversation succeeded");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error starting new chat: {ex.Message}");
            }
        }

        private async Task HandleViewChats()
        {
            try
            {
                // TODO: Navigate to chats page when implemented
                await DisplayAlert("Coming Soon", "Chat history will be available soon!", "OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error viewing chats: {ex.Message}");
            }
        }

        private async Task DisplayAlert(string title, string message, string cancel)
        {
            try
            {
                var page = Application.Current?.Windows?.FirstOrDefault()?.Page;
                if (page != null)
                {
                    await page.DisplayAlert(title, message, cancel);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error displaying alert: {ex.Message}");
            }
        }

        public void Dispose()
        {
            try
            {
                if (_isThemeEventSubscribed)
                {
                    ThemeManager.ThemeChanged -= OnThemeChanged;
                    _isThemeEventSubscribed = false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in Dispose: {ex.Message}");
            }
        }
    }

    public class FavoriteModelItem
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Provider { get; set; }
        public required string ColorHex { get; set; }
    }

    public class RecentConversationItem
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
