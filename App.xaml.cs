using NexusChat.Helpers;
using Microsoft.Maui.Controls;
using CommunityToolkit.Mvvm.Messaging;
using NexusChat.ViewModels;
using NexusChat.Resources.Styles;
using NexusChat.Data;

namespace NexusChat
{
    public partial class App : Application
    {
        private readonly IStartupInitializer _startupInitializer;
        
        public App(IStartupInitializer startupInitializer)
        {
            InitializeComponent();
            
            // Store the startup initializer
            _startupInitializer = startupInitializer;
            
            // Verify fonts are properly registered
            FontAwesomeHelper.VerifyFontAwesomeFonts();
            
            // Initialize theme system using the ThemeManager
            ThemeManager.Initialize();
            
            // Initialize database asynchronously
            InitializeAsync();
            
            MainPage = new AppShell();
        }
        
        private async void InitializeAsync()
        {
            await _startupInitializer.Initialize();
        }
    }
}
