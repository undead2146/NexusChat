using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexusChat.Core.Models;
using NexusChat.Data.Repositories;
using System.Linq;
using NexusChat.Services.Interfaces;

namespace NexusChat.Core.ViewModels
{
    /// <summary>
    /// ViewModel for chat interaction functionality
    /// </summary>
    public partial class ChatViewModel : ObservableObject
    {
        private readonly IAIService _aiService;
        private readonly IMessageRepository _messageRepository;
        private readonly IConversationRepository _conversationRepository;
        
        /// <summary>
        /// Currently active conversation
        /// </summary>
        [ObservableProperty]
        private Conversation _currentConversation;
        
        /// <summary>
        /// Collection of messages in the current conversation
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<Message> _messages;
        
        /// <summary>
        /// User input message text
        /// </summary>
        [ObservableProperty]
        private string _messageText;
        
        /// <summary>
        /// Indicates if an operation is in progress
        /// </summary>
        [ObservableProperty]
        private bool _isBusy;

        /// <summary>
        /// Initializes a new instance of ChatViewModel
        /// </summary>
        public ChatViewModel(
            IAIService aiService,
            IMessageRepository messageRepository,
            IConversationRepository conversationRepository)
        {
            _aiService = aiService;
            _messageRepository = messageRepository;
            _conversationRepository = conversationRepository;
            
            _messages = new ObservableCollection<Message>();
            
            // Initialize commands
            SendMessageCommand = new AsyncRelayCommand(SendMessageAsync, CanSendMessage);
            LoadConversationCommand = new AsyncRelayCommand<int>(LoadConversationAsync);
        }

        /// <summary>
        /// Command to send a message
        /// </summary>
        public IAsyncRelayCommand SendMessageCommand { get; }
        
        /// <summary>
        /// Command to load a specific conversation
        /// </summary>
        public IAsyncRelayCommand<int> LoadConversationCommand { get; }

        /// <summary>
        /// Initializes the ViewModel
        /// </summary>
        public async void InitializeAsync()
        {
            if (CurrentConversation == null)
            {
                // Load default conversation or create new one
                await LoadMostRecentConversationAsync();
            }
        }
        
        /// <summary>
        /// Cleans up resources
        /// </summary>
        public void Cleanup()
        {
            // Cleanup code, e.g., save state, unsubscribe from events
        }
        
        private bool CanSendMessage()
        {
            return !string.IsNullOrWhiteSpace(MessageText) && !IsBusy;
        }
        
        private async Task SendMessageAsync()
        {
            // Implementation for sending messages
            // ...
        }
        
        private async Task LoadConversationAsync(int conversationId)
        {
            // Implementation for loading a conversation
            // ...
        }
        
        private async Task LoadMostRecentConversationAsync()
        {
            // Implementation for loading most recent conversation
            // ...
        }
    }
}
