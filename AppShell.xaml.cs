using System;
using Microsoft.Maui.Controls;
using NexusChat.Services.Interfaces;
using NexusChat.Views.Pages;
using System.Diagnostics;

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
            try
            {
                // Register routes with unique names to avoid conflicts
                Routing.RegisterRoute("ChatPage", typeof(ChatPage));
                Routing.RegisterRoute("AIModelsPage", typeof(AIModelsPage));
                
                Debug.WriteLine("AppShell: Routes registered successfully");
                Debug.WriteLine($"Registered routes: ChatPage -> {typeof(ChatPage).Name}");
                Debug.WriteLine($"Registered routes: AIModelsPage -> {typeof(AIModelsPage).Name}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error registering routes: {ex.Message}");
            }
            
            // Notify navigation service that routes are registered
            _navigationService.RegisterRoutes();
            
            Debug.WriteLine("AppShell: All routes registered successfully");
        }
    }
}
