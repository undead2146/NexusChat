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
            try
            {
                // First initialize the XAML resources
                InitializeComponent();
                
                // Register styles immediately - these are needed for AppShell
                RegisterMessageBubbleStyles();
                
                // Create and set the Shell - MUST be done before any initialization
                MainPage = new AppShell();
                
                // Initialize theme system
                ThemeManager.Initialize();
                
                // Initialize other services in the background AFTER MainPage is set
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        await InitializeServicesAsync();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error during async initialization: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Critical error in App constructor: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Emergency fallback - ensure MainPage is set
                if (MainPage == null)
                {
                    MainPage = new AppShell();
                }
            }
        }

        private async Task InitializeServicesAsync()
        {
            try
            {
                // Add a delay to ensure the app is fully initialized
                await Task.Delay(100);
                
                // Get the startup initializer from the service provider
                var initializer = Handler?.MauiContext?.Services.GetService<IStartupInitializer>();
                
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
                // Create safer styles without using Application.Current in constructor
                Color userBubbleBackgroundLight = Color.FromArgb("#e3f2fd");
                Color userBubbleBackgroundDark = Color.FromArgb("#0d47a1");
                Color aiBubbleBackgroundLight = Color.FromArgb("#f0f0f0");
                Color aiBubbleBackgroundDark = Color.FromArgb("#303030");
                
                // Add user bubble style
                if (!Resources.TryGetValue("MessageBubbleUserFrame", out _))
                {
                    var style = new Style(typeof(Frame));
                    style.Setters.Add(new Setter { Property = Frame.BackgroundColorProperty, Value = new AppThemeBindingExtension 
                    { 
                        Light = userBubbleBackgroundLight,
                        Dark = userBubbleBackgroundDark
                    }});
                    style.Setters.Add(new Setter { Property = Frame.CornerRadiusProperty, Value = 10 });
                    style.Setters.Add(new Setter { Property = Frame.PaddingProperty, Value = new Thickness(12, 8) });
                    style.Setters.Add(new Setter { Property = Frame.BorderColorProperty, Value = Colors.Transparent });
                    style.Setters.Add(new Setter { Property = Frame.HasShadowProperty, Value = false });
                    
                    Resources.Add("MessageBubbleUserFrame", style);
                    Debug.WriteLine("Registered MessageBubbleUserFrame style globally");
                }

                // Add AI bubble style
                if (!Resources.TryGetValue("MessageBubbleAIFrame", out _))
                {
                    var style = new Style(typeof(Frame));
                    style.Setters.Add(new Setter { Property = Frame.BackgroundColorProperty, Value = new AppThemeBindingExtension 
                    { 
                        Light = aiBubbleBackgroundLight,
                        Dark = aiBubbleBackgroundDark
                    }});
                    style.Setters.Add(new Setter { Property = Frame.CornerRadiusProperty, Value = 10 });
                    style.Setters.Add(new Setter { Property = Frame.PaddingProperty, Value = new Thickness(12, 8) });
                    style.Setters.Add(new Setter { Property = Frame.BorderColorProperty, Value = Colors.Transparent });
                    style.Setters.Add(new Setter { Property = Frame.HasShadowProperty, Value = false });
                    
                    Resources.Add("MessageBubbleAIFrame", style);
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
