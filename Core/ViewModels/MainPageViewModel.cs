using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexusChat.Helpers;
using NexusChat.Views;
using Microsoft.Maui.Controls;
using NexusChat.Tests;

namespace NexusChat.ViewModels
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
        private bool _isNavigating;

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
            NavigateToThemeTestCommand = new AsyncRelayCommand(async () => await SafeNavigate(nameof(ThemeTestPage)));
            NavigateToIconTestCommand = new AsyncRelayCommand(async () => await SafeNavigate(nameof(IconTestPage)));
            CounterClickCommand = new AsyncRelayCommand(HandleCounterClicked);
            
            // Add the new model tests command
            RunModelTestsCommand = new AsyncRelayCommand(RunModelTests);
            
            // Add database viewer command
            ViewDatabaseCommand = new AsyncRelayCommand(ViewDatabase);
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
        public ICommand NavigateToThemeTestCommand { get; }

        /// <summary>
        /// Command to navigate to icon test page
        /// </summary>
        public ICommand NavigateToIconTestCommand { get; }

        /// <summary>
        /// Command for counter button click
        /// </summary>
        public ICommand CounterClickCommand { get; }
        
        /// <summary>
        /// Command to run model tests
        /// </summary>
        public ICommand RunModelTestsCommand { get; }
        
        /// <summary>
        /// Command to view the database
        /// </summary>
        public ICommand ViewDatabaseCommand { get; }

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
            await Application.Current.MainPage.DisplayAlert("New Chat", "Ready to start a new conversation?", "OK");
            
            // Animation handled in view - just for this control since it's a simple animation
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
        
        #region Navigation
        
        /// <summary>
        /// Safely navigates to a page, preventing multiple rapid navigations
        /// </summary>
        private async Task SafeNavigate(string route)
        {
            if (_isNavigating) return;
            
            try
            {
                _isNavigating = true;
                await Shell.Current.GoToAsync(route);
            }
            finally
            {
                await Task.Delay(500);
                _isNavigating = false;
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
            await SafeNavigate(nameof(Views.ModelTestingPage));
        }
        
        /// <summary>
        /// Opens the database viewer
        /// </summary>
        private async Task ViewDatabase()
        {
            await SafeNavigate(nameof(Views.DatabaseViewerPage));
        }
        
        #endregion
    }
}
