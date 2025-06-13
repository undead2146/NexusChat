using Microsoft.Maui.Controls;
using NexusChat.Core.ViewModels;
using NexusChat.Core.Models;
using NexusChat.Views.Pages;
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
            
            Debug.WriteLine($"ConversationsSidebar BindingContext changed to: {BindingContext?.GetType().Name ?? "null"}");
            
            // Unsubscribe from previous view model
            if (_viewModel != null)
            {
                _viewModel.ConversationSelected -= OnConversationSelected;
                _viewModel.ConversationCreated -= OnConversationCreated;
                _viewModel.ConversationDeleted -= OnConversationDeleted;
                _viewModel = null;
            }
            
            // Subscribe to new view model only if it's the correct type
            if (BindingContext is ConversationsSidebarViewModel viewModel)
            {
                _viewModel = viewModel;
                viewModel.ConversationSelected += OnConversationSelected;
                viewModel.ConversationCreated += OnConversationCreated;
                viewModel.ConversationDeleted += OnConversationDeleted;
                Debug.WriteLine("Successfully bound to ConversationsSidebarViewModel");
            }
            else if (BindingContext != null)
            {
                Debug.WriteLine($"Warning: Unexpected binding context type: {BindingContext.GetType().Name}");
                // Try to force the parent to reload with correct binding context
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Task.Delay(100);
                    if (Parent is ContentView parentView && parentView.Parent is ChatPage chatPage)
                    {
                        Debug.WriteLine("Requesting sidebar reload from ChatPage");
                        // Signal to ChatPage that sidebar needs to be reloaded
                    }
                });
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

        public async Task RefreshConversationsAsync()
        {
            if (_viewModel != null)
            {
                await _viewModel.LoadConversations(forceRefresh: true);
                Debug.WriteLine("ConversationsSidebar: Conversations refreshed");
            }
        }
        
        public void RemoveConversation(int conversationId)
        {
            if (_viewModel != null)
            {
                var conversation = _viewModel.Conversations.FirstOrDefault(c => c.Id == conversationId);
                if (conversation != null)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        _viewModel.Conversations.Remove(conversation);
                        _viewModel.ShowNoConversations = _viewModel.Conversations.Count == 0;
                        
                        if (_viewModel.SelectedConversation?.Id == conversationId)
                        {
                            _viewModel.SelectedConversation = _viewModel.Conversations.FirstOrDefault();
                        }
                    });
                }
            }
        }
    }
}
