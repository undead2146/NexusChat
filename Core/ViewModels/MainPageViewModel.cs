using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexusChat.Helpers;
using NexusChat.Views.Pages.DevTools;
using Microsoft.Maui.Controls;
using NexusChat.Tests;
using NexusChat.Views.Pages;
using NexusChat.Core.ViewModels.DevTools;
using NexusChat.Services;
using NexusChat.Services.Interfaces;
using NexusChat.Core.Models;

namespace NexusChat.Core.ViewModels
{
    /// <summary>
    /// ViewModel for the main page containing application home screen and navigation
    /// </summary>
    public partial class MainPageViewModel : BaseViewModel, IDisposable
    {
        private readonly INavigationService _navigationService;
        private MiniGameHelper _miniGameHelper;
        private Grid _mainGrid;
        private Button _counterButton;
        private bool _isNavigating;
        private bool _isNavigatingToThemes;
        private bool _isThemeEventSubscribed = false;
        private bool _isInitialized = false; // Added to track initialization

        [ObservableProperty]
        private string _themesButtonText = "View Themes";

        [ObservableProperty]
        private bool _themesButtonEnabled = true;
        
        [ObservableProperty]
        private int _maxComboScore = 0;
        
        [ObservableProperty]
        private string _themeIconText = "\uf185"; // Sun icon by default
        
        [ObservableProperty]
        private string _currentThemeText = "Light"; // Default to Light theme
        
        [ObservableProperty]
        private ObservableCollection<FavoriteModelItem> _favoriteModels = new();
        
        [ObservableProperty]
        private bool _hasNoFavoriteModels = true;

        [ObservableProperty]
        private bool _isDarkTheme = false;
        
        /// <summary>
        /// Initializes a new instance of the MainPageViewModel class
        /// </summary>
        public MainPageViewModel(INavigationService navigationService)
        {
            try 
            {
                _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
                
                // Initialize commands with defensive null checks
                HomeCommand = new RelayCommand(ShowHomeAlert);
                NewChatCommand = new RelayCommand(ShowNewChatAlert);
                ProfileCommand = new RelayCommand(ShowProfileAlert);
                
                ChatsCommand = new RelayCommand(ShowChatsAlert);
                ModelsCommand = new AsyncRelayCommand(HandleShowModels);
                SettingsCommand = new RelayCommand(ShowSettingsAlert);
                
                StartNewChatCommand = new AsyncRelayCommand(HandleStartNewChat);
                CounterClickCommand = new AsyncRelayCommand(HandleCounterClicked);
                
                NavigateToThemesCommand = new AsyncRelayCommand(HandleNavigateToThemes);
                RunModelTestsCommand = new AsyncRelayCommand(HandleRunModelTests);
                ViewDatabaseCommand = new AsyncRelayCommand(HandleViewDatabase);
                
                // Added command for theme toggling with safety check
                ToggleThemeCommand = new RelayCommand(HandleToggleThemeSafe);
                SelectModelCommand = new RelayCommand<FavoriteModelItem>(HandleModelSelected);
                
                // Initialize placeholder favorite models - must happen before bindings
                HasNoFavoriteModels = true;
                
                // Set default values for properties that might be accessed before initialization
                IsDarkTheme = false;
                ThemeIconText = "\uf185"; // Default sun icon
                CurrentThemeText = "Light";
                
                // Defer theme initialization until page appears
                // InitializeThemeAsync() will be called from InitializeUI
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in MainPageViewModel constructor: {ex.Message}");
                // Set defaults for critical properties to prevent null reference exceptions
                IsDarkTheme = false;
                ThemeIconText = "\uf185";
                CurrentThemeText = "Light";
                HasNoFavoriteModels = true;
            }
        }

        /// <summary>
        /// Initialize theme properties and events with proper error handling and delay
        /// </summary>
        private async void InitializeThemeAsync()
        {
            // Prevent multiple initializations
            if (_isInitialized) return;
            
            try
            {
                _isInitialized = true;
                
                // Delay to ensure app startup is complete before accessing theme
                await Task.Delay(100);
                
                // Update theme properties safely
                UpdateThemePropertiesSafe();
                
                // Subscribe to theme changes to keep UI in sync after a short delay
                await Task.Delay(50);
                
                try
                {
                    // Fixed: Don't check the event itself, just try to subscribe
                    if (!_isThemeEventSubscribed)
                    {
                        ThemeManager.ThemeChanged += OnThemeChanged;
                        _isThemeEventSubscribed = true;
                        Debug.WriteLine("Successfully subscribed to ThemeChanged event");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Could not subscribe to theme events: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in InitializeThemeAsync: {ex.Message}");
                // Set defaults for critical properties to prevent null reference exceptions
                IsDarkTheme = false;
                ThemeIconText = "\uf185";
                CurrentThemeText = "Light";
            }
        }

        /// <summary>
        /// Updates theme properties safely with exception handling
        /// </summary>
        private void UpdateThemePropertiesSafe()
        {
            try
            {
                // Safe access to theme status with multiple fallbacks
                bool isDark = false;
                try
                {
                    isDark = ThemeManager.IsDarkTheme;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to access ThemeManager.IsDarkTheme: {ex.Message}");
                    
                    // First fallback - check UserAppTheme
                    try
                    {
                        isDark = Application.Current?.UserAppTheme == AppTheme.Dark;
                    }
                    catch (Exception ex2)
                    {
                        Debug.WriteLine($"Failed to access Application.Current.UserAppTheme: {ex2.Message}");
                        
                        // Second fallback - check RequestedTheme
                        try
                        {
                            isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
                        }
                        catch (Exception ex3)
                        {
                            Debug.WriteLine($"Failed to access Application.Current.RequestedTheme: {ex3.Message}");
                            // Last resort fallback - assume light theme
                            isDark = false;
                        }
                    }
                }
                
                // Update properties
                IsDarkTheme = isDark;
                ThemeIconText = isDark ? "\uf186" : "\uf185"; // Moon or sun icon
                CurrentThemeText = isDark ? "Dark" : "Light";
                
                // Force UI update
                OnPropertyChanged(nameof(IsDarkTheme));
                OnPropertyChanged(nameof(ThemeIconText));
                OnPropertyChanged(nameof(CurrentThemeText));
                
                Debug.WriteLine($"Theme properties updated: IsDarkTheme={IsDarkTheme}, Icon={ThemeIconText}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating theme properties: {ex.Message}");
                // Keep default values if there's an error
            }
        }
        
        /// <summary>
        /// Safe handler for theme changes
        /// </summary>
        private void OnThemeChanged(object sender, bool isDark)
        {
            try
            {
                if (MainThread.IsMainThread)
                {
                    // Already on main thread
                    SafeUpdateThemeProperties(isDark);
                }
                else
                {
                    MainThread.BeginInvokeOnMainThread(() => {
                        SafeUpdateThemeProperties(isDark);
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in theme change handler: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Updates theme properties with extra safety checks
        /// </summary>
        private void SafeUpdateThemeProperties(bool isDark)
        {
            try
            {
                // Update local properties without using ThemeManager
                IsDarkTheme = isDark;
                
                // Explicitly set the correct icon based on the theme
                // This fixes the issue with the moon icon not updating
                ThemeIconText = isDark ? "\uf186" : "\uf185"; // \uf186 is moon, \uf185 is sun
                CurrentThemeText = isDark ? "Dark" : "Light";
                
                // Trigger UI updates
                OnPropertyChanged(nameof(IsDarkTheme));
                OnPropertyChanged(nameof(ThemeIconText));
                OnPropertyChanged(nameof(CurrentThemeText));
                
                // Debug to verify icon is being updated
                Debug.WriteLine($"Updated theme icon to: {ThemeIconText} for isDark={isDark}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SafeUpdateThemeProperties: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Safe wrapper for theme toggle
        /// </summary>
        private void HandleToggleThemeSafe()
        {
            try
            {
                // Store current state in case ThemeManager access fails
                bool currentIsDark = IsDarkTheme;
                
                // Set loading flag to avoid multiple clicks during theme change
                MainThread.BeginInvokeOnMainThread(async () => {
                    try
                    {
                        // Toggle local state immediately for better UI feedback
                        IsDarkTheme = !currentIsDark;
                        ThemeIconText = IsDarkTheme ? "\uf186" : "\uf185"; // Moon or sun icon
                        CurrentThemeText = IsDarkTheme ? "Dark" : "Light";
                        
                        // Update UI immediately
                        OnPropertyChanged(nameof(IsDarkTheme));
                        OnPropertyChanged(nameof(ThemeIconText));
                        OnPropertyChanged(nameof(CurrentThemeText));
                        
                        // Then try to toggle ThemeManager
                        try
                        {
                            ThemeManager.ToggleTheme();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Failed to toggle ThemeManager: {ex.Message}");
                            // Local UI is already updated, so we can continue
                        }
                        
                        // Force property refresh on UI thread after small delay
                        await Task.Delay(50);
                        MainThread.BeginInvokeOnMainThread(() => {
                            // Trigger another refresh for reliability
                            OnPropertyChanged(nameof(IsDarkTheme));
                            OnPropertyChanged(nameof(ThemeIconText));
                            OnPropertyChanged(nameof(CurrentThemeText));
                        });
                        
                        Debug.WriteLine($"Theme toggled successfully to {(IsDarkTheme ? "Dark" : "Light")}");
                    }
                    catch (Exception innerEx)
                    {
                        Debug.WriteLine($"Error during theme toggle on UI thread: {innerEx.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error toggling theme: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handles selection of a model
        /// </summary>
        private void HandleModelSelected(FavoriteModelItem model)
        {
            if (model == null) return;
            
            try
            {
                Debug.WriteLine($"Selected model: {model.Name}");
                // Here you would implement the model selection logic
                // For now, just navigate to the new chat page
                MainThread.BeginInvokeOnMainThread(async () => {
                    await HandleStartNewChat();
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error selecting model: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Command to toggle between light and dark themes
        /// </summary>
        public IRelayCommand ToggleThemeCommand { get; private set; }

        /// <summary>
        /// Command to select a model
        /// </summary>
        public IRelayCommand<FavoriteModelItem> SelectModelCommand { get; }

        #region Commands

        /// <summary>
        /// Command to navigate to home screen
        /// </summary>
        public IRelayCommand HomeCommand { get; }

        /// <summary>
        /// Command to start new chat
        /// </summary>
        public IRelayCommand NewChatCommand { get; }

        /// <summary>
        /// Command to view profile
        /// </summary>
        public IRelayCommand ProfileCommand { get; }

        /// <summary>
        /// Command to show chats list
        /// </summary>
        public ICommand ChatsCommand { get; }

        /// <summary>
        /// Command to show models list
        /// </summary>
        public ICommand ModelsCommand { get; }

        /// <summary>
        /// Command to show settings
        /// </summary>
        public ICommand SettingsCommand { get; }
        
        /// <summary>
        /// Command to initiate a new chat
        /// </summary>
        public ICommand StartNewChatCommand { get; }

        /// <summary>
        /// Command to navigate to theme test page
        /// </summary>
        public IAsyncRelayCommand NavigateToThemesCommand { get; }

        /// <summary>
        /// Command for counter button click
        /// </summary>
        public IAsyncRelayCommand CounterClickCommand { get; }
        
        /// <summary>
        /// Command to run model tests
        /// </summary>
        public IAsyncRelayCommand RunModelTestsCommand { get; }
        
        /// <summary>
        /// Command to view the database
        /// </summary>
        public IAsyncRelayCommand ViewDatabaseCommand { get; }

        #endregion
        
        #region Alert Handlers
        
        private void ShowHomeAlert()
        {
            Application.Current.MainPage.DisplayAlert("Home", "You're already on the home page", "OK");
        }
        
        private void ShowNewChatAlert()
        {
            Application.Current.MainPage.DisplayAlert("Coming Soon", "New chat functionality will be available in a future update", "OK");
        }
        
        private void ShowProfileAlert()
        {
            Application.Current.MainPage.DisplayAlert("Coming Soon", "User profile functionality will be available in a future update", "OK");
        }
        
        private void ShowChatsAlert()
        {
            Application.Current.MainPage.DisplayAlert("Chats", "Chat list will be available in a future update", "OK");
        }
        
        private async Task HandleShowModels()
        {
            Debug.WriteLine("MainPageViewModel: HandleShowModels start");
            try
            {
                // Use direct Shell navigation first as a more reliable approach
                await Shell.Current.GoToAsync("AIModelsPage");
                Debug.WriteLine("MainPageViewModel: HandleShowModels completed successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in HandleShowModels: {ex.Message}");
                
                try {
                    // Try alternative approach as fallback
                    await SafeNavigate("AIModelsPage");
                }
                catch (Exception innerEx) {
                    Debug.WriteLine($"Fallback navigation also failed: {innerEx.Message}");
                    await Application.Current.MainPage.DisplayAlert(
                        "Navigation Error", 
                        "Could not navigate to Models Page. Please try again.", 
                        "OK");
                }
            }
        }
        
        private void ShowSettingsAlert()
        {
            Application.Current.MainPage.DisplayAlert("Settings", "Settings page will be available in a future update", "OK");
        }
        
        #endregion
        
        #region Click Handlers
        
        /// <summary>
        /// Handles the start new chat button click
        /// </summary>
        private async Task HandleStartNewChat()
        {
            try
            {
                _isNavigating = true;
                // Navigate to ChatPage with better error handling
                await Shell.Current.GoToAsync("ChatPage");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Navigation error: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", 
                    $"Could not start a new chat: {ex.Message}", "OK");
            }
            finally
            {
                _isNavigating = false;
            }
        }
        
        /// <summary>
        /// Handles counter button click
        /// </summary>
        private async Task HandleCounterClicked()
        {
            if (_miniGameHelper == null && _counterButton != null && _mainGrid != null)
            {
                _miniGameHelper = new MiniGameHelper(_counterButton, _mainGrid);
            }
            
            if (_miniGameHelper != null)
            {
                await _miniGameHelper.HandleClick();
            }
        }
        #endregion
        
        #region Navigation Handlers

        /// <summary>
        /// Navigates to a specific page using the route name
        /// </summary>
        private async Task NavigateToPageAsync(string route)
        {
            if (IsNavigating) return;
            
            try
            {
                IsNavigating = true;
                await Shell.Current.GoToAsync(route);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Navigation error to {route}: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Navigation Error", 
                    $"Could not navigate to {route}: {ex.Message}", "OK");
            }
            finally
            {
                await Task.Delay(500);
                IsNavigating = false;
            }
        }
        
        /// <summary>
        /// Handles navigation to the Themes page
        /// </summary>
        private async Task HandleNavigateToThemes()
        {
            if (IsNavigatingToThemes) return;
            
            Debug.WriteLine("MainPageViewModel: HandleNavigateToThemes - Start");
            
            try
            {
                IsNavigatingToThemes = true;
                ThemesButtonEnabled = false;
                ThemesButtonText = "Loading...";
                
                await _navigationService.NavigateToAsync(nameof(ThemesPage)); // Updated navigation logic
                Debug.WriteLine("ThemesPage navigation successful");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in HandleNavigateToThemes: {ex.Message}");
                
                // Alert user with clearer error message
                await Application.Current.MainPage.DisplayAlert(
                    "Navigation Failed", 
                    "Could not open component library page.", 
                    "OK");
            }
            finally
            {
                await Task.Delay(300);
                ThemesButtonText = "View Themes"; // Updated text
                ThemesButtonEnabled = true;
                IsNavigatingToThemes = false;
            }
        }

        /// <summary>
        /// Handles running model tests
        /// </summary>
        private async Task HandleRunModelTests()
        {
            Debug.WriteLine("MainPageViewModel: HandleRunModelTests start");
            try
            {
                await _navigationService.NavigateToAsync(nameof(ModelTestingPage)); // Updated navigation logic
                Debug.WriteLine("MainPageViewModel: HandleRunModelTests completed successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in HandleRunModelTests: {ex.Message}");
                
                // Show error alert when navigation fails
                await Application.Current.MainPage.DisplayAlert(
                    "Navigation Error", 
                    $"Could not navigate to Model Testing Page: {ex.Message}", 
                    "OK");
            }
        }
        
        /// <summary>
        /// Handles viewing the database
        /// </summary>
        private async Task HandleViewDatabase()
        {
            await _navigationService.NavigateToAsync(nameof(DatabaseViewerPage)); // Updated navigation logic
        }

        /// <summary>
        /// Safely navigates to a page, preventing multiple rapid navigations
        /// </summary>
        private async Task SafeNavigate(string route)
        {
            if (IsNavigating) return;
            
            try
            {
                IsNavigating = true;
                
                // Always navigate on UI thread
                await MainThread.InvokeOnMainThreadAsync(async () => {
                    await Shell.Current.GoToAsync(route);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Navigation error to {route}: {ex.Message}");
            }
            finally
            {
                await Task.Delay(500);
                IsNavigating = false;
            }
        }
        
        #endregion
        
        #region Page Lifecycle

        /// <summary>
        /// Initializes references to UI elements from the view
        /// </summary>
        public void InitializeUI(Grid mainGrid, Button counterButton)
        {
            try
            {
                _mainGrid = mainGrid;
                _counterButton = counterButton;
                
                // Safely initialize mini-game
                if (_miniGameHelper == null && _counterButton != null && _mainGrid != null)
                {
                    _miniGameHelper = new MiniGameHelper(_counterButton, _mainGrid);
                    
                    // Subscribe to combo updates
                    _miniGameHelper.OnComboChanged += (maxCombo) => 
                    {
                        MaxComboScore = maxCombo;
                    };
                }
                
                // Now that UI is initialized, we can safely initialize the theme
                // This prevents crashes when ThemeManager tries to access UI elements
                if (!_isInitialized)
                {
                    InitializeThemeAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in InitializeUI: {ex.Message}");
            }
        }

        /// <summary>
        /// Cleans up resources when the view disappears
        /// </summary>
        public void Cleanup()
        {
            try 
            {
                if (_miniGameHelper != null)
                {
                    _miniGameHelper.Dispose();
                    _miniGameHelper = null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error cleaning up resources: {ex.Message}");
            }
        }

        /// <summary>
        /// Disposes of resources
        /// </summary>
        public void Dispose()
        {
            try
            {
                Cleanup();
                
                // Safely unsubscribe from theme events
                if (_isThemeEventSubscribed)
                {
                    try
                    {
                        ThemeManager.ThemeChanged -= OnThemeChanged;
                        _isThemeEventSubscribed = false;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error unsubscribing from theme events: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in Dispose: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Properties

        /// <summary>
        /// Gets or sets whether a navigation is currently in progress
        /// </summary>
        public bool IsNavigating 
        { 
            get => _isNavigating;
            private set => _isNavigating = value;
        }

        /// <summary>
        /// Gets or sets whether navigation to themes page is in progress
        /// </summary>
        public bool IsNavigatingToThemes
        {
            get => _isNavigatingToThemes;
            private set => _isNavigatingToThemes = value;
        }
        
        #endregion
    }

    /// <summary>
    /// Represents a favorite model item displayed on the home page
    /// </summary>
    public class FavoriteModelItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Provider { get; set; }
        public string ColorHex { get; set; }
    }
}
