using NexusChat.Core.ViewModels;
using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using NexusChat.Views.Pages;
using NexusChat.Views.Pages.DevTools;
using System.Diagnostics;
using NexusChat.Services;

namespace NexusChat.Views.Pages
{
    public partial class MainPage : ContentPage, IDisposable
    {
        private MainPageViewModel _viewModel;
        private bool _handlersInitialized = false;
        private readonly NavigationService _navigationService;

        public MainPage(NavigationService navigationService)
        {
            InitializeComponent();
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _viewModel = new MainPageViewModel(navigationService);
            BindingContext = _viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            // Only set up handlers once to prevent duplicate registrations
            if (!_handlersInitialized)
            {
                SetupUIHandlers();
                _handlersInitialized = true;
            }
            
            // Pass UI references to the ViewModel
            _viewModel.InitializeUI((Grid)Content, CounterBtn);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _viewModel.Cleanup();
        }

        private void SetupUIHandlers()
        {
            // Footer navigation
            HomeButton.Tapped += (s, e) => _viewModel.HomeCommand.Execute(null);
            NewChatButton.Tapped += (s, e) => _viewModel.NewChatCommand.Execute(null);
            ProfileButton.Tapped += (s, e) => _viewModel.ProfileCommand.Execute(null);
            
            // Quick access tabs
            ChatsTabTap.Tapped += (s, e) => _viewModel.ChatsCommand.Execute(null);
            ModelsTabTap.Tapped += (s, e) => _viewModel.ModelsCommand.Execute(null);
            SettingsTabTap.Tapped += (s, e) => _viewModel.SettingsCommand.Execute(null);

            // Welcome section button
            var newChatButtonTapGesture = new TapGestureRecognizer();
            newChatButtonTapGesture.Tapped += async (s, e) => {
                if (_viewModel.StartNewChatCommand is IAsyncRelayCommand cmd)
                    await cmd.ExecuteAsync(null);
                
                // Simple animation stays in the view since it's UI-specific
                if (s is Button btn)
                {   
                    await btn.ScaleTo(0.95, 100);
                    await btn.ScaleTo(1.0, 100);
                }
            };
            StartNewChatButton.GestureRecognizers.Add(newChatButtonTapGesture);
        }

        private async void ThemesBtn_Clicked(object sender, EventArgs e)
        {
            if (sender is Button button)
            {
                button.IsEnabled = false;
                
                // Execute the command through the ViewModel
                if (_viewModel.NavigateToThemesCommand != null)
                {
                    // Set up data binding for button text/state
                    button.SetBinding(Button.TextProperty, new Binding(nameof(MainPageViewModel.ThemesButtonText), source: _viewModel));
                    button.SetBinding(Button.IsEnabledProperty, new Binding(nameof(MainPageViewModel.ThemesButtonEnabled), source: _viewModel));
                    
                    // Execute the command
                    await _viewModel.NavigateToThemesCommand.ExecuteAsync(null);
                }
            }
        }

        private async void ModelTestBtn_Clicked(object sender, EventArgs e)
        {
            if (sender is Button button)
            {
                button.IsEnabled = false;
                
                if (_viewModel.RunModelTestsCommand != null)
                {
                    await _viewModel.RunModelTestsCommand.ExecuteAsync(null);
                }
                
                button.IsEnabled = true;
            }
        }

        private async void DbViewerBtn_Clicked(object sender, EventArgs e)
        {
            if (sender is Button button)
            {
                button.IsEnabled = false;
                
                if (_viewModel.ViewDatabaseCommand != null)
                {
                    await _viewModel.ViewDatabaseCommand.ExecuteAsync(null);
                }
                
                button.IsEnabled = true;
            }
        }

        private async void OnCounterClicked(object sender, EventArgs e)
        {
            if (_viewModel.CounterClickCommand != null)
            {
                await _viewModel.CounterClickCommand.ExecuteAsync(null);
            }
        }

        // Alternative cleanup approach - remove existing handlers
        private void CleanupEventHandlers()
        {
            // Remove tap gesture recognizers to prevent duplicate events
            if (StartNewChatButton.GestureRecognizers.Count > 0)
            {
                StartNewChatButton.GestureRecognizers.Clear();
            }
        }
        
        public void Dispose()
        {
            CleanupEventHandlers();
            _viewModel.Dispose();
        }
    }
}
