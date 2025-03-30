using System;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using NexusChat.Core.Models;

namespace NexusChat.Core.ViewModels
{
    /// <summary>
    /// ViewModel for the MessageBubble control
    /// </summary>
    public partial class MessageBubbleViewModel : ObservableObject
    {
        private Message? _message;
        private string? _cachedStatusText;
        private bool _cachedHasValidContent;
        
        public Message Message
        {
            get => _message;
            set
            {
                if (SetProperty(ref _message, value))
                {
                    // Reset caches
                    _cachedStatusText = null;
                    
                    // When message changes, notify these properties
                    OnPropertyChanged(nameof(StatusText));
                    OnPropertyChanged(nameof(HasStatus));
                    OnPropertyChanged(nameof(HasValidContent));
                    
                    // Pre-compute values to improve rendering performance
                    _cachedHasValidContent = value != null && !string.IsNullOrWhiteSpace(value.Content);
                }
            }
        }

        /// <summary>
        /// Gets the formatted status text based on message status
        /// </summary>
        public string StatusText 
        {
            get 
            {
                // Use cached value if available
                if (_cachedStatusText != null)
                    return _cachedStatusText;
                    
                _cachedStatusText = FormatStatusText();
                return _cachedStatusText;
            }
        }
        
        /// <summary>
        /// Gets whether the message has a status to display
        /// </summary>
        public bool HasStatus => !string.IsNullOrEmpty(StatusText);
        
        /// <summary>
        /// Gets whether the message has valid content to display
        /// </summary>
        public bool HasValidContent 
        {
            get => _cachedHasValidContent;
        }
        
        /// <summary>
        /// Initializes a new instance of MessageBubbleViewModel
        /// </summary>
        public MessageBubbleViewModel()
        {
            // Initialize with empty state
        }

        /// <summary>
        /// Formats the message status text based on current status
        /// </summary>
        private string FormatStatusText()
        {
            if (Message == null)
                return string.Empty;
                
            if (Message.IsAI) 
                return string.Empty;
                
            return Message.Status switch
            {
                "Sent" => "✓ Sent",
                "Delivered" => "✓✓ Delivered",
                "Read" => "✓✓ Read",
                "Failed" => "⚠️ Failed to send",
                _ => string.Empty
            };
        }
        
        /// <summary>
        /// Cleans up resources used by the ViewModel
        /// </summary>
        public void Cleanup()
        {
            // Release resources
            _message = null;
            _cachedStatusText = null;
        }
    }
}
