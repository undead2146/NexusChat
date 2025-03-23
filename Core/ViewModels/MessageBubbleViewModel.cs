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
        private Message _message;
        
        public Message Message
        {
            get => _message;
            set
            {
                if (SetProperty(ref _message, value))
                {
                    // When message changes, notify these properties
                    OnPropertyChanged(nameof(StatusText));
                    OnPropertyChanged(nameof(HasStatus));
                    OnPropertyChanged(nameof(HasValidContent));
                    
                    if (value != null)
                    {
                        Debug.WriteLine($"MessageBubbleViewModel: Message set to: {value?.Content?.Substring(0, Math.Min(20, value?.Content?.Length ?? 0))}...");
                        Debug.WriteLine($"MessageBubbleViewModel: IsAI={value?.IsAI}, Timestamp={value?.Timestamp}");
                    }
                    else
                    {
                        Debug.WriteLine("MessageBubbleViewModel: Message set to null");
                    }
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
                string result = FormatStatusText();
                Debug.WriteLine($"MessageBubbleViewModel: StatusText = {result}");
                return result;
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
            get 
            {
                bool result = Message != null && !string.IsNullOrWhiteSpace(Message.Content);
                Debug.WriteLine($"MessageBubbleViewModel: HasValidContent = {result}");
                return result;
            }
        }
        
        /// <summary>
        /// Initializes a new instance of MessageBubbleViewModel
        /// </summary>
        public MessageBubbleViewModel()
        {
            // Initialize with empty state
            Debug.WriteLine("MessageBubbleViewModel: Created");
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
            // Currently no resources to clean up, but method is included for consistency
            Debug.WriteLine("MessageBubbleViewModel cleanup");
        }
    }
}
