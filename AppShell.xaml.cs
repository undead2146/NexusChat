using Microsoft.Extensions.DependencyInjection;
using NexusChat.Views.Pages.DevTools;
using NexusChat.Views.Pages;
using System;
using System.Diagnostics;
using NexusChat.Tests;

namespace NexusChat
{
    public partial class AppShell : Shell
    {
        private bool _isNavigating = false;
        
        public AppShell()
        {
            InitializeComponent();
            
            // Register all routes that aren't tab-based
            RegisterRoutes();
        }
        
        private void RegisterRoutes()
        {
            Debug.WriteLine("AppShell: Registering routes");
            
            try
            {
                // Unregister routes to avoid conflicts
                try 
                {
                    Routing.UnRegisterRoute(nameof(ThemesPage));
                    Routing.UnRegisterRoute(nameof(DatabaseViewerPage));
                    Routing.UnRegisterRoute(nameof(ModelTestingPage));
                    Routing.UnRegisterRoute(nameof(ChatPage));
                }
                catch { /* Ignore errors during unregistration */ }
                
                // Debug tools routes - register with explicit type references
                Routing.RegisterRoute(nameof(DatabaseViewerPage), typeof(DatabaseViewerPage));
                Routing.RegisterRoute(nameof(ModelTestingPage), typeof(ModelTestingPage));
                
                // Application pages
                Routing.RegisterRoute(nameof(ChatPage), typeof(ChatPage));
                
                Debug.WriteLine("Routes registered successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR registering routes: {ex.Message}");
            }
        }
    }
}
