using System;
using System.Diagnostics;
using Microsoft.Maui.Controls;
using NexusChat.Core.Models;

namespace NexusChat.Views.Controls
{
    /// <summary>
    /// Custom control for displaying chat messages with different styles based on author (user or AI)
    /// </summary>
    public partial class MessageBubble : ContentView
    {
        /// <summary>
        /// Bindable property for Message object
        /// </summary>
        public static readonly BindableProperty MessageProperty =
            BindableProperty.Create(nameof(Message), typeof(Message), typeof(MessageBubble), null, propertyChanged: OnMessageChanged);

        /// <summary>
        /// Bindable property indicating if the message is from an AI
        /// </summary>
        public static readonly BindableProperty IsAIProperty =
            BindableProperty.Create(nameof(IsAI), typeof(bool), typeof(MessageBubble), false);

        /// <summary>
        /// Bindable property for the message content text
        /// </summary>
        public static readonly BindableProperty MessageContentProperty =
            BindableProperty.Create(nameof(MessageContent), typeof(string), typeof(MessageBubble), string.Empty);

        /// <summary>
        /// Bindable property for the message timestamp
        /// </summary>
        public static readonly BindableProperty TimestampProperty =
            BindableProperty.Create(nameof(Timestamp), typeof(DateTime), typeof(MessageBubble), DateTime.Now);

        /// <summary>
        /// Gets or sets the Message object
        /// </summary>
        public Message Message
        {
            get => (Message)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the message is from an AI
        /// </summary>
        public bool IsAI
        {
            get => (bool)GetValue(IsAIProperty);
            set => SetValue(IsAIProperty, value);
        }

        /// <summary>
        /// Gets or sets the message content text
        /// </summary>
        public string MessageContent
        {
            get => (string)GetValue(MessageContentProperty);
            set => SetValue(MessageContentProperty, value);
        }

        /// <summary>
        /// Gets or sets the message timestamp
        /// </summary>
        public DateTime Timestamp
        {
            get => (DateTime)GetValue(TimestampProperty);
            set => SetValue(TimestampProperty, value);
        }

        /// <summary>
        /// Initializes a new instance of MessageBubble
        /// </summary>
        public MessageBubble()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing MessageBubble: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles changes to the Message property
        /// </summary>
        private static void OnMessageChanged(BindableObject bindable, object oldValue, object newValue)
        {
            try
            {
                var control = (MessageBubble)bindable;
                var message = (Message)newValue;

                if (message == null) return;

                // Update basic properties
                control.IsAI = message.IsAI;
                control.MessageContent = message.Content;
                control.Timestamp = message.Timestamp;
                
                // Check if this is a typing message
                bool isTyping = message.IsAI && message.Status == "Typing";
                
                // Update UI immediately on main thread
                MainThread.BeginInvokeOnMainThread(() => {
                    try
                    {
                        // Show/hide UI elements based on message state
                        if (control.ThinkingGrid != null)
                        {
                            control.ThinkingGrid.IsVisible = isTyping;
                            
                            // Make sure the thinking indicator is active if visible
                            var thinkingIndicator = control.ThinkingGrid.Children.FirstOrDefault() as ThinkingIndicator;
                            if (thinkingIndicator != null)
                            {
                                thinkingIndicator.IsActive = isTyping;
                            }
                        }
                        
                        // Show content only when not in typing state
                        if (control.ContentLabel != null)
                        {
                            control.ContentLabel.IsVisible = !isTyping;
                        }
                        
                        Debug.WriteLine($"Message bubble updated - ID: {message.Id}, IsAI: {message.IsAI}, " +
                                       $"Status: {message.Status}, IsTyping: {isTyping}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error updating message UI: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnMessageChanged: {ex}");
            }
        }
    }
}
