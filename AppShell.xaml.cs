using System;
using Microsoft.Maui.Controls;
using NexusChat.Services.Interfaces;
using NexusChat.Views.Pages;
using NexusChat.Views.Pages.DevTools;

namespace NexusChat
{
    public partial class AppShell : Shell
    {
        private readonly INavigationService _navigationService;

        public AppShell(INavigationService navigationService)
        {
            InitializeComponent();
            
            _navigationService = navigationService;
            
            // Register routes for navigation
            RegisterRoutes();
        }

        private void RegisterRoutes()
        {
            // Register MainPage as a route (even though it's the default)
            Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
            
            // Register routes for all pages that will be navigated to
            Routing.RegisterRoute(nameof(ChatPage), typeof(ChatPage));
            Routing.RegisterRoute(nameof(AIModelsPage), typeof(AIModelsPage));
            
            // Dev tools routes
            Routing.RegisterRoute(nameof(DatabaseViewerPage), typeof(DatabaseViewerPage));
            Routing.RegisterRoute(nameof(ModelTestingPage), typeof(ModelTestingPage));
            Routing.RegisterRoute(nameof(ThemesPage), typeof(ThemesPage));
            
            // Let the navigation service know routes are registered
            _navigationService.RegisterRoutes();
        }
    }
}
