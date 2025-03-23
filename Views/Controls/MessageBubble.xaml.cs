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
                // Directly update UI - no async operations or extra methods
                if (message == null) return;
                
                try
                {
                    // Simple direct property setting - no layout calculations
                    bubble.ContentLabel.Text = message.Content;
                    bubble.TimestampLabel.Text = message.Timestamp.ToString("t");
                    bubble.MainGrid.HorizontalOptions = message.IsAI ? LayoutOptions.Start : LayoutOptions.End;
                    
                    // Set background color directly
                    bubble.MessageFrame.BackgroundColor = message.IsAI 
                        ? Color.FromArgb(Application.Current?.RequestedTheme == AppTheme.Dark ? "#303030" : "#f0f0f0")
                        : Color.FromArgb(Application.Current?.RequestedTheme == AppTheme.Dark ? "#0d47a1" : "#e3f2fd");
                    
                    // Set other properties
                    bubble.MessageFrame.BorderColor = Colors.Transparent;
                    
                    // Set status text
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
                        
                        bubble.StatusLabel.Text = statusText;
                        bubble.StatusLabel.IsVisible = !string.IsNullOrEmpty(statusText);
                    }
                    else
                    {
                        bubble.StatusLabel.IsVisible = false;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error updating MessageBubble UI: {ex.Message}");
                }
            }
        }
    }
}
