using System;
using Microsoft.Maui.Controls;
using NexusChat.Services.Interfaces;
using NexusChat.Views.Pages;

namespace NexusChat
{
    public partial class AppShell : Shell
    {
        private readonly INavigationService _navigationService;

        public AppShell(INavigationService navigationService)
        {
            InitializeComponent();
            
            _navigationService = navigationService;
            
            // Centralized route registration - single source of truth
            RegisterAllRoutes();
        }

        /// <summary>
        /// Centralized route registration for all application pages
        /// This is the single, authoritative location for route definitions
        /// </summary>
        private void RegisterAllRoutes()
        {
            // Main application pages
            Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
            Routing.RegisterRoute(nameof(ChatPage), typeof(ChatPage));
            Routing.RegisterRoute(nameof(AIModelsPage), typeof(AIModelsPage));
            
            // Notify navigation service that routes are registered
            _navigationService.RegisterRoutes();
            
            System.Diagnostics.Debug.WriteLine("AppShell: All routes registered successfully");
        }
    }
}
