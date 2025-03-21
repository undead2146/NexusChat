using NexusChat.ViewModels;
using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input; // Add this import for AsyncRelayCommand

namespace NexusChat
{
    public partial class MainPage : ContentPage, IDisposable
    {
        private MainPageViewModel _viewModel;

        public MainPage()
        {
            InitializeComponent();
            _viewModel = new MainPageViewModel(Navigation);
            BindingContext = _viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            SetupUIHandlers();
            
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

        private async void TestBtn_Clicked(object sender, EventArgs e)
        {
            if (sender is Button button)
                button.IsEnabled = false;
                
            if (_viewModel.NavigateToThemeTestCommand is IAsyncRelayCommand cmd)
                await cmd.ExecuteAsync(null);
            
            await Task.Delay(500);
            if (sender is Button btn)
                btn.IsEnabled = true;
        }

        private async void IconTestBtn_Clicked(object sender, EventArgs e)
        {
            if (sender is Button button)
                button.IsEnabled = false;
                
            if (_viewModel.NavigateToIconTestCommand is IAsyncRelayCommand cmd)
                await cmd.ExecuteAsync(null);
            
            await Task.Delay(500);
            if (sender is Button btn)
                btn.IsEnabled = true;
        }

        private async void OnCounterClicked(object sender, EventArgs e)
        {
            if (_viewModel.CounterClickCommand is IAsyncRelayCommand cmd)
                await cmd.ExecuteAsync(null);
        }
        
        public void Dispose()
        {
            _viewModel.Dispose();
        }
    }
}
