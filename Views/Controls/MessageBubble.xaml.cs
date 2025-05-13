using System;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using NexusChat.Core.Models;
using NexusChat.Core.ViewModels;

namespace NexusChat.Views.Controls
{
    /// <summary>
    /// Custom control for displaying chat messages
    /// </summary>
    public partial class MessageBubble : ContentView
    {
        /// <summary>
        /// Message property
        /// </summary>
        public static readonly BindableProperty MessageProperty =
            BindableProperty.Create(nameof(Message), typeof(Message), typeof(MessageBubble), null,
                propertyChanged: OnMessageChanged);

        /// <summary>
        /// Message content property
        /// </summary>
        public static readonly BindableProperty MessageContentProperty =
            BindableProperty.Create(nameof(MessageContent), typeof(string), typeof(MessageBubble), string.Empty);

        /// <summary>
        /// IsAI property
        /// </summary>
        public static readonly BindableProperty IsAIProperty =
            BindableProperty.Create(nameof(IsAI), typeof(bool), typeof(MessageBubble), false);

        /// <summary>
        /// Timestamp property
        /// </summary>
        public static readonly BindableProperty TimestampProperty =
            BindableProperty.Create(nameof(Timestamp), typeof(DateTime), typeof(MessageBubble), DateTime.Now);

        /// <summary>
        /// RegenerateCommand property
        /// </summary>
        public static readonly BindableProperty RegenerateCommandProperty =
            BindableProperty.Create(nameof(RegenerateCommand), typeof(ICommand), typeof(MessageBubble));

        /// <summary>
        /// Gets or sets the message
        /// </summary>
        public Message Message
        {
            get => (Message)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        /// <summary>
        /// Gets or sets the message content
        /// </summary>
        public string MessageContent
        {
            get => (string)GetValue(MessageContentProperty);
            set => SetValue(MessageContentProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the message is from AI
        /// </summary>
        public bool IsAI
        {
            get => (bool)GetValue(IsAIProperty);
            set => SetValue(IsAIProperty, value);
        }

        /// <summary>
        /// Gets or sets the timestamp
        /// </summary>
        public DateTime Timestamp
        {
            get => (DateTime)GetValue(TimestampProperty);
            set => SetValue(TimestampProperty, value);
        }

        /// <summary>
        /// Gets or sets the regenerate command
        /// </summary>
        public ICommand RegenerateCommand
        {
            get => (ICommand)GetValue(RegenerateCommandProperty);
            set => SetValue(RegenerateCommandProperty, value);
        }
        
        private static void OnMessageChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is MessageBubble bubble && newValue is Message message)
            {
                bubble.MessageContent = message.Content;
                bubble.IsAI = message.IsAI;
                bubble.Timestamp = message.CreatedAt;
                
                // If status is 'thinking', show the thinking indicator
                if (message.Status?.ToLowerInvariant() == "thinking")
                {
                    bubble.ThinkingGrid.IsVisible = true;
                    bubble.ContentLabel.IsVisible = false;
                }
                else
                {
                    bubble.ThinkingGrid.IsVisible = false;
                    bubble.ContentLabel.IsVisible = true;
                }
            }
        }

        /// <summary>
        /// Initializes a new MessageBubble
        /// </summary>
        public MessageBubble()
        {
            InitializeComponent();
        }
        
        /// <summary>
        /// Updates message content during streaming
        /// </summary>
        public void UpdateStreamingContent(string content)
        {
            MessageContent = content;
        }
    }
}
