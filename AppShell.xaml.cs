using NexusChat.Views;

namespace NexusChat
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(ThemeTestPage), typeof(ThemeTestPage));
            Routing.RegisterRoute(nameof(IconTestPage), typeof(IconTestPage));
        }
    }
}
