using NexusChat.Helpers;
using Microsoft.Maui.Controls;
using CommunityToolkit.Mvvm.Messaging;
using NexusChat.ViewModels;
using NexusChat.Resources.Styles;

namespace NexusChat
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            
            // Verify fonts are properly registered
            FontAwesomeHelper.VerifyFontAwesomeFonts();
            
            // Initialize theme system using the ThemeManager
            ThemeManager.Initialize();
            
            MainPage = new AppShell();
        }
    }
}
