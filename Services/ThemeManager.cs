using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using NexusChat.Resources.Styles;
#if ANDROID
using AndroidX.Core.View;
#endif
#if IOS
using UIKit;
#endif

namespace NexusChat.Services
{
    /// <summary>
    /// Centralized manager for application theme switching
    /// </summary>
    public static class ThemeManager {
        private const string THEME_PREFERENCE_KEY = "theme";
        private static bool _isChangingTheme = false;
        private static readonly object _themeLock = new object();
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

                    if (Application.Current != null)
                    {
                        AppTheme requestedTheme = themeName switch
                        {
                            "Dark" => AppTheme.Dark,
                            "Light" => AppTheme.Light,
                            _ => AppTheme.Unspecified
                        };
                        
                        Preferences.Default.Set(THEME_PREFERENCE_KEY, themeName);
                        
                        bool isDark = themeName == "Dark" || 
                            (themeName == "System" && Application.Current.PlatformAppTheme == AppTheme.Dark);

                        Application.Current.UserAppTheme = requestedTheme;
                        ForceThemeDictionaryApplication(isDark ? "Dark" : "Light");
                        
                        UpdateSystemChrome(isDark);

                        MainThread.BeginInvokeOnMainThread(() => {
                            try {
                                ForceThemeDictionaryApplication(isDark ? "Dark" : "Light");
                                UpdateSystemChrome(isDark);
                                ForceWindowRefresh(isDark);
                                
                                Task.Delay(100).ContinueWith(_ => {
                                    MainThread.BeginInvokeOnMainThread(() => {
                                        ForceAppWideRefresh(isDark);
                                        UpdateSystemChrome(isDark);
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
                    Task.Delay(200).ContinueWith(_ => _isChangingTheme = false);
                }
            }
        }

        /// <summary>
        /// Updates system chrome elements (status bar, navigation bar)
        /// </summary>
        private static void UpdateSystemChrome(bool isDark)
        {
            try
            {
#if ANDROID
                var activity = Platform.CurrentActivity ?? Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
                if (activity?.Window != null)
                {
                    var window = activity.Window;
                    var decorView = window.DecorView;
                    
                    if (isDark)
                    {
                        // Dark theme - use light content on dark background
                        window.SetStatusBarColor(Android.Graphics.Color.ParseColor("#121212"));
                        window.SetNavigationBarColor(Android.Graphics.Color.ParseColor("#121212"));
                        
                        // Clear light status bar flag for dark theme
                        decorView.SystemUiVisibility = (Android.Views.StatusBarVisibility)
                            ((int)decorView.SystemUiVisibility & ~(int)Android.Views.SystemUiFlags.LightStatusBar);
                        
                        // Clear light navigation bar flag
                        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
                        {
                            decorView.SystemUiVisibility = (Android.Views.StatusBarVisibility)
                                ((int)decorView.SystemUiVisibility & ~(int)Android.Views.SystemUiFlags.LightNavigationBar);
                        }
                    }
                    else
                    {
                        // Light theme - use dark content on light background
                        window.SetStatusBarColor(Android.Graphics.Color.White);
                        window.SetNavigationBarColor(Android.Graphics.Color.White);
                        
                        // Set light status bar flag for light theme
                        decorView.SystemUiVisibility = (Android.Views.StatusBarVisibility)
                            ((int)decorView.SystemUiVisibility | (int)Android.Views.SystemUiFlags.LightStatusBar);
                        
                        // Set light navigation bar flag
                        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
                        {
                            decorView.SystemUiVisibility = (Android.Views.StatusBarVisibility)
                                ((int)decorView.SystemUiVisibility | (int)Android.Views.SystemUiFlags.LightNavigationBar);
                        }
                    }
                }
#elif IOS
                if (UIKit.UIApplication.SharedApplication?.KeyWindow != null)
                {
                    var window = UIKit.UIApplication.SharedApplication.KeyWindow;
                    if (isDark)
                    {
                        window.OverrideUserInterfaceStyle = UIKit.UIUserInterfaceStyle.Dark;
                    }
                    else
                    {
                        window.OverrideUserInterfaceStyle = UIKit.UIUserInterfaceStyle.Light;
                    }
                }
#endif
                Debug.WriteLine($"System chrome updated for {(isDark ? "dark" : "light")} theme");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating system chrome: {ex.Message}");
            }
        }

        /// <summary>
        /// Forces a complete window refresh
        /// </summary>
        private static void ForceWindowRefresh(bool isDark)
        {
            try
            {
#if ANDROID
                var activity = Platform.CurrentActivity;
                if (activity?.Window != null)
                {
                    // Force window to redraw by invalidating the decor view
                    activity.Window.DecorView.Invalidate();
                    
                    // Force recreation of window insets
                    if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Kitkat)
                    {
                        activity.Window.DecorView.RequestApplyInsets();
                    }
                }
#elif IOS
                // Force all windows to refresh their appearance
                foreach (var window in UIKit.UIApplication.SharedApplication.Windows)
                {
                    window.SetNeedsDisplay();
                    window.LayoutIfNeeded();
                }
#endif
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error forcing window refresh: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies theme immediately without waiting
        /// </summary>
        private static void ApplyThemeImmediately(string themeName, bool isDark)
        {
            try
            {
                Debug.WriteLine($"ThemeManager: Initial theme application starting - {themeName}");
                
                Preferences.Default.Set(THEME_PREFERENCE_KEY, themeName);
                
                if (Application.Current != null)
                {
                    AppTheme requestedTheme = themeName switch
                    {
                        "Dark" => AppTheme.Dark,
                        "Light" => AppTheme.Light,
                        _ => AppTheme.Unspecified
                    };
                    
                    // Apply theme and resources immediately
                    Application.Current.UserAppTheme = requestedTheme;
                    ForceThemeDictionaryApplication(isDark ? "Dark" : "Light");
                    
                    // Update system chrome immediately
                    UpdateSystemChrome(isDark);
                    
                    Debug.WriteLine("ThemeManager: Initial theme applied");
                }
                
                MainThread.BeginInvokeOnMainThread(() => {
                    try
                    {
                        Debug.WriteLine("ThemeManager: UI thread theme application starting");
                        
                        if (Application.Current != null)
                        {
                            ForceThemeDictionaryApplication(isDark ? "Dark" : "Light");
                            
                            // Update system chrome on UI thread
                            UpdateSystemChrome(isDark);
                            
                            // Force window refresh
                            ForceWindowRefresh(isDark);
                            
                            ForceAppWideRefresh(isDark);
                            
                            Task.Delay(150).ContinueWith(_ => {
                                MainThread.BeginInvokeOnMainThread(() => {
                                    // Final system chrome update
                                    UpdateSystemChrome(isDark);
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
        /// Forces an app-wide refresh of theme-dependent UI
        /// </summary>
        private static void ForceAppWideRefresh(bool isDark)
        {
            try
            {
                var app = Application.Current;
                if (app == null) return;
                
                MainThread.BeginInvokeOnMainThread(async () => {
                    try
                    {
                        // Update system chrome first
                        UpdateSystemChrome(isDark);
                        
                        if (app.MainPage != null)
                        {
                            app.MainPage.InvalidateMeasure();
                            
                            if (app.MainPage is Shell shell)
                            {
                                shell.InvalidateMeasure();
                                shell.ForceLayout();
                                
                                if (shell.CurrentPage != null)
                                {
                                    shell.CurrentPage.InvalidateMeasure();
                                    shell.CurrentPage.ForceLayout();
                                }
                            }
                            
                            double opacity = app.MainPage.Opacity;
                            app.MainPage.Opacity = 0.99;
                            await Task.Delay(10);
                            app.MainPage.Opacity = opacity;
                            
                            await Task.Delay(50);
                            RefreshUIComponents();
                            
                            await Task.Delay(150);
                            RefreshVisualElement(app.MainPage);
                            
                            // Final system chrome update
                            UpdateSystemChrome(isDark);
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
                try
                {
                    theme = themeName == "Dark" ? CreateDarkTheme() : CreateLightTheme();
                    _cachedThemes[themeName] = theme;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error creating theme dictionary for {themeName}: {ex.Message}");
                    theme = CreateFallbackTheme(themeName == "Dark");
                    _cachedThemes[themeName] = theme;
                }
            }
            return theme;
        }

        /// <summary>
        /// Creates a dark theme dictionary safely
        /// </summary>
        private static ResourceDictionary CreateDarkTheme()
        {
            try
            {
                return new DarkTheme();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating DarkTheme instance: {ex.Message}");
                return CreateFallbackTheme(true);
            }
        }

        /// <summary>
        /// Creates a light theme dictionary safely
        /// </summary>
        private static ResourceDictionary CreateLightTheme()
        {
            try
            {
                return new LightTheme();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating LightTheme instance: {ex.Message}");
                return CreateFallbackTheme(false);
            }
        }

        /// <summary>
        /// Creates a fallback theme dictionary with essential colors
        /// </summary>
        private static ResourceDictionary CreateFallbackTheme(bool isDark)
        {
            var fallbackTheme = new ResourceDictionary();
            
            if (isDark)
            {
                fallbackTheme["Primary"] = Color.FromArgb("#7B68EE");
                fallbackTheme["Background"] = Color.FromArgb("#121212");
                fallbackTheme["CardBackground"] = Color.FromArgb("#252525");
                fallbackTheme["PrimaryTextColor"] = Colors.White;
                fallbackTheme["SecondaryTextColor"] = Color.FromArgb("#B0B0B0");
            }
            else
            {
                fallbackTheme["Primary"] = Color.FromArgb("#512BD4");
                fallbackTheme["Background"] = Colors.White;
                fallbackTheme["CardBackground"] = Colors.White;
                fallbackTheme["PrimaryTextColor"] = Colors.Black;
                fallbackTheme["SecondaryTextColor"] = Color.FromArgb("#616161");
            }
            
            return fallbackTheme;
        }

        /// <summary>
        /// Ensures all required resources exist in the application resources
        /// </summary>
        private static void EnsureRequiredResourcesExist()
        {
            try
            {
                if (Application.Current?.Resources == null)
                {
                    Debug.WriteLine("Application.Current.Resources is null, cannot ensure required resources");
                    return;
                }

                var resources = Application.Current.Resources;
                
                // Essential colors that must exist for the app to function
                var requiredColors = new Dictionary<string, Color>
                {
                    ["Primary"] = Color.FromArgb("#512BD4"),
                    ["PrimaryDark"] = Color.FromArgb("#7B68EE"),
                    ["Background"] = Colors.White,
                    ["BackgroundDark"] = Color.FromArgb("#121212"),
                    ["CardBackground"] = Colors.White,
                    ["CardBackgroundDark"] = Color.FromArgb("#252525"),
                    ["PrimaryTextColor"] = Colors.Black,
                    ["PrimaryTextColorDark"] = Colors.White,
                    ["SecondaryTextColor"] = Color.FromArgb("#616161"),
                    ["SecondaryTextColorDark"] = Color.FromArgb("#B0B0B0"),
                    ["White"] = Colors.White,
                    ["Black"] = Colors.Black,
                    ["Gray100"] = Color.FromArgb("#F5F5F5"),
                    ["Gray200"] = Color.FromArgb("#EEEEEE"),
                    ["Gray600"] = Color.FromArgb("#757575"),
                    ["Gray800"] = Color.FromArgb("#424242"),
                    ["Gray900"] = Color.FromArgb("#212121"),
                    ["Gray950"] = Color.FromArgb("#121212"),
                    ["OffBlack"] = Color.FromArgb("#121212")
                };

                // Add missing colors
                foreach (var colorPair in requiredColors)
                {
                    if (!resources.ContainsKey(colorPair.Key))
                    {
                        try
                        {
                            resources[colorPair.Key] = colorPair.Value;
                            Debug.WriteLine($"Added missing color resource: {colorPair.Key}");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error adding color resource {colorPair.Key}: {ex.Message}");
                        }
                    }
                }

                Debug.WriteLine("Required resources verification completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error ensuring required resources exist: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Initializes the theme manager (for internal use)
        /// </summary>
        internal static void Init()
        {
            // Preload both themes to ensure their dictionaries are created
            try
            {
                var darkTheme = CreateDarkTheme();
                var lightTheme = CreateLightTheme();
                
                _cachedThemes["Dark"] = darkTheme;
                _cachedThemes["Light"] = lightTheme;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error preloading themes: {ex.Message}");
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
