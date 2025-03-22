using System;
using System.Collections.Generic;
using NexusChat.Resources.Styles;

namespace NexusChat
{
    /// <summary>
    /// Centralized manager for application theme switching
    /// </summary>
    public static class ThemeManager {
        private const string THEME_PREFERENCE_KEY = "theme";

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
            themeName ??= "System";
            Preferences.Default.Set(THEME_PREFERENCE_KEY, themeName);

            // Resolve "System" theme to actual Light/Dark value
            string actualTheme = themeName;
            if (themeName == "System") {
                actualTheme = Application.Current?.PlatformAppTheme == AppTheme.Dark ? "Dark" : "Light";
                Application.Current.UserAppTheme = AppTheme.Unspecified;
            } else {
                Application.Current.UserAppTheme = themeName == "Dark" ? AppTheme.Dark : AppTheme.Light;
            }

            LoadThemeResources(actualTheme);
            ThemeChanged?.Invoke(null, actualTheme == "Dark");
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
        /// Loads the appropriate theme resource dictionary
        /// </summary>
        private static void LoadThemeResources(string themeName) {
            if (!MainThread.IsMainThread) {
                MainThread.BeginInvokeOnMainThread(() => LoadThemeResources(themeName));
                return;
            }

            var app = Application.Current;
            if (app == null) return;

            // Use the correct paths for theme dictionaries
            ResourceDictionary newTheme = themeName == "Dark" 
                ? new DarkTheme() 
                : new LightTheme();

            // Remove existing theme dictionaries
            var mergedDicts = app.Resources.MergedDictionaries;
            var dictionariesToRemove = new List<ResourceDictionary>();
            foreach (var dict in mergedDicts) {
                if (dict is DarkTheme || dict is LightTheme) {
                    dictionariesToRemove.Add(dict);
                }
            }
            
            foreach (var dict in dictionariesToRemove) {
                mergedDicts.Remove(dict);
            }

            // Add the new theme dictionary
            mergedDicts.Add(newTheme);
        }

        /// <summary>
        /// Initialize theme system based on preferences
        /// </summary>
        public static void Initialize() =>
            SetThemeByName(Preferences.Default.Get(THEME_PREFERENCE_KEY, "System"));
    }
}
