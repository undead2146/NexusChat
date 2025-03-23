using System;
using NexusChat.Core.ViewModels;

namespace NexusChat.Views.Pages
{
    /// <summary>
    /// Page for chat interactions with AI
    /// </summary>
    public partial class ChatPage : ContentPage
    {
        private readonly ChatViewModel _viewModel;
        
        /// <summary>
        /// Initializes a new instance of ChatPage with injected ViewModel
        /// </summary>
        public ChatPage(ChatViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }
        
        protected override void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.InitializeAsync();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _viewModel.Cleanup();
        }
    }
}
