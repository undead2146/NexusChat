using Microsoft.Maui.Controls;
using NexusChat.Helpers;

namespace NexusChat.Views
{
    public partial class ThemeTestPage : ContentPage
    {
        public ThemeTestPage()
        {
            InitializeComponent();
            UpdateThemeToggleText();
        }
        
        private void OnThemeToggleClicked(object sender, EventArgs e)
        {
            // Toggle between light and dark themes
            Application.Current.UserAppTheme = Application.Current.UserAppTheme == AppTheme.Light 
                ? AppTheme.Dark 
                : AppTheme.Light;
                
            // Update toggle button text
            UpdateThemeToggleText();
            
            // Force page refresh to ensure theme changes are applied
            ForcePageRefresh();
        }
        
        private async void ForcePageRefresh()
        {
            // Force UI refresh by toggling visibility
            this.Opacity = 0.99;
            await Task.Delay(50);
            this.Opacity = 1.0;
            
            // Update the background color explicitly
            var backgroundColor = Application.Current.UserAppTheme == AppTheme.Dark 
                ? Color.FromArgb("#212529")  // DarkTheme Background
                : Color.FromArgb("#f8f9fa"); // LightTheme Background
                
            this.BackgroundColor = backgroundColor;
            
            // Force layout update - properly refresh the UI
            this.ForceLayout();
        }
        
        private void UpdateThemeToggleText()
        {
            MainThread.BeginInvokeOnMainThread(() => 
            {
                bool isDark = Application.Current.UserAppTheme == AppTheme.Dark;
                
                ThemeToggleButton.Text = isDark 
                    ? "Switch to Light Theme" 
                    : "Switch to Dark Theme";
                
                // Update the theme icon
                ThemeIconLabel.Text = isDark 
                    ? "\uf185" // Sun icon for light theme option
                    : "\uf186"; // Moon icon for dark theme option
            });
        }
    }
}
