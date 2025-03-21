using System;
using Microsoft.Maui.Controls;

namespace NexusChat.Helpers
{
    public static class ThemeSwitcher
    {
        public static bool IsDarkTheme 
        { 
            get 
            {
                if (Application.Current == null)
                    return false;
                
                var requestedTheme = Application.Current.RequestedTheme;
                if (Application.Current.UserAppTheme != AppTheme.Unspecified)
                {
                    return Application.Current.UserAppTheme == AppTheme.Dark;
                }
                
                return requestedTheme == AppTheme.Dark;
            }
        }
        
        public static void SetTheme(bool isDark)
        {
            // Set the app's requested theme
            if (Application.Current != null)
            {
                Application.Current.UserAppTheme = isDark ? AppTheme.Dark : AppTheme.Light;
                
                // Force theme update by toggling a resources change
                var resources = Application.Current.Resources;
                var temp = resources.TryGetValue("Primary", out var _);
            }
        }
        
        public static void ToggleTheme()
        {
            SetTheme(!IsDarkTheme);
        }
    }
}
