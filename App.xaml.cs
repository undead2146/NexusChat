using NexusChat.Helpers;

namespace NexusChat
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            
            // Verify fonts are properly registered
            FontAwesomeHelper.VerifyFontAwesomeFonts();
            
            MainPage = new AppShell();
        }
    }
}
