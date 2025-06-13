using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexusChat.Core.Models;
using System.Windows.Input;

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
        private string? _formattedContent;
        
        [ObservableProperty]
        private bool _isStreaming = false;
        
        [ObservableProperty]
        private string _streamingContent = string.Empty;
        
        /// <summary>
        /// Gets or sets the message
        /// </summary>
        public Message Message
        {
            get => _message;
            set
            {
                if (SetProperty(ref _message, value))
                {
                    // Reset caches
                    _cachedStatusText = null;
                    _formattedContent = null;
                    
                    // When message changes, notify these properties
                    OnPropertyChanged(nameof(StatusText));
                    OnPropertyChanged(nameof(HasStatus));
                    OnPropertyChanged(nameof(HasValidContent));
                    OnPropertyChanged(nameof(FormattedContent));
                    OnPropertyChanged(nameof(IsAI));
                    
                    // Pre-compute values to improve rendering performance
                    _cachedHasValidContent = value != null && !string.IsNullOrWhiteSpace(value.Content);
                }
            }
        }
        
        /// <summary>
        /// Gets whether the message is from AI
        /// </summary>
        public bool IsAI => _message?.IsAI ?? false;
        public string StatusText 
        {
            get 
            {
                if (_cachedStatusText != null)
                    return _cachedStatusText;

                if (_message == null)
                {
                    _cachedStatusText = string.Empty;
                    return _cachedStatusText;
                }

                if (_message.IsError)
                    _cachedStatusText = "Error";
                else if (_message.IsAI && _message.Status == "thinking")
                    _cachedStatusText = "Thinking...";
                else if (_message.IsAI && _message.Status == "streaming")
                    _cachedStatusText = "Streaming...";
                else if (!_message.IsAI && _message.Status == "pending")
                    _cachedStatusText = "Sending...";
                else
                    _cachedStatusText = string.Empty; // Or some default like "Sent" / "Received" if desired

                return _cachedStatusText;
            }
        }
        public string FormattedContent
        {
            get
            {
                if (_formattedContent != null)
                    return _formattedContent;
                    
                if (_message == null || string.IsNullOrEmpty(_message.Content))
                {
                    _formattedContent = string.Empty;
                    return _formattedContent;
                }
                _formattedContent = _message.Content.Trim(); 
                return _formattedContent;
            }
        }
        public bool HasStatus => !string.IsNullOrEmpty(StatusText);
        
        /// <summary>
        /// Gets whether the message has valid content to display
        /// </summary>
        public bool HasValidContent 
        {
            get => _cachedHasValidContent;
        }
        
        /// <summary>
        /// Command for regenerating an AI message
        /// </summary>
        public ICommand RegenerateCommand { get; set; }
        
        /// <summary>
        /// Initializes a new instance of MessageBubbleViewModel
        /// </summary>
        public MessageBubbleViewModel()
        {
            // Initialize with empty state
            RegenerateCommand = new Command<Message>(OnRegenerateMessage);
        }
        
        /// <summary>
        /// Handles regenerate message command
        /// </summary>
        private void OnRegenerateMessage(Message message)
        {
            try
            {
                if (message == null || !message.IsAI) return;
                
                Debug.WriteLine($"Requesting regeneration of message ID: {message.Id}");
                // The actual implementation would be in the parent ViewModel
                // which will be bound to this command
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnRegenerateMessage: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Updates content during streaming
        /// </summary>
        public void UpdateStreamingContent(string content)
        {
            IsStreaming = true;
            StreamingContent = content;
        }
        
        /// <summary>
        /// Stops streaming and updates final content
        /// </summary>
        public void StopStreaming()
        {
            IsStreaming = false;
            
            if (_message != null && !string.IsNullOrEmpty(StreamingContent))
            {
                _message.Content = StreamingContent;
                _formattedContent = null;  // Clear cache to force reformatting
                OnPropertyChanged(nameof(FormattedContent));
            }
            
            StreamingContent = string.Empty;
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
                "sent" => "✓ Sent",
                "delivered" => "✓✓ Delivered",
                "read" => "✓✓ Read",
                "failed" => "⚠️ Failed to send",
                _ => string.Empty
            };
        }
        
        /// <summary>
        /// Formats message content for display
        /// </summary>
        private string FormatMessageContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return string.Empty;
                
            try
            {
                // Replace multiple newlines with just two
                string formatted = Regex.Replace(content, @"\n{3,}", "\n\n");
                
                return formatted;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error formatting message content: {ex.Message}");
                return content;  // Return original on error
            }
        }
        
        /// <summary>
        /// Cleans up resources used by the ViewModel
        /// </summary>
        public void Cleanup()
        {
            // Release resources
            _message = null;
            _cachedStatusText = null;
            _formattedContent = null;
        }
    }
}
