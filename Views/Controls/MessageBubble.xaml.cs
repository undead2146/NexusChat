using System;
using System.Diagnostics;
using Microsoft.Maui.Controls;
using NexusChat.Core.Models;

namespace NexusChat.Views.Controls
{
    public partial class MessageBubble : ContentView
    {
        public static readonly BindableProperty MessageProperty = 
            BindableProperty.Create(nameof(Message), typeof(Message), typeof(MessageBubble), 
                propertyChanged: OnMessageChanged);

        public Message Message
        {
            get => (Message)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public MessageBubble()
        {
            try
            {
                InitializeComponent();
                Debug.WriteLine("MessageBubble control initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing MessageBubble: {ex.Message}");
            }
        }

        private static void OnMessageChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is MessageBubble bubble && newValue is Message message)
            {
                Debug.WriteLine($"MessageBubble: Message changed - Content: {message?.Content?.Substring(0, Math.Min(20, message?.Content?.Length ?? 0))}..., IsAI: {message?.IsAI}");
                
                // Fix: Use BeginInvokeOnMainThread instead of InvokeOnMainThread
                MainThread.BeginInvokeOnMainThread(() => {
                    bubble.UpdateUI(message);
                });
            }
        }

        private void UpdateUI(Message message)
        {
            if (message == null)
                return;

            try
            {
                // Set content
                ContentLabel.Text = message.Content;
                
                // Set timestamp
                TimestampLabel.Text = message.Timestamp.ToString("t");
                
                // Set horizontal alignment based on message sender
                MainGrid.HorizontalOptions = message.IsAI ? LayoutOptions.Start : LayoutOptions.End;
                
                // Apply styling based on message type
                if (message.IsAI)
                {
                    // Set user message styling
                    MessageFrame.BackgroundColor = Application.Current?.RequestedTheme == AppTheme.Dark 
                        ? Color.FromArgb("#303030") 
                        : Color.FromArgb("#f0f0f0");
                }
                else
                {
                    // Set AI message styling
                    MessageFrame.BackgroundColor = Application.Current?.RequestedTheme == AppTheme.Dark 
                        ? Color.FromArgb("#0d47a1") 
                        : Color.FromArgb("#e3f2fd");
                }
                
                // Always set these properties directly rather than relying on styles
                MessageFrame.CornerRadius = 10;
                MessageFrame.Padding = new Thickness(12, 8);
                MessageFrame.BorderColor = Colors.Transparent;
                MessageFrame.HasShadow = false;
                
                // Set status text and visibility
                if (!message.IsAI && !string.IsNullOrEmpty(message.Status))
                {
                    string statusText = message.Status switch
                    {
                        "Sent" => "✓ Sent",
                        "Delivered" => "✓✓ Delivered",
                        "Read" => "✓✓ Read",
                        "Failed" => "⚠️ Failed to send",
                        _ => string.Empty
                    };
                    
                    StatusLabel.Text = statusText;
                    StatusLabel.IsVisible = !string.IsNullOrEmpty(statusText);
                }
                else
                {
                    StatusLabel.IsVisible = false;
                }
                
                // Force layout update to ensure changes are applied immediately
                InvalidateLayout();
                
                Debug.WriteLine($"MessageBubble UI updated for: {message.Content?.Substring(0, Math.Min(20, message.Content?.Length ?? 0))}...");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating MessageBubble UI: {ex.Message}");
            }
        }
    }
}
