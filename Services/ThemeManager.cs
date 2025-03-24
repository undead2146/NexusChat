using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using NexusChat.Resources.Styles;

namespace NexusChat
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
                            _ => AppTheme.Unspecified // System
                        };

                        // Save preference
                        Preferences.Default.Set(THEME_PREFERENCE_KEY, themeName);
                        
                        // Determine actual theme for event
                        bool isDark = themeName == "Dark" || 
                            (themeName == "System" && Application.Current.PlatformAppTheme == AppTheme.Dark);

                        // Notify subscribers 
                        MainThread.BeginInvokeOnMainThread(() => {
                            ThemeChanged?.Invoke(null, isDark);
                            
                            // Refresh visible UI
                            ForceUIRefresh();
                            
                            _isChangingTheme = false;
                        });
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error setting theme: {ex.Message}");
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
            
            // Notify theme change
            ThemeChanged?.Invoke(null, themeName == "Dark");
            
            // Queue minimal UI refresh
            Device.BeginInvokeOnMainThread(() => RefreshCurrentPage());
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
        /// Resolves theme name to actual theme based on system settings
        /// </summary>
        private static string ResolveActualTheme(string themeName)
        {
            if (themeName == "System") {
                return Application.Current?.PlatformAppTheme == AppTheme.Dark ? "Dark" : "Light";
            }
            return themeName ?? "System";
        }

        /// <summary>
        /// Applies theme dictionary with minimal UI operations
        /// </summary>
        private static void ApplyThemeDictionary(ResourceDictionary newTheme, string themeName)
        {
            Debug.WriteLine($"ThemeManager: Applying {themeName} theme dictionary");
            
            var app = Application.Current;
            if (app == null) return;
            
            var mergedDicts = app.Resources.MergedDictionaries;
            
            // Find existing theme dictionary using efficient search
            ResourceDictionary existingTheme = null;
            foreach (var dict in mergedDicts)
            {
                if (dict is DarkTheme || dict is LightTheme)
                {
                    existingTheme = dict;
                    break;
                }
            }
            
            // Simple remove and add - don't do other operations during this
            if (existingTheme != null)
            {
                mergedDicts.Remove(existingTheme);
            }
            
            mergedDicts.Add(newTheme);
            
            // Notify theme change
            ThemeChanged?.Invoke(null, themeName == "Dark");
            
            // Optimize UI refresh - do in separate operation
            Device.BeginInvokeOnMainThread(async () => {
                await Task.Delay(10);
                MinimalUIRefresh();
            });
        }

        /// <summary>
        /// Performs absolute minimum UI refresh required for theme change
        /// </summary>
        private static void MinimalUIRefresh()
        {
            try
            {
                Debug.WriteLine("Performing minimal UI refresh");
                
                // Find current displayed page only
                if (Application.Current?.MainPage is Shell shell && shell.CurrentPage != null)
                {
                    shell.CurrentPage.ForceLayout();
                }
                else if (Application.Current?.MainPage is NavigationPage navPage && navPage.CurrentPage != null)
                {
                    navPage.CurrentPage.ForceLayout();
                }
                else if (Application.Current?.MainPage != null)
                {
                    Application.Current.MainPage.ForceLayout();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in MinimalUIRefresh: {ex.Message}");
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
        /// Optimized theme resource loading
        /// </summary>
        private static void OptimizedThemeLoading(string themeName)
        {
            Debug.WriteLine($"ThemeManager: Beginning optimized theme loading for {themeName}");
            
            // Create the theme dictionary on the background thread if possible
            ResourceDictionary newTheme = null;
            if (!_cachedThemes.TryGetValue(themeName, out newTheme))
            {
                Debug.WriteLine("ThemeManager: Creating new theme dictionary");
                newTheme = themeName == "Dark" ? new DarkTheme() : new LightTheme();
                _cachedThemes[themeName] = newTheme;
            }
            else
            {
                Debug.WriteLine("ThemeManager: Using cached theme dictionary");
            }
            
            // Only modify UI on the main thread
            MainThread.BeginInvokeOnMainThread(() => {
                try {
                    var app = Application.Current;
                    if (app == null) return;
                    
                    var mergedDicts = app.Resources.MergedDictionaries;
                    
                    // Find existing theme dictionary using efficient search
                    ResourceDictionary existingTheme = null;
                    foreach (var dict in mergedDicts)
                    {
                        if (dict is DarkTheme || dict is LightTheme)
                        {
                            existingTheme = dict;
                            break;
                        }
                    }
                    
                    // Update resource dictionary - remove old, add new with minimal operations
                    if (existingTheme != null)
                    {
                        Debug.WriteLine("ThemeManager: Removing existing theme dictionary");
                        mergedDicts.Remove(existingTheme);
                    }
                    
                    Debug.WriteLine("ThemeManager: Adding new theme dictionary");
                    mergedDicts.Add(newTheme);
                    
                    // Notify theme change
                    ThemeChanged?.Invoke(null, themeName == "Dark");
                    
                    // Apply to current page only for better performance
                    LightweightUIRefresh();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in OptimizedThemeLoading: {ex.Message}");
                }
            });
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

            try {
                // Get or create the theme dictionary using our cache
                if (!_cachedThemes.TryGetValue(themeName, out ResourceDictionary newTheme))
                {
                    // Create new theme dictionary only if not cached
                    newTheme = themeName == "Dark" 
                        ? new DarkTheme() 
                        : new LightTheme();
                    _cachedThemes[themeName] = newTheme;
                }

                var mergedDicts = app.Resources.MergedDictionaries;
                
                // Find existing theme dictionary
                ResourceDictionary existingTheme = null;
                foreach (var dict in mergedDicts)
                {
                    if (dict is DarkTheme || dict is LightTheme)
                    {
                        existingTheme = dict;
                        break;
                    }
                }

                // Replace existing theme or add new one
                if (existingTheme != null)
                {
                    // Remove and add to replace (can't use indexing with ICollection)
                    mergedDicts.Remove(existingTheme);
                    mergedDicts.Add(newTheme);
                }
                else
                {
                    mergedDicts.Add(newTheme);
                }
                
                // Force UI refresh to apply themes to all controls
                ForceUIRefresh();
            }
            catch (Exception ex) {
                Debug.WriteLine($"Error loading theme resources: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Optimized UI refresh after theme change
        /// </summary>
        private static void OptimizedUIRefresh()
        {
            // Queue this after the theme change has been applied
            MainThread.BeginInvokeOnMainThread(async () => {
                try
                {
                    // Only refresh current visible page
                    if (Application.Current?.MainPage == null) return;
                    
                    // Short delay to ensure the theme changes are applied
                    await Task.Delay(10);
                    
                    // Get current visible page in a Shell application
                    Page currentPage = null;
                    
                    if (Application.Current.MainPage is Shell shell)
                    {
                        currentPage = shell.CurrentPage;
                    }
                    else if (Application.Current.MainPage is NavigationPage navPage)
                    {
                        currentPage = navPage.CurrentPage;
                    }
                    else
                    {
                        currentPage = Application.Current.MainPage;
                    }
                    
                    // Only force layout on the current page
                    currentPage?.ForceLayout();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in OptimizedUIRefresh: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Forces UI elements to refresh when theme changes
        /// </summary>
        private static void ForceUIRefresh()
        {
            try
            {
                // Always run on main thread
                MainThread.BeginInvokeOnMainThread(async () => {
                    try
                    {
                        // Allow a brief moment for resources to update
                        await Task.Delay(50);
                        
                        // Get current page to refresh
                        var mainPage = Application.Current?.MainPage;
                        if (mainPage == null) return;
            
                        Debug.WriteLine("ThemeManager: Refreshing UI for theme change");
                        
                        // Refresh Shell and all visible pages
                        if (mainPage is Shell shell)
                        {
                            Debug.WriteLine("Refreshing Shell and current page");
                            shell.ForceLayout();
                            
                            // Force layout update for current page
                            if (shell.CurrentPage != null)
                            {
                                shell.CurrentPage.ForceLayout();
                            }
                            
                            // Get all currently in-memory pages
                            foreach (var item in shell.Items)
                            {
                                try {
                                    if (item.CurrentItem?.CurrentItem?.Content is Page page)
                                    {
                                        page.ForceLayout();
                                    }
                                } catch (Exception ex) {
                                    Debug.WriteLine($"Error refreshing Shell item: {ex.Message}");
                                }
                            }
                        }
                        else if (mainPage is NavigationPage navPage)
                        {
                            // Refresh NavigationPage and its stack
                            navPage.ForceLayout();
                            if (navPage.CurrentPage != null)
                            {
                                navPage.CurrentPage.ForceLayout();
                            }
                            
                            // Force refresh of navigation bar
                            navPage.BarBackgroundColor = navPage.BarBackgroundColor;
                        }
                        else
                        {
                            // Direct refresh of main page
                            mainPage.ForceLayout();
                        }
                        
                        // Fire events again after UI refresh to ensure components update
                        await Task.Delay(100);
                        ThemeChanged?.Invoke(null, IsDarkTheme);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in ThemeManager.ForceUIRefresh: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error refreshing UI: {ex.Message}");
            }
        }

        /// <summary>
        /// Performs a lightweight UI refresh after theme changes
        /// </summary>
        private static void LightweightUIRefresh()
        {
            MainThread.BeginInvokeOnMainThread(async () => {
                try
                {
                    Debug.WriteLine("ThemeManager: Performing lightweight UI refresh");
                    
                    // Short delay to let the theme resources apply
                    await Task.Delay(10);
                    
                    // Just force layout on the current visible content
                    if (Application.Current?.MainPage is Shell shell)
                    {
                        // Only refresh the current displayed page
                        Page currentPage = shell.CurrentPage;
                        currentPage?.ForceLayout();
                        
                        Debug.WriteLine($"ThemeManager: Refreshed {currentPage?.GetType().Name ?? "Unknown"} page");
                    }
                    else if (Application.Current?.MainPage is NavigationPage navPage)
                    {
                        navPage.CurrentPage?.ForceLayout();
                    }
                    else if (Application.Current?.MainPage != null)
                    {
                        Application.Current.MainPage.ForceLayout();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in LightweightUIRefresh: {ex.Message}");
                }
            });
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
                Debug.WriteLine($"Initializing theme system with {themeName}");
                
                // Apply the theme
                SetThemeByName(themeName);
                
                // Verify all theme resources for dark/light mode
                MainThread.BeginInvokeOnMainThread(() => {
                    VerifyThemeResources();
                });
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
        
        /// <summary>
        /// Verify all theme resources are correctly defined
        /// </summary>
        private static void VerifyThemeResources()
        {
            var resources = Application.Current?.Resources;
            if (resources == null) return;
            
            // Essential color pairs to verify
            var colorPairs = new Dictionary<string, string>()
            {
                { "Primary", "PrimaryDark" },
                { "Background", "BackgroundDark" },
                { "CardBackground", "CardBackgroundDark" },
                { "PrimaryTextColor", "PrimaryTextColorDark" },
                { "SecondaryTextColor", "SecondaryTextColorDark" }
            };
            
            foreach (var pair in colorPairs)
            {
                if (!resources.ContainsKey(pair.Key) || !resources.ContainsKey(pair.Value))
                {
                    Debug.WriteLine($"Warning: Theme pair missing - {pair.Key} / {pair.Value}");
                }
            }
        }
    }
}
