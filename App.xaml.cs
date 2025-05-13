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
using Microsoft.Maui;
using NexusChat.Views.Pages;

namespace NexusChat
{
    public partial class App : Microsoft.Maui.Controls.Application
    {
        private readonly IServiceProvider _serviceProvider;

        public App(IServiceProvider serviceProvider)
        {
            try
            {
                _serviceProvider = serviceProvider;
                
                Debug.WriteLine("App: Starting initialization sequence");
                
                // Apply default colors before any UI components are created
                // This ensures colors are available during XAML parsing
                ApplyDefaultColors();
                
                // Initialize the XAML resources
                InitializeComponent();
                
                // Register message bubble styles
                RegisterMessageBubbleStyles();
                
                ThemeManager.Initialize();
                Debug.WriteLine("App: ThemeManager initialized directly");
                
                // Create and set the Shell with MainPage as the entry point
                MainPage = new AppShell(serviceProvider.GetRequiredService<INavigationService>());
                
                // Initialize other services in the background after UI is set up
                MainThread.BeginInvokeOnMainThread(async () => {
                    try
                    {
                        await Task.Delay(100); // Small delay to allow UI to render
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
                
                // Emergency fallback
                if (MainPage == null)
                {
                    var navService = serviceProvider.GetRequiredService<INavigationService>();
                    MainPage = new AppShell(navService);
                }
            }
        }

        protected override void OnStart()
        {
            base.OnStart();
            
            // Initialize the ServiceLocator with the app's service provider
            ServiceLocator.Initialize(IPlatformApplication.Current.Services);
        }

        /// <summary>
        /// Apply default colors before InitializeComponent to ensure consistent rendering
        /// </summary>
        private void ApplyDefaultColors()
        {
            try
            {
                // Create a minimal application resources dictionary with essential colors
                if (Resources == null)
                {
                    Resources = new ResourceDictionary();
                }
                
                // Add critical colors for initial rendering
                Resources["Primary"] = Color.FromArgb("#512BD4");
                Resources["PrimaryDark"] = Color.FromArgb("#7B68EE");
                Resources["Background"] = Color.FromArgb("#F9F9F9");
                Resources["BackgroundDark"] = Color.FromArgb("#121212");
                Resources["CardBackground"] = Color.FromArgb("#FFFFFF");
                Resources["CardBackgroundDark"] = Color.FromArgb("#252525");
                Resources["PrimaryTextColor"] = Color.FromArgb("#212121");
                Resources["PrimaryTextColorDark"] = Color.FromArgb("#EEEEEE");
                Resources["SecondaryTextColor"] = Color.FromArgb("#616161");
                Resources["SecondaryTextColorDark"] = Color.FromArgb("#B0B0B0");
                Resources["White"] = Colors.White;
                Resources["Black"] = Colors.Black;
                
                // Added for compatibility with styles
                Resources["Gray100"] = Color.FromArgb("#F5F5F5");
                Resources["Gray200"] = Color.FromArgb("#EEEEEE");
                Resources["Gray600"] = Color.FromArgb("#757575");
                Resources["Gray800"] = Color.FromArgb("#424242");
                Resources["Gray900"] = Color.FromArgb("#212121");
                Resources["Gray950"] = Color.FromArgb("#121212");
                Resources["OffBlack"] = Color.FromArgb("#121212");
                
                Debug.WriteLine("App: Default colors applied successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error applying default colors: {ex.Message}");
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
                    // Use InitializeAsync instead of Initialize
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
