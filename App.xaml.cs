using NexusChat.Helpers;
using Microsoft.Maui.Controls;
using CommunityToolkit.Mvvm.Messaging;
using NexusChat.Core.ViewModels;
using NexusChat.Resources.Styles;
using NexusChat.Data.Context;
using NexusChat.Services;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace NexusChat
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            
            // Register styles for message bubbles globally
            RegisterMessageBubbleStyles();
            
            MainPage = new AppShell();
            
            // Initialize theme system
            ThemeManager.Initialize();
            
            // Initialize database in the background
            Task.Run(async () => await InitializeServicesAsync());
        }
        
        private async Task InitializeServicesAsync()
        {
            try
            {
                // Get the startup initializer from the service provider
                var initializer = Handler.MauiContext?.Services.GetService<IStartupInitializer>();
                
                if (initializer != null)
                {
                    await initializer.InitializeAsync();
                    Debug.WriteLine("App startup initialization completed successfully");
                }
                else
                {
                    Debug.WriteLine("IStartupInitializer was not found in the service provider");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during app initialization: {ex.Message}");
            }
        }

        /// <summary>
        /// Ensures message bubble styles are available globally
        /// </summary>
        private void RegisterMessageBubbleStyles()
        {
            try
            {
                // Add user bubble style
                if (!Resources.TryGetValue("MessageBubbleUserFrame", out _))
                {
                    Resources.Add("MessageBubbleUserFrame", new Style(typeof(Frame))
                    {
                        Setters =
                        {
                            new Setter { Property = Frame.BackgroundColorProperty, Value = Application.Current?.RequestedTheme == AppTheme.Dark 
                                ? Color.FromArgb("#0d47a1") 
                                : Color.FromArgb("#e3f2fd") },
                            new Setter { Property = Frame.CornerRadiusProperty, Value = 10 },
                            new Setter { Property = Frame.PaddingProperty, Value = new Thickness(12, 8) },
                            new Setter { Property = Frame.BorderColorProperty, Value = Colors.Transparent },
                            new Setter { Property = Frame.HasShadowProperty, Value = false }
                        }
                    });
                    Debug.WriteLine("Registered MessageBubbleUserFrame style globally");
                }

                // Add AI bubble style
                if (!Resources.TryGetValue("MessageBubbleAIFrame", out _))
                {
                    Resources.Add("MessageBubbleAIFrame", new Style(typeof(Frame))
                    {
                        Setters =
                        {
                            new Setter { Property = Frame.BackgroundColorProperty, Value = Application.Current?.RequestedTheme == AppTheme.Dark 
                                ? Color.FromArgb("#303030") 
                                : Color.FromArgb("#f0f0f0") },
                            new Setter { Property = Frame.CornerRadiusProperty, Value = 10 },
                            new Setter { Property = Frame.PaddingProperty, Value = new Thickness(12, 8) },
                            new Setter { Property = Frame.BorderColorProperty, Value = Colors.Transparent },
                            new Setter { Property = Frame.HasShadowProperty, Value = false }
                        }
                    });
                    Debug.WriteLine("Registered MessageBubbleAIFrame style globally");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error registering message bubble styles: {ex.Message}");
            }
        }
    }
}
