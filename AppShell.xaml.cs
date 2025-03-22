using Microsoft.Extensions.DependencyInjection;
using NexusChat.Views;
using System;
using System.Diagnostics;

namespace NexusChat
{
    public partial class AppShell : Shell
    {
        private bool _isNavigating = false;
        
        public AppShell()
        {
            InitializeComponent();
            
            // Register routes
            Routing.RegisterRoute(nameof(ThemeTestPage), typeof(ThemeTestPage));
            Routing.RegisterRoute(nameof(IconTestPage), typeof(IconTestPage));
            Routing.RegisterRoute(nameof(ModelTestingPage), typeof(ModelTestingPage));
            
            // Register DatabaseViewerPage WITHOUT custom navigation handler
            try
            {
                Routing.RegisterRoute(nameof(DatabaseViewerPage), typeof(DatabaseViewerPage));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error registering route: {ex.Message}");
            }
        }
        
        // Remove the custom navigation handler that was causing issues
        // We'll let the normal Shell navigation work as intended
    }
}
