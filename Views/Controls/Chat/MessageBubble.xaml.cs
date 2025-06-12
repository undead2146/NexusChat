using System;
using System.Diagnostics;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using NexusChat.Core.Models;

namespace NexusChat.Views.Controls
{
    /// <summary>
    /// Custom control for displaying chat messages 
    /// </summary>
    public partial class MessageBubble : ContentView
    {
        #region Bindable Properties

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

        #endregion

        #region Properties

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

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new MessageBubble
        /// </summary>
        public MessageBubble()
        {
            InitializeComponent();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Handles message property changes
        /// </summary>
        private static void OnMessageChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is not MessageBubble bubble) return;
            
            try
            {
                if (newValue is Message message)
                {
                    bubble.MessageContent = message.Content ?? string.Empty;
                    bubble.IsAI = message.IsAI;
                    bubble.Timestamp = message.CreatedAt;
                    
                    Debug.WriteLine($"MessageBubble updated: Content='{bubble.MessageContent}', IsAI={bubble.IsAI}, Status='{message.Status}'");
                    
                    // Handle thinking state - only show thinking if AI message with thinking status and no content
                    bubble.UpdateThinkingState(message);
                }
                else
                {
                    // Clear properties if message is null
                    bubble.MessageContent = string.Empty;
                    bubble.IsAI = false;
                    bubble.Timestamp = DateTime.Now;
                    bubble.UpdateThinkingState(null);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MessageBubble: Error in OnMessageChanged - {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the thinking indicator state
        /// </summary>
        private void UpdateThinkingState(Message? message)
        {
            try
            {
                bool shouldShowThinking = false;
                bool shouldShowContent = true;
                
                if (message != null && message.IsAI)
                {
                    // Only show thinking if status is explicitly "thinking" AND content is empty
                    // AND the message was just created (not loaded from database)
                    shouldShowThinking = message.Status == "thinking" && 
                                       string.IsNullOrWhiteSpace(message.Content) &&
                                       message.Id == 0; // New messages have Id = 0 until saved
                    
                    shouldShowContent = !shouldShowThinking;
                }
                else if (message != null && !message.IsAI)
                {
                    // User messages always show content
                    shouldShowThinking = false;
                    shouldShowContent = true;
                }
                
                Debug.WriteLine($"MessageBubble thinking state: ShowThinking={shouldShowThinking}, ShowContent={shouldShowContent}, Status='{message?.Status}', Content='{message?.Content}', Id={message?.Id}");
                
                // Update UI elements
                if (ThinkingGrid != null)
                    ThinkingGrid.IsVisible = shouldShowThinking;
                    
                if (ContentLabel != null)
                    ContentLabel.IsVisible = shouldShowContent;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MessageBubble: Error updating thinking state - {ex.Message}");
                // Fallback to show content and hide thinking
                if (ThinkingGrid != null)
                    ThinkingGrid.IsVisible = false;
                if (ContentLabel != null)
                    ContentLabel.IsVisible = true;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates message content during streaming
        /// </summary>
        public void UpdateStreamingContent(string content)
        {
            try
            {
                MessageContent = content ?? string.Empty;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MessageBubble: Error updating streaming content - {ex.Message}");
            }
        }

        #endregion
    }
}
