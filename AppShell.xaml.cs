using Microsoft.Extensions.DependencyInjection;
using NexusChat.Views.Pages.DevTools;
using NexusChat.Views.Pages;
using NexusChat.Services;
using System;
using System.Diagnostics;
using NexusChat.Tests;

namespace NexusChat
{
    public partial class AppShell : Shell
    {
        private readonly NavigationService _navigationService;
        
        public AppShell(NavigationService navigationService)
        {
            InitializeComponent();
            
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            
            // Register routes for navigation
            _navigationService.RegisterRoutes();
        }
    }
}
