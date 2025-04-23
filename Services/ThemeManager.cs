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
                        // FIXED: Set RequestedTheme first, then UserAppTheme for better theme propagation
                        AppTheme requestedTheme = themeName switch
                        {
                            "Dark" => AppTheme.Dark,
                            "Light" => AppTheme.Light,
                            _ => AppTheme.Unspecified
                        };
                        
                        // Save preference before applying to avoid race conditions
                        Preferences.Default.Set(THEME_PREFERENCE_KEY, themeName);
                        
                        // Determine actual theme for event
                        bool isDark = themeName == "Dark" || 
                            (themeName == "System" && Application.Current.PlatformAppTheme == AppTheme.Dark);

                        // Apply theme dictionary on the main thread with stronger synchronization
                        MainThread.BeginInvokeOnMainThread(() => {
                            try {
                                // FIXED: First apply theme dictionary to ensure resources are in place
                                ForceApplyThemeDictionary(
                                    GetThemeDictionary(isDark ? "Dark" : "Light"),
                                    isDark ? "Dark" : "Light"
                                );
                                
                                // FIXED: Then set UserAppTheme which will trigger system theme changes
                                Application.Current.UserAppTheme = requestedTheme;
                                
                                // FIXED: Force an additional refresh after a short delay
                                Task.Delay(50).ContinueWith(_ => {
                                    MainThread.BeginInvokeOnMainThread(() => {
                                        // Add a more aggressive app-level theme refresh
                                        ForceAppWideRefresh(isDark);
                                        
                                        // Notify subscribers after the theme is fully applied
                                        ThemeChanged?.Invoke(null, isDark);
                                    });
                                });
                            }
                            catch (Exception ex) {
                                Debug.WriteLine($"Error applying theme on UI thread: {ex.Message}");
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error setting theme: {ex.Message}");
                }
                finally
                {
                    // FIXED: Delay theme lock release to prevent rapid theme toggles
                    Task.Delay(200).ContinueWith(_ => _isChangingTheme = false);
                }
            }
        }

        // FIXED: New method that ensures full refresh
        private static void ForceApplyThemeDictionary(ResourceDictionary newTheme, string themeName)
        {
            if (Application.Current?.Resources == null) return;
            
            var mergedDicts = Application.Current.Resources.MergedDictionaries;
            
            // Find and remove existing theme dictionary - more aggressive approach
            List<ResourceDictionary> toRemove = new List<ResourceDictionary>();
            foreach (var dict in mergedDicts)
            {
                if (dict is DarkTheme || dict is LightTheme)
                {
                    toRemove.Add(dict);
                }
            }
            
            // Remove all found theme dictionaries to avoid conflicts
            foreach (var dict in toRemove)
            {
                mergedDicts.Remove(dict);
            }
            
            // Add the new theme explicitly at the end for higher precedence
            mergedDicts.Add(newTheme);
            
            // FIXED: Force a refresh of key resources
            ForceResourceRefresh(newTheme, themeName);
        }
        
        // FIXED: New method to ensure key resources are refreshed
        private static void ForceResourceRefresh(ResourceDictionary theme, string themeName)
        {
            try
            {
                bool isDark = themeName == "Dark";
                var resources = Application.Current.Resources;
                
                // Force update key colors by explicitly resetting them
                resources["Primary"] = isDark ? 
                    theme.TryGetValue("PrimaryDark", out var primaryDark) ? primaryDark : Color.FromArgb("#9982EA") : 
                    theme.TryGetValue("Primary", out var primary) ? primary : Colors.Purple;
                
                resources["Background"] = isDark ? 
                    theme.TryGetValue("BackgroundDark", out var backgroundDark) ? backgroundDark : Color.FromArgb("#121212") : 
                    theme.TryGetValue("Background", out var background) ? background : Colors.White;
                
                // Force update of other critical resources
                resources["PrimaryTextColor"] = isDark ? 
                    theme.TryGetValue("PrimaryTextColorDark", out var textDark) ? textDark : Colors.White : 
                    theme.TryGetValue("PrimaryTextColor", out var text) ? text : Colors.Black;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error forcing resource refresh: {ex.Message}");
            }
        }

        /// <summary>
        /// Forces an app-wide refresh of theme-dependent UI
        /// </summary>
        private static void ForceAppWideRefresh(bool isDark)
        {
            try
            {
                // FIXED: Enhanced app-wide refresh
                var app = Application.Current;
                if (app == null) return;
                
                // FIXED: Force multiple refresh signals
                MainThread.BeginInvokeOnMainThread(async () => {
                    try
                    {
                        // Trigger layout invalidation throughout the application
                        if (app.MainPage != null)
                        {
                            // Force a visual property change on all pages
                            app.MainPage.InvalidateMeasure();
                            
                            if (app.MainPage is Shell shell)
                            {
                                // Double invalidation for Shell (triggers needed refresh events)
                                shell.InvalidateMeasure();
                                shell.ForceLayout();
                                
                                if (shell.CurrentPage != null)
                                {
                                    shell.CurrentPage.InvalidateMeasure();
                                    shell.CurrentPage.ForceLayout();
                                }
                            }
                            
                            // FIXED: Use additional technique to force redraw
                            double opacity = app.MainPage.Opacity;  // Changed from Opacity to double
                            app.MainPage.Opacity = 0.99;
                            await Task.Delay(10);
                            app.MainPage.Opacity = opacity;
                            
                            // Force another UI refresh after a short delay
                            await Task.Delay(50);
                            RefreshUIComponents();
                            
                            // FIXED: Add a second refresh cycle with a longer delay
                            await Task.Delay(150);
                            RefreshVisualElement(app.MainPage);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in ForceAppWideRefresh: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error forcing app-wide refresh: {ex.Message}");
            }
        }

        /// <summary>
        /// Initialize theme system based on preferences
        /// </summary>
        public static void Initialize() 
        {
            try 
            {
                Debug.WriteLine("ThemeManager: Starting initialization");
                
                // Make sure all required resources exist immediately
                EnsureRequiredResourcesExist();
                
                // Get saved theme preference with default to Light
                string themeName = "Light";
                try 
                {
                    themeName = Preferences.Default.Get(THEME_PREFERENCE_KEY, "Light");
                    Debug.WriteLine($"ThemeManager: Loaded theme preference: {themeName}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error reading theme preference: {ex.Message}");
                    themeName = "Light";  // Safer default
                }
                
                // Make sure theme name is valid
                if (string.IsNullOrEmpty(themeName) || 
                    (themeName != "Light" && themeName != "Dark" && themeName != "System"))
                {
                    themeName = "Light";
                }
                
                // Apply the saved theme immediately, don't wait for UI thread
                string resolvedTheme = ResolveEffectiveTheme(themeName);
                Debug.WriteLine($"ThemeManager: Effective theme will be {resolvedTheme}");
                
                // Apply theme with high priority
                ApplyThemeImmediately(themeName, resolvedTheme == "Dark");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing theme: {ex.Message}");
                // Apply light theme as failsafe
                try {
                    ApplyThemeImmediately("Light", false);
                } catch {}
            }
        }

        /// <summary>
        /// Resolves the effective theme based on the theme name
        /// </summary>
        private static string ResolveEffectiveTheme(string themeName)
        {
            if (themeName == "System")
            {
                try
                {
                    return Application.Current?.PlatformAppTheme == AppTheme.Dark ? "Dark" : "Light";
                }
                catch
                {
                    return "Light"; // Safe default
                }
            }
            return themeName;
        }

        /// <summary>
        /// Applies theme immediately without waiting
        /// </summary>
        private static void ApplyThemeImmediately(string themeName, bool isDark)
        {
            // First apply on current thread with high priority
            try
            {
                Debug.WriteLine($"ThemeManager: Initial theme application starting - {themeName}");
                
                // Set preferences first to ensure consistency
                Preferences.Default.Set(THEME_PREFERENCE_KEY, themeName);
                
                // Force appropriate theme dictionary application
                ForceThemeDictionaryApplication(isDark ? "Dark" : "Light");
                
                // Then set application theme
                if (Application.Current != null)
                {
                    // Translate theme name to AppTheme
                    AppTheme requestedTheme = themeName switch
                    {
                        "Dark" => AppTheme.Dark,
                        "Light" => AppTheme.Light,
                        _ => AppTheme.Unspecified
                    };
                    
                    Application.Current.UserAppTheme = requestedTheme;
                    
                    // Force initial refresh
                    ForceResourceRefresh(null, isDark ? "Dark" : "Light");
                    
                    Debug.WriteLine("ThemeManager: Initial theme applied");
                }
                
                // Then schedule main thread application for UI
                MainThread.BeginInvokeOnMainThread(() => {
                    try
                    {
                        Debug.WriteLine("ThemeManager: UI thread theme application starting");
                        
                        // Refresh on UI thread
                        if (Application.Current != null)
                        {
                            // Force theme resources again on UI thread
                            ForceThemeDictionaryApplication(isDark ? "Dark" : "Light");
                            
                            // Force app-wide refresh
                            ForceAppWideRefresh(isDark);
                            
                            // Notify subscribers after a delay to ensure UI has updated
                            Task.Delay(100).ContinueWith(_ => {
                                MainThread.BeginInvokeOnMainThread(() => {
                                    SafelyTriggerThemeChanged(isDark);
                                    Debug.WriteLine("ThemeManager: Theme change event fired");
                                });
                            });
                            
                            Debug.WriteLine("ThemeManager: UI thread theme application completed");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"ThemeManager: UI thread application error: {ex.Message}");
                    }
                });
            }
            catch (Exception ex) 
            {
                Debug.WriteLine($"ThemeManager: Error in immediate theme application: {ex.Message}");
            }
        }

        /// <summary>
        /// Forces theme dictionary application regardless of current state
        /// </summary>
        private static void ForceThemeDictionaryApplication(string themeName)
        {
            if (Application.Current?.Resources == null) 
            {
                Debug.WriteLine("ThemeManager: Can't apply theme dictionary - Application.Current.Resources is null");
                return;
            }
            
            try
            {
                // Get appropriate theme dictionary
                ResourceDictionary themeDict = GetThemeDictionary(themeName);
                
                // Get all merged dictionaries
                var resources = Application.Current.Resources;
                var mergedDictionaries = resources.MergedDictionaries;
                
                // Remove any existing theme dictionaries
                List<ResourceDictionary> toRemove = new List<ResourceDictionary>();
                foreach (var dict in mergedDictionaries)
                {
                    var dictType = dict.GetType().Name;
                    if (dictType.Contains("Theme", StringComparison.OrdinalIgnoreCase) ||
                        dict is LightTheme || dict is DarkTheme)
                    {
                        toRemove.Add(dict);
                    }
                }
                
                // Remove identified theme dictionaries
                foreach (var dict in toRemove)
                {
                    mergedDictionaries.Remove(dict);
                }
                
                // Add new theme dictionary at the end for higher precedence
                mergedDictionaries.Add(themeDict);
                
                // Force update critical resources
                bool isDark = themeName == "Dark";
                
                // Basic resource enforcement - explicitly update key resources
                resources["Primary"] = isDark ? GetColorResource("PrimaryDark") : GetColorResource("Primary");
                resources["Background"] = isDark ? GetColorResource("BackgroundDark") : GetColorResource("Background");
                resources["CardBackground"] = isDark ? GetColorResource("CardBackgroundDark") : GetColorResource("CardBackground");
                resources["PrimaryTextColor"] = isDark ? GetColorResource("PrimaryTextColorDark") : GetColorResource("PrimaryTextColor");
                resources["SecondaryTextColor"] = isDark ? GetColorResource("SecondaryTextColorDark") : GetColorResource("SecondaryTextColor");
                
                Debug.WriteLine($"ThemeManager: Theme dictionary '{themeName}' applied successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ThemeManager: Error forcing theme dictionary application: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets a color resource with fallback
        /// </summary>
        private static Color GetColorResource(string key)
        {
            try
            {
                if (Application.Current?.Resources.TryGetValue(key, out var resource) == true &&
                    resource is Color color)
                {
                    return color;
                }
            }
            catch {}
            
            // Fallbacks for critical colors
            return key switch
            {
                "Primary" => Color.FromArgb("#512BD4"),
                "PrimaryDark" => Color.FromArgb("#7B68EE"),
                "Background" => Colors.White,
                "BackgroundDark" => Color.FromArgb("#121212"),
                "CardBackground" => Colors.White,
                "CardBackgroundDark" => Color.FromArgb("#252525"),
                "PrimaryTextColor" => Colors.Black,
                "PrimaryTextColorDark" => Colors.White,
                "SecondaryTextColor" => Color.FromArgb("#616161"),
                "SecondaryTextColorDark" => Color.FromArgb("#B0B0B0"),
                _ => Colors.Transparent
            };
        }

        /// <summary>
        /// Safely triggers the ThemeChanged event
        /// </summary>
        private static void SafelyTriggerThemeChanged(bool isDark)
        {
            try
            {
                ThemeChanged?.Invoke(null, isDark);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error triggering ThemeChanged event: {ex.Message}");
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
            EnsureResource("Gray200", Color.FromArgb("#E5E5E5"));
            EnsureResource("Gray600", Color.FromArgb("#757575"));
            
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
        /// Performs a more thorough UI refresh to ensure theme changes are reflected
        /// </summary>
        private static void RefreshUIComponents()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    // More aggressive UI refresh approach
                    if (Application.Current?.MainPage is Shell shell)
                    {
                        // Force immediate invalidation of shell
                        shell.InvalidateMeasure();
                        
                        // Get current visible page
                        if (shell.CurrentPage != null)
                        {
                            // Force invalidate the current page and all its children
                            shell.CurrentPage.InvalidateMeasure();
                            RefreshVisualElement(shell.CurrentPage);
                            
                            // Try to get the actual content page
                            if (shell.CurrentPage is ContentPage contentPage)
                            {
                                contentPage.InvalidateMeasure();
                                
                                // If there's content, force refresh it
                                if (contentPage.Content != null && contentPage.Content is VisualElement content)
                                {
                                    content.InvalidateMeasure();
                                    RefreshVisualElement(content);
                                }
                            }
                        }
                        
                        // Force layout on all navigation items that are VisualElements
                        // Note: ShellItem is not a VisualElement, so we need to handle it differently
                        foreach (var item in shell.Items)
                        {
                            // Find the actual visual elements within the ShellItem, if any
                            if (item is Element element)
                            {
                                RefreshShellElement(element);
                            }
                        }
                    }
                    else if (Application.Current?.MainPage is NavigationPage navPage)
                    {
                        // Similar approach for NavigationPage
                        navPage.InvalidateMeasure();
                        
                        if (navPage.CurrentPage != null)
                        {
                            navPage.CurrentPage.InvalidateMeasure();
                            RefreshVisualElement(navPage.CurrentPage);
                        }
                    }
                    else if (Application.Current?.MainPage != null)
                    {
                        Application.Current.MainPage.InvalidateMeasure();
                        RefreshVisualElement(Application.Current.MainPage);
                    }
                    
                    // Small delay to ensure UI thread can process the updates
                    await Task.Delay(50);
                    
                    // Force redraw with property changes
                    ForceRedraw();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error refreshing UI: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Refreshes a Shell element and its visual children
        /// </summary>
        private static void RefreshShellElement(Element element)
        {
            try
            {
                // If it's a visual element, we can refresh it
                if (element is VisualElement ve)
                {
                    ve.InvalidateMeasure();
                    RefreshVisualElement(ve);
                }
                
                // Recursively process child elements, looking for visual elements
                foreach (var child in element.LogicalChildren)
                {
                    if (child is Element childElement)
                    {
                        RefreshShellElement(childElement);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error refreshing shell element: {ex.Message}");
            }
        }

        /// <summary>
        /// Forces a redraw by modifying properties
        /// </summary>
        private static void ForceRedraw()
        {
            if (Application.Current?.MainPage == null) return;
            
            try
            {
                // Make small opacity changes to force redraw
                var opacity = Application.Current.MainPage.Opacity;
                Application.Current.MainPage.Opacity = 0.99;
                
                // Force a visual property change on the page to trigger redraw
                if (Application.Current.MainPage is Shell shell)
                {
                    var current = shell.BackgroundColor;
                    shell.BackgroundColor = Colors.Transparent;
                    shell.BackgroundColor = current;
                }
                
                // Restore original opacity
                Application.Current.MainPage.Opacity = opacity;
                
                // Also dispatch a dummy event to force the UI thread to process pending updates
                MainThread.BeginInvokeOnMainThread(() => {
                    Application.Current.MainPage.InvalidateMeasure();
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ForceRedraw: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Recursively refresh visual elements
        /// </summary>
        private static void RefreshVisualElement(VisualElement element)
        {
            try
            {
                // Skip null elements
                if (element == null) return;
                
                // Request layout update for the element itself
                element.InvalidateMeasure();
                
                // For layouts, refresh all children
                if (element is Layout layout)
                {
                    // For layouts, request layout update
                    layout.InvalidateMeasure();
                    
                    // Process all children
                    foreach (var child in layout.Children)
                    {
                        if (child is VisualElement ve)
                        {
                            RefreshVisualElement(ve);
                        }
                    }
                }
                
                // For content views, refresh the content
                if (element is ContentView contentView && contentView.Content is VisualElement content)
                {
                    RefreshVisualElement(content);
                }
                
                // Special handling for common UI elements that may have theme-specific appearances
                if (element is Frame frame)
                {
                    frame.InvalidateMeasure();
                    // Force a visual change to trigger redraw
                    var padding = frame.Padding;
                    frame.Padding = new Thickness(padding.Left + 0.1);
                    frame.Padding = padding;
                }
                
                if (element is Label label)
                {
                    label.InvalidateMeasure();
                    // Force text redraw
                    var text = label.Text;
                    if (!string.IsNullOrEmpty(text))
                    {
                        label.Text = text + " ";
                        label.Text = text;
                    }
                }
                
                if (element is Button button)
                {
                    button.InvalidateMeasure();
                }
                
                if (element is BoxView boxView)
                {
                    boxView.InvalidateMeasure();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error refreshing element {element?.GetType()}: {ex.Message}");
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
    }
}
