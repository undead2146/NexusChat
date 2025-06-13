using NexusChat.Helpers;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
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
                
                // Initialize the XAML resources first without any custom colors
                InitializeComponent();
                
                // Apply default colors after XAML is loaded
                ApplyDefaultColors();
                
                // Register message bubble styles
                RegisterMessageBubbleStyles();
                
                // Initialize theme manager after resources are ready
                ThemeManager.Initialize();
                Debug.WriteLine("App: ThemeManager initialized");
                
                // Create and set the Shell with MainPage as the entry point
                MainPage = new AppShell(serviceProvider.GetRequiredService<INavigationService>());
                
                // Initialize other services in the background after UI is set up
                MainThread.BeginInvokeOnMainThread(async () => {
                    try
                    {
                        await Task.Delay(100);
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
                
                // Emergency fallback with minimal setup
                try
                {
                    if (Resources == null)
                    {
                        Resources = new ResourceDictionary();
                    }
                    
                    ApplyEmergencyColors();
                    
                    if (MainPage == null)
                    {
                        var navService = serviceProvider.GetRequiredService<INavigationService>();
                        MainPage = new AppShell(navService);
                    }
                }
                catch (Exception fallbackEx)
                {
                    Debug.WriteLine($"Emergency fallback also failed: {fallbackEx.Message}");
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
        /// Apply default colors after InitializeComponent to ensure consistent rendering
        /// </summary>
        private void ApplyDefaultColors()
        {
            try
            {
                if (Resources == null)
                {
                    Resources = new ResourceDictionary();
                }
                
                // Add critical colors for initial rendering
                var defaultColors = new Dictionary<string, Color>
                {
                    ["Primary"] = Color.FromArgb("#512BD4"),
                    ["PrimaryDark"] = Color.FromArgb("#7B68EE"),
                    ["Background"] = Color.FromArgb("#F9F9F9"),
                    ["BackgroundDark"] = Color.FromArgb("#121212"),
                    ["CardBackground"] = Color.FromArgb("#FFFFFF"),
                    ["CardBackgroundDark"] = Color.FromArgb("#252525"),
                    ["PrimaryTextColor"] = Color.FromArgb("#212121"),
                    ["PrimaryTextColorDark"] = Color.FromArgb("#EEEEEE"),
                    ["SecondaryTextColor"] = Color.FromArgb("#616161"),
                    ["SecondaryTextColorDark"] = Color.FromArgb("#B0B0B0"),
                    ["White"] = Colors.White,
                    ["Black"] = Colors.Black,
                    ["Gray100"] = Color.FromArgb("#F5F5F5"),
                    ["Gray200"] = Color.FromArgb("#EEEEEE"),
                    ["Gray600"] = Color.FromArgb("#757575"),
                    ["Gray800"] = Color.FromArgb("#424242"),
                    ["Gray900"] = Color.FromArgb("#212121"),
                    ["Gray950"] = Color.FromArgb("#121212"),
                    ["OffBlack"] = Color.FromArgb("#121212")
                };

                foreach (var colorPair in defaultColors)
                {
                    try
                    {
                        Resources[colorPair.Key] = colorPair.Value;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error setting color {colorPair.Key}: {ex.Message}");
                    }
                }
                
                Debug.WriteLine("App: Default colors applied successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error applying default colors: {ex.Message}");
            }
        }

        /// <summary>
        /// Emergency colors for critical fallback scenarios
        /// </summary>
        private void ApplyEmergencyColors()
        {
            try
            {
                Resources["Primary"] = Colors.Purple;
                Resources["Background"] = Colors.White;
                Resources["PrimaryTextColor"] = Colors.Black;
                Debug.WriteLine("Emergency colors applied");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Emergency colors failed: {ex.Message}");
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
                Color userBubbleBackgroundLight = Color.FromArgb("#e3f2fd");
                Color userBubbleBackgroundDark = Color.FromArgb("#0d47a1");
                Color aiBubbleBackgroundLight = Color.FromArgb("#f0f0f0");
                Color aiBubbleBackgroundDark = Color.FromArgb("#303030");
                
                // Add user bubble style using Border instead of Frame
                if (!Resources.TryGetValue("MessageBubbleUserBorder", out _))
                {
                    var style = new Style(typeof(Border));
                    style.Setters.Add(new Setter { Property = Border.BackgroundColorProperty, Value = new AppThemeBindingExtension 
                    { 
                        Light = userBubbleBackgroundLight,
                        Dark = userBubbleBackgroundDark
                    }});
                    style.Setters.Add(new Setter { Property = Border.StrokeShapeProperty, Value = new RoundRectangle { CornerRadius = new CornerRadius(10) } });
                    style.Setters.Add(new Setter { Property = Border.PaddingProperty, Value = new Thickness(12, 8) });
                    style.Setters.Add(new Setter { Property = Border.StrokeProperty, Value = Colors.Transparent });
                    
                    Resources.Add("MessageBubbleUserBorder", style);
                }

                // Add AI bubble style using Border instead of Frame
                if (!Resources.TryGetValue("MessageBubbleAIBorder", out _))
                {
                    var style = new Style(typeof(Border));
                    style.Setters.Add(new Setter { Property = Border.BackgroundColorProperty, Value = new AppThemeBindingExtension 
                    { 
                        Light = aiBubbleBackgroundLight,
                        Dark = aiBubbleBackgroundDark
                    }});
                    style.Setters.Add(new Setter { Property = Border.StrokeShapeProperty, Value = new RoundRectangle { CornerRadius = new CornerRadius(10) } });
                    style.Setters.Add(new Setter { Property = Border.PaddingProperty, Value = new Thickness(12, 8) });
                    style.Setters.Add(new Setter { Property = Border.StrokeProperty, Value = Colors.Transparent });
                    
                    Resources.Add("MessageBubbleAIBorder", style);
                }

                System.Diagnostics.Debug.WriteLine("App: Message bubble styles registered with Border (Frame constraint compliance)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error registering message bubble styles: {ex.Message}");
            }
        }
    }
}
