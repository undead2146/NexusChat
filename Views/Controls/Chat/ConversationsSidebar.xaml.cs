using Microsoft.Maui.Controls;
using NexusChat.Core.ViewModels;
using NexusChat.Core.Models;
using System.Diagnostics;

namespace NexusChat.Views.Controls
{
    public partial class ConversationsSidebar : ContentView
    {
        private ConversationsSidebarViewModel? _viewModel;

        public event Action<Conversation>? ConversationSelected;
        public event Action<Conversation>? ConversationCreated;
        public event Action<Conversation>? ConversationDeleted;

        public ConversationsSidebar()
        {
            InitializeComponent();
        }

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();
            
            // Unsubscribe from previous view model
            if (_viewModel != null)
            {
                _viewModel.ConversationSelected -= OnConversationSelected;
                _viewModel.ConversationCreated -= OnConversationCreated;
                _viewModel.ConversationDeleted -= OnConversationDeleted;
            }
            
            // Subscribe to new view model
            if (BindingContext is ConversationsSidebarViewModel viewModel)
            {
                _viewModel = viewModel;
                viewModel.ConversationSelected += OnConversationSelected;
                viewModel.ConversationCreated += OnConversationCreated;
                viewModel.ConversationDeleted += OnConversationDeleted;
            }
        }

        private void OnConversationSelected(Conversation conversation)
        {
            Debug.WriteLine($"ConversationsSidebar: Conversation selected - {conversation.Title}");
            ConversationSelected?.Invoke(conversation);
        }

        private void OnConversationCreated(Conversation conversation)
        {
            Debug.WriteLine($"ConversationsSidebar: New conversation created - {conversation.Title}");
            ConversationCreated?.Invoke(conversation);
        }

        private void OnConversationDeleted(Conversation conversation)
        {
            Debug.WriteLine($"ConversationsSidebar: Conversation deleted - {conversation.Title}");
            ConversationDeleted?.Invoke(conversation);
        }

        public async Task InitializeAsync()
        {
            if (_viewModel != null)
            {
                await _viewModel.InitializeAsync();
            }
        }

        public async Task RefreshAsync()
        {
            if (_viewModel != null)
            {
                await _viewModel.RefreshConversationsCommand.ExecuteAsync(null);
            }
        }

        public async Task HandleConversationUpdated(Conversation conversation)
        {
            if (_viewModel != null)
            {
                await _viewModel.HandleConversationUpdated(conversation);
            }
        }

        public void SelectConversation(int conversationId)
        {
            if (_viewModel != null)
            {
                var conversation = _viewModel.Conversations.FirstOrDefault(c => c.Id == conversationId);
                if (conversation != null)
                {
                    _viewModel.OpenConversationCommand.Execute(conversation);
                }
            }
        }
    }
}
