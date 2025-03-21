using System;
using Microsoft.Maui.Controls;

namespace NexusChat.Helpers
{
    public static class ThemeHelper
    {
        private const string LightTheme = "LightTheme";
        private const string DarkTheme = "DarkTheme";
        
        public static bool IsDarkTheme { get; private set; }
        
        public static void SetTheme(bool isDark)
        {
            IsDarkTheme = isDark;
            
            if (Application.Current == null)
                return;
                
            ICollection<ResourceDictionary> mergedDictionaries = Application.Current.Resources.MergedDictionaries;
            
            // Remove both themes to avoid duplication
            var lightTheme = mergedDictionaries.FirstOrDefault(d => d.Source?.OriginalString?.EndsWith("LightTheme.xaml") == true);
            var darkTheme = mergedDictionaries.FirstOrDefault(d => d.Source?.OriginalString?.EndsWith("DarkTheme.xaml") == true);
            
            if (lightTheme != null)
                mergedDictionaries.Remove(lightTheme);
                
            if (darkTheme != null)
                mergedDictionaries.Remove(darkTheme);
                
            // Add the selected theme using MauiAppBuilder's loading approach
            var themeName = isDark ? DarkTheme : LightTheme;
            var resourcePath = $"{themeName}.xaml";
            
            ResourceDictionary resourceDictionary = new ResourceDictionary();
            var loadableComponent = new ResourceLoader(resourcePath);
            loadableComponent.LoadResources(resourceDictionary);
            mergedDictionaries.Add(resourceDictionary);
        }
        
        public static void ToggleTheme()
        {
            SetTheme(!IsDarkTheme);
        }
        
        // Helper class to load XAML resources
        private class ResourceLoader
        {
            private readonly string _resourcePath;
            
            public ResourceLoader(string resourcePath)
            {
                _resourcePath = resourcePath;
            }
            
            public void LoadResources(ResourceDictionary resourceDictionary)
            {
                // Try to load the resource directly
                try
                {
                    var resourceUri = new Uri($"Resources/Styles/Themes/{_resourcePath}", UriKind.Relative);
                    var loadedRD = new ResourceDictionary { Source = resourceUri };
                    foreach (var key in loadedRD.Keys)
                    {
                        resourceDictionary[key] = loadedRD[key];
                    }
                }
                catch
                {
                    // Alternative approach if direct loading fails
                    AddHardcodedTheme(resourceDictionary, _resourcePath.Contains("Dark"));
                }
            }
            
            // Backup approach if XAML loading fails
            private void AddHardcodedTheme(ResourceDictionary resourceDictionary, bool isDark)
            {
                // Add basic color scheme if loading fails
                if (isDark)
                {
                    resourceDictionary["Background"] = Colors.Black;
                    resourceDictionary["Foreground"] = Colors.White;
                }
                else
                {
                    resourceDictionary["Background"] = Colors.White;
                    resourceDictionary["Foreground"] = Colors.Black;
                }
            }
        }
    }
}
