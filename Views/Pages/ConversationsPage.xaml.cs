using System;
using System.Diagnostics;
using NexusChat.Core.ViewModels;
using NexusChat.Services.Interfaces;
using NexusChat.Data.Interfaces;

namespace NexusChat.Views.Pages
{
    public partial class ConversationsPage : ContentPage {
        private readonly ConversationsPageViewModel _viewModel;

        public ConversationsPage(ConversationsPageViewModel viewModel = null) {
            InitializeComponent();

            // Use injected viewmodel or create new one if used in sidebar
            var conversationRepository = Handler?.MauiContext?.Services?.GetService<IConversationRepository>();
            var navigationService = Handler?.MauiContext?.Services?.GetService<INavigationService>();

            if (conversationRepository == null || navigationService == null) {
                throw new InvalidOperationException("Required services are not available.");
            }

            _viewModel = viewModel ?? new ConversationsPageViewModel(conversationRepository, navigationService);

            BindingContext = _viewModel;

            Debug.WriteLine("ConversationsPage initialized");
        }

        protected override async void OnAppearing() {
            base.OnAppearing();
            Debug.WriteLine("ConversationsPage.OnAppearing");

            if (_viewModel != null) {
                await _viewModel.OnAppearing();
            }
        }
    }
}
