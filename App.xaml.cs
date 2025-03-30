using NexusChat.Helpers;
using Microsoft.Maui.Controls;
using CommunityToolkit.Mvvm.Messaging;
using NexusChat.Core.ViewModels;
using NexusChat.Resources.Styles;
using NexusChat.Data.Context;
using NexusChat.Services;
using NexusChat.Services.Interfaces; 
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace NexusChat
{
    public partial class App : Application
    {
        public App(IServiceProvider serviceProvider)
        {
            try
            {
                // First initialize the XAML resources
                InitializeComponent();
                
                // Register message bubble styles
                RegisterMessageBubbleStyles();
                
                // Create and set the Shell - MUST be done before any initialization
                MainPage = serviceProvider.GetRequiredService<AppShell>();
                
                // Initialize theme system
                ThemeManager.Initialize();
                
                // Initialize other services in the background AFTER MainPage is set
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        await InitializeServicesAsync(serviceProvider);
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
                    MainPage = serviceProvider.GetRequiredService<AppShell>();
                }
            }
        }

        private async Task InitializeServicesAsync(IServiceProvider serviceProvider)
        {
            try
            {
                // Add a delay to ensure the app is fully initialized
                await Task.Delay(100);
                
                // Get all startup initializers from the service provider
                var initializers = serviceProvider.GetServices<IStartupInitializer>();
                
                foreach (var initializer in initializers)
                {
                    await initializer.InitializeAsync();
                }
                
                Debug.WriteLine("App startup initialization completed successfully");
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
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error registering message bubble styles: {ex.Message}");
            }
        }
    }
}
