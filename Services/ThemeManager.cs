using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using NexusChat.Resources.Styles;

namespace NexusChat.Services
{
    /// <summary>
    /// Centralized manager for application theme switching
    /// </summary>
    public static class ThemeManager {
        private const string THEME_PREFERENCE_KEY = "theme";
        private static bool _isChangingTheme = false;
        private static readonly object _themeLock = new object();
        // Cache theme dictionaries to prevent repeatedly creating new instances
        private static readonly Dictionary<string, ResourceDictionary> _cachedThemes = new Dictionary<string, ResourceDictionary>();

        /// <summary>
        /// Event triggered when theme changes (isDark)
        /// </summary>
        public static event EventHandler<bool> ThemeChanged;

        /// <summary>
        /// Gets a value indicating whether the current theme is dark
        /// </summary>
        public static bool IsDarkTheme => GetCurrentTheme() == "Dark";

        /// <summary>
        /// Toggles between light and dark themes
        /// </summary>
        public static void ToggleTheme() =>
            SetThemeByName(IsDarkTheme ? "Light" : "Dark");

        /// <summary>
        /// Sets the application theme to light or dark mode
        /// </summary>
        public static void SetTheme(bool isDark) =>
            SetThemeByName(isDark ? "Dark" : "Light");

        /// <summary>
        /// Sets theme by name: "Light", "Dark", or "System"
        /// </summary>
        public static void SetThemeByName(string themeName) {
            if (_isChangingTheme)
            {
                Debug.WriteLine("Theme change already in progress");
                return;
            }
            
            lock (_themeLock)
            {
                try
                {
                    _isChangingTheme = true;
                    Debug.WriteLine($"Changing theme to: {themeName}");

                    // Apply the theme setting
                    if (Application.Current != null)
                    {
                        // Set user theme preference
                        Application.Current.UserAppTheme = themeName switch
                        {
                            "Dark" => AppTheme.Dark,
                            "Light" => AppTheme.Light,
                            _ => AppTheme.Unspecified
                        };

                        // Save preference
                        Preferences.Default.Set(THEME_PREFERENCE_KEY, themeName);
                        
                        // Determine actual theme for event
                        bool isDark = themeName == "Dark" || 
                            (themeName == "System" && Application.Current.PlatformAppTheme == AppTheme.Dark);

                        // Notify subscribers
                        MainThread.BeginInvokeOnMainThread(() => {
                            ThemeChanged?.Invoke(null, isDark);
                        });
                        
                        // Apply theme dictionary
                        MainThread.BeginInvokeOnMainThread(() => {
                            ApplyThemeDictionarySimple(
                                GetThemeDictionary(isDark ? "Dark" : "Light"),
                                isDark ? "Dark" : "Light"
                            );
                        });
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error setting theme: {ex.Message}");
                }
                finally
                {
                    _isChangingTheme = false;
                }
            }
        }

        /// <summary>
        /// Gets a cached theme dictionary or creates a new one
        /// </summary>
        private static ResourceDictionary GetThemeDictionary(string themeName)
        {
            if (!_cachedThemes.TryGetValue(themeName, out ResourceDictionary theme))
            {
                theme = themeName == "Dark" ? new DarkTheme() : new LightTheme();
                _cachedThemes[themeName] = theme;
            }
            return theme;
        }

        /// <summary>
        /// Applies theme dictionary with absolute minimal UI operations
        /// </summary>
        private static void ApplyThemeDictionarySimple(ResourceDictionary newTheme, string themeName)
        {
            if (Application.Current?.Resources == null) return;
            
            var mergedDicts = Application.Current.Resources.MergedDictionaries;
            
            // Find and remove existing theme dictionary
            ResourceDictionary existingTheme = null;
            foreach (var dict in mergedDicts)
            {
                if (dict is DarkTheme || dict is LightTheme)
                {
                    existingTheme = dict;
                    break;
                }
            }
            
            // Perform the minimal required operations
            if (existingTheme != null)
            {
                mergedDicts.Remove(existingTheme);
            }
            
            mergedDicts.Add(newTheme);
            
            // Queue minimal UI refresh
            MainThread.BeginInvokeOnMainThread(() => RefreshCurrentPage());
        }
        
        /// <summary>
        /// Refreshes only the current visible page
        /// </summary>
        private static void RefreshCurrentPage()
        {
            if (Application.Current?.MainPage == null) return;
            
            // Only force layout on the current page
            if (Application.Current.MainPage is Shell shell)
            {
                shell.CurrentPage?.ForceLayout();
            }
            else if (Application.Current.MainPage is NavigationPage navPage)
            {
                navPage.CurrentPage?.ForceLayout();
            }
            else
            {
                Application.Current.MainPage.ForceLayout();
            }
        }

        /// <summary>
        /// Gets the current theme name
        /// </summary>
        public static string GetCurrentTheme() {
            string storedTheme = Preferences.Default.Get(THEME_PREFERENCE_KEY, "System");

            return storedTheme == "System"
                ? Application.Current?.PlatformAppTheme == AppTheme.Dark ? "Dark" : "Light"
                : storedTheme;
        }

        /// <summary>
        /// Initialize theme system based on preferences
        /// </summary>
        public static void Initialize() 
        {
            try 
            {
                // Make sure all required resources exist
                EnsureRequiredResourcesExist();
                
                // Get saved theme preference
                string themeName = Preferences.Default.Get(THEME_PREFERENCE_KEY, "System");
                
                // Apply the saved theme
                string resolvedTheme = themeName == "System"
                    ? (Application.Current?.PlatformAppTheme == AppTheme.Dark ? "Dark" : "Light")
                    : themeName;
                
                // Apply theme dictionary
                var themeDictionary = GetThemeDictionary(resolvedTheme);
                ApplyThemeDictionarySimple(themeDictionary, resolvedTheme);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing theme: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Ensures all required theme resources exist
        /// </summary>
        private static void EnsureRequiredResourcesExist()
        {
            if (Application.Current?.Resources == null) return;
            
            // Light theme resources
            EnsureResource("Primary", Colors.Purple);
            EnsureResource("Secondary", Colors.LavenderBlush);
            EnsureResource("Tertiary", Colors.MidnightBlue);
            EnsureResource("Background", Colors.White);
            EnsureResource("CardBackground", Colors.WhiteSmoke);
            EnsureResource("SurfaceBackground", Colors.White);
            EnsureResource("PrimaryTextColor", Colors.Black);
            EnsureResource("SecondaryTextColor", Colors.DarkGray);
            
            // Dark theme resources
            EnsureResource("PrimaryDark", Color.FromArgb("#9982EA"));
            EnsureResource("SecondaryDark", Color.FromArgb("#625B71"));
            EnsureResource("TertiaryDark", Color.FromArgb("#A09FFF"));
            EnsureResource("BackgroundDark", Color.FromArgb("#121212"));
            EnsureResource("CardBackgroundDark", Color.FromArgb("#1E1E1E"));
            EnsureResource("SurfaceBackgroundDark", Color.FromArgb("#1E1E1E"));
            EnsureResource("PrimaryTextColorDark", Colors.White);
            EnsureResource("SecondaryTextColorDark", Color.FromArgb("#B3B3B3"));
            
            // Common values
            EnsureResource("MessageBubbleWidth", 280.0);
        }
        
        /// <summary>
        /// Ensures a resource exists, adding it if missing
        /// </summary>
        private static void EnsureResource(string key, object defaultValue)
        {
            var resources = Application.Current?.Resources;
            if (resources != null && !resources.ContainsKey(key))
            {
                resources[key] = defaultValue;
                Debug.WriteLine($"Added missing resource: {key}");
            }
        }
    }
}
