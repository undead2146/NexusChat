using System;
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

namespace NexusChat.Core.ViewModels
{
    /// <summary>
    /// ViewModel for the main page containing application home screen and navigation
    /// </summary>
    public partial class MainPageViewModel : ObservableObject, IDisposable
    {
        private readonly INavigation _navigation;
        private MiniGameHelper _miniGameHelper;
        private Grid _mainGrid;
        private Button _counterButton;

        [ObservableProperty]
        private bool _isNavigating;

        [ObservableProperty]
        private bool _isNavigatingToThemes;

        [ObservableProperty]
        private string _themesButtonText = "Themes";

        [ObservableProperty]
        private bool _themesButtonEnabled = true;

        /// <summary>
        /// Initializes a new instance of the MainPageViewModel class
        /// </summary>
        public MainPageViewModel(INavigation navigation)
        {
            _navigation = navigation;
            
            // Initialize commands
            HomeCommand = new RelayCommand(ShowHomeAlert);
            NewChatCommand = new RelayCommand(ShowNewChatAlert);
            ProfileCommand = new RelayCommand(ShowProfileAlert);
            
            ChatsCommand = new RelayCommand(ShowChatsAlert);
            ModelsCommand = new RelayCommand(ShowModelsAlert);
            SettingsCommand = new RelayCommand(ShowSettingsAlert);
            
            StartNewChatCommand = new AsyncRelayCommand(HandleStartNewChat);
            CounterClickCommand = new AsyncRelayCommand(HandleCounterClicked);
            
            // Define navigation commands - remove IconsPage command
            NavigateToThemesCommand = new AsyncRelayCommand(HandleNavigateToThemes);
            RunModelTestsCommand = new AsyncRelayCommand(HandleRunModelTests);
            ViewDatabaseCommand = new AsyncRelayCommand(HandleViewDatabase);
        }
        
        #region Commands

        /// <summary>
        /// Command to navigate to home screen
        /// </summary>
        public ICommand HomeCommand { get; }

        /// <summary>
        /// Command to start new chat
        /// </summary>
        public ICommand NewChatCommand { get; }

        /// <summary>
        /// Command to view profile
        /// </summary>
        public ICommand ProfileCommand { get; }

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

        // Remove NavigateToIconsCommand

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
        
        private void ShowModelsAlert()
        {
            Application.Current.MainPage.DisplayAlert("Models", "AI model selection will be available in a future update", "OK");
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
                
                // FIXED NAVIGATION: Create a new instance directly rather than using complex routing
                await MainThread.InvokeOnMainThreadAsync(async () => 
                {
                    try
                    {
                        // Simple navigation with direct instance creation
                        var viewModel = new ThemesPageViewModel(); // Create viewmodel directly
                        var page = new ThemesPage(viewModel);      // Create page with viewmodel
                        
                        // Push page directly to navigation stack - avoid Shell complexity
                        await _navigation.PushAsync(page);
                        
                        Debug.WriteLine("ThemesPage navigation successful");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Navigation error: {ex.Message}");
                        
                        // Alert user with clearer error message
                        await Application.Current.MainPage.DisplayAlert(
                            "Navigation Failed", 
                            "Could not open component library page.", 
                            "OK");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in HandleNavigateToThemes: {ex.Message}");
            }
            finally
            {
                await Task.Delay(300);
                ThemesButtonText = "Themes";
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
                // FIX: Use SafeNavigate just like other navigation methods
                await SafeNavigate(nameof(ModelTestingPage));
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
            await SafeNavigate(nameof(DatabaseViewerPage));
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
            _mainGrid = mainGrid;
            _counterButton = counterButton;
            
            // Initialize mini-game
            if (_miniGameHelper == null && _counterButton != null && _mainGrid != null)
            {
                _miniGameHelper = new MiniGameHelper(_counterButton, _mainGrid);
            }
        }

        /// <summary>
        /// Cleans up resources when the view disappears
        /// </summary>
        public void Cleanup()
        {
            _miniGameHelper?.Dispose();
            _miniGameHelper = null;
        }

        /// <summary>
        /// Disposes of resources
        /// </summary>
        public void Dispose()
        {
            Cleanup();
        }
        
        #endregion
        
        #region Test Handlers
        
        /// <summary>
        /// Runs tests for data models
        /// </summary>
        private async Task RunModelTests()
        {
            // Ensure consistent namespace usage
            await SafeNavigate(nameof(ModelTestingPage));
        }
        
        /// <summary>
        /// Opens the database viewer
        /// </summary>
        private async Task ViewDatabase()
        {
            await SafeNavigate(nameof(DatabaseViewerPage));
        }
        
        #endregion
    }
}
