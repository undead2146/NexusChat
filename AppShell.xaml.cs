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
            
            // Register routes for navigation
            Routing.RegisterRoute(nameof(ThemesPage), typeof(ThemesPage));
            Routing.RegisterRoute(nameof(ModelTestingPage), typeof(ModelTestingPage));
            Routing.RegisterRoute(nameof(DatabaseViewerPage), typeof(DatabaseViewerPage));
            Routing.RegisterRoute(nameof(ChatPage), typeof(ChatPage));
            
        }
    }
}
