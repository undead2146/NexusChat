using NexusChat.Views;

namespace NexusChat
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            
            // Register routes
            Routing.RegisterRoute(nameof(ThemeTestPage), typeof(ThemeTestPage));
            Routing.RegisterRoute(nameof(IconTestPage), typeof(IconTestPage));
            Routing.RegisterRoute(nameof(ModelTestingPage), typeof(ModelTestingPage));
        }
    }
}
