using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using NexusChat.Services;

namespace NexusChat.Core.ViewModels.DevTools
{
    /// <summary>
    /// ViewModel for theme testing functionality and UI component showcase
    /// </summary>
    public partial class ThemesPageViewModel : ObservableObject
    {
        #region Observable Properties
        
        [ObservableProperty]
        private bool _useSystemTheme;
        
        [ObservableProperty]
        private bool _isDarkTheme;
        
        [ObservableProperty]
        private string _themeIconText = "\uf185"; // Sun icon for light theme by default
        
        [ObservableProperty]
        private string _themeToggleText;
        
        [ObservableProperty]
        private string _currentThemeText;
        
        [ObservableProperty]
        private string _systemThemeText;
        
        [ObservableProperty]
        private string _selectedModel = "GPT-4 Turbo";
        
        [ObservableProperty]
        private bool _isLoading;
        
        [ObservableProperty]
        private string _lastCopiedColor;
        
        [ObservableProperty]
        private string _selectedComponent;

        [ObservableProperty]
        private bool _isComponentLoading;

        [ObservableProperty]
        private bool _isPageLoading = true;

        [ObservableProperty]
        private ObservableCollection<string> _componentCategories;
        
        // Flag to track if icons have been initialized
        private bool _iconsInitialized = false;
        
        // Dictionary to track which components have been loaded
        private readonly Dictionary<string, bool> _loadedComponents = new Dictionary<string, bool>();
        
        #endregion
        
        #region Computed Properties
        
        /// <summary>
        /// Gets whether we're not using the system theme
        /// </summary>
        public bool IsNotUsingSystemTheme => !UseSystemTheme;
        
        #endregion
        
        #region Commands
        
        /// <summary>
        /// Command to toggle between light and dark themes
        /// </summary>
        public IRelayCommand ToggleThemeCommand { get; }
        
        /// <summary>
        /// Command to toggle system theme usage
        /// </summary>
        public IRelayCommand ToggleUseSystemThemeCommand { get; }
        
        /// <summary>
        /// Command to navigate back
        /// </summary>
        public IAsyncRelayCommand GoBackCommand { get; }
        
        /// <summary>
        /// Command to show the model picker
        /// </summary>
        public IAsyncRelayCommand ShowModelPickerCommand { get; }
        
        /// <summary>
        /// Command to copy a color to clipboard
        /// </summary>
        public IAsyncRelayCommand<string> CopyColorToClipboardCommand { get; }
        
        /// <summary>
        /// Command to select a component to display
        /// </summary>
        public IRelayCommand<string> SelectComponentCommand { get; }
        
        /// <summary>
        /// Command to copy icon code to clipboard
        /// </summary>
        public IAsyncRelayCommand<string> CopyIconCommand { get; }
        
        #endregion
        
        #region Icon Properties
        
        /// <summary>
        /// Collection of common FontAwesome icon codes
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<IconItem> _icons;

        /// <summary>
        /// Status message for icon feedback
        /// </summary>
        [ObservableProperty]
        private string _iconStatusMessage = "Select an icon to copy its code";

        /// <summary>
        /// Font debugging information
        /// </summary>
        [ObservableProperty]
        private string _fontDebugInfo;
        
        #endregion
        
        // Property to expose the component change to the view
        public event EventHandler<string> ComponentChanged;
        
        /// <summary>
        /// Initializes a new instance of ThemesPageViewModel
        /// </summary>
        public ThemesPageViewModel()
        {
            // Initialize commands
            ToggleThemeCommand = new RelayCommand(ToggleTheme);
            ToggleUseSystemThemeCommand = new RelayCommand(ToggleUseSystemTheme);
            GoBackCommand = new AsyncRelayCommand(GoBackAsync);
            ShowModelPickerCommand = new AsyncRelayCommand(ShowModelPickerAsync);
            CopyColorToClipboardCommand = new AsyncRelayCommand<string>(CopyColorToClipboardAsync);
            SelectComponentCommand = new RelayCommand<string>(SelectComponent);
            CopyIconCommand = new AsyncRelayCommand<string>(CopyIconCodeAsync);
            
            // Initialize theme state - Get actual current theme
            _isDarkTheme = Application.Current.UserAppTheme == AppTheme.Dark;
            _useSystemTheme = Application.Current.UserAppTheme == AppTheme.Unspecified;
            
            // Initialize text properties
            UpdateThemeText();
            
            // Subscribe to theme changes
            ThemeManager.ThemeChanged += OnThemeChanged;

            // Initialize component categories
            _componentCategories = new ObservableCollection<string>
            {
                "Colors",
                "Typography", 
                "Buttons",
                "InputControls",
                "ChatComponents",
                "StatusIndicators",
                "LayoutComponents",
                "FormComponents",
                "Accessibility",
                "Icons"
            };
            
            // Initialize empty icons collection
            _icons = new ObservableCollection<IconItem>();
            
            // Initialize the component tracking dictionary
            foreach (var category in _componentCategories)
            {
                _loadedComponents[category] = false;
            }
        }

        /// <summary>
        /// Toggles the use of system theme
        /// </summary>
        public void ToggleUseSystemTheme()
        {
            UseSystemTheme = !UseSystemTheme;
            OnPropertyChanged(nameof(IsNotUsingSystemTheme));
            
            ThemeManager.SetThemeByName(UseSystemTheme ? "System" : (IsDarkTheme ? "Dark" : "Light"));
            
            // Update theme state after changing system theme setting
            IsDarkTheme = ThemeManager.IsDarkTheme;
            UpdateThemeText();
        }
        
        /// <summary>
        /// Updates the theme text indicators based on the current theme
        /// </summary>
        private void UpdateThemeText()
        {
            // Get actual current theme
            IsDarkTheme = Application.Current.RequestedTheme == AppTheme.Dark;
            if (!UseSystemTheme)
            {
                IsDarkTheme = Application.Current.UserAppTheme == AppTheme.Dark;
            }
            
            CurrentThemeText = IsDarkTheme ? "Dark" : "Light";
            
            // Use direct FontAwesome characters, not HTML entities
            ThemeIconText = UseSystemTheme ? "\uf042" : (IsDarkTheme ? "\uf186" : "\uf185");
            
            // Fix the toggle text to match the current theme
            ThemeToggleText = IsDarkTheme ? "Switch to Light" : "Switch to Dark";
            
            var systemTheme = Application.Current.RequestedTheme == AppTheme.Dark ? "Dark" : "Light";
            SystemThemeText = systemTheme;
        }
        
        /// <summary>
        /// React to theme changes from other sources
        /// </summary>
        private void OnThemeChanged(object sender, bool isDark)
        {
            MainThread.BeginInvokeOnMainThread(async () => 
            {
                try
                {
                    // Update ViewModel properties to match new theme
                    IsDarkTheme = isDark;
                    UpdateThemeText();
                    
                    // Force a theme-specific UI refresh
                    RefreshThemeSpecificUIElements();
                    
                    // Add a small delay to ensure text refresh has time to complete
                    await Task.Delay(50);
                    
                    // Force another refresh for reliable icon display
                    RefreshThemeSpecificUIElements();
                    
                    // If icons are loaded, refresh them explicitly
                    if (_iconsInitialized && Icons?.Count > 0)
                    {
                        var tempIcons = Icons.ToList();
                        Icons.Clear();
                        
                        // Add a small delay before re-adding icons to force a refresh
                        await Task.Delay(50);
                        foreach (var icon in tempIcons)
                        {
                            Icons.Add(icon);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in theme change handler: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Explicitly refreshes theme-specific UI elements
        /// </summary>
        private void RefreshThemeSpecificUIElements()
        {
            try
            {
                // Update icon visibility by modifying property
                var currentIcon = ThemeIconText;
                ThemeIconText = currentIcon == "\uf185" ? "\uf186" : "\uf185";  // Toggle icon to force update
                ThemeIconText = currentIcon;  // Set back to correct icon
                
                // Notify UI to refresh important properties
                OnPropertyChanged(nameof(ThemeIconText));
                OnPropertyChanged(nameof(ThemeToggleText));
                OnPropertyChanged(nameof(CurrentThemeText));
                OnPropertyChanged(nameof(SystemThemeText));
                OnPropertyChanged(nameof(IsDarkTheme));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error refreshing theme UI elements: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Navigates back to the previous page
        /// </summary>
        private async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
        
        /// <summary>
        /// Shows the model picker dialog
        /// </summary>
        private async Task ShowModelPickerAsync()
        {
            string result = await Shell.Current.DisplayActionSheet(
                "Select AI Model",
                "Cancel",
                null,
                "GPT-4 Turbo",
                "GPT-4o",
                "Claude 3 Opus",
                "Claude 3 Sonnet",
                "Gemini Pro",
                "Custom Model"
            );
            
            if (!string.IsNullOrEmpty(result) && result != "Cancel")
            {
                SelectedModel = result;
            }
        }
        
        /// <summary>
        /// Copies a color value to the clipboard
        /// </summary>
        private async Task CopyColorToClipboardAsync(string colorKey)
        {
            try
            {
                await Clipboard.SetTextAsync(colorKey);
                LastCopiedColor = colorKey;
                
                // Show toast notification
                var toast = Toast.Make($"Color key '{colorKey}' copied to clipboard");
                await toast.Show();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to copy color: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Selects a component to display with lazy loading
        /// </summary>
        public void SelectComponent(string componentName)
        {
            Debug.WriteLine($"ThemesPageViewModel: SelectComponent {componentName}");
            
            // Set loading state
            IsComponentLoading = true;
            SelectedComponent = componentName;
            
            // Unload other components if needed
            // This is optional - if memory usage becomes an issue, we could implement
            // a more aggressive unloading strategy
            
            // Load this component if it hasn't been loaded yet
            if (!_loadedComponents.ContainsKey(componentName) || !_loadedComponents[componentName])
            {
                // Execute the appropriate loading method based on component type
                switch (componentName)
                {
                    case "Icons":
                        if (!_iconsInitialized)
                        {
                            InitializeIcons();
                            _iconsInitialized = true;
                        }
                        break;
                    
                    default:
                        // For most components, just mark them as loaded
                        break;
                }
                
                // Mark this component as loaded
                _loadedComponents[componentName] = true;
            }
            
            // Notify UI that component should change
            ComponentChanged?.Invoke(this, componentName);
        }
        
        /// <summary>
        /// Toggles between light and dark themes
        /// </summary>
        public void ToggleTheme()
        {
            try
            {
                Debug.WriteLine("ThemesPageViewModel: Toggling theme");
                
                // Directly use ThemeManager to change the theme
                // This will affect the entire application, not just the current page
                bool newTheme = !IsDarkTheme; 
                
                // Set loading state to indicate theme change is happening
                IsLoading = true;
                
                // Force icon refresh before changing theme
                RefreshThemeSpecificUIElements();
                
                // Apply theme change
                ThemeManager.SetTheme(newTheme);
                
                // Explicitly update our local properties to match new theme
                IsDarkTheme = newTheme;
                UpdateThemeText();
                
                // Reset loading state after a small delay
                MainThread.BeginInvokeOnMainThread(async () => {
                    await Task.Delay(100);
                    IsLoading = false;
                    RefreshThemeSpecificUIElements();
                });
                
                Debug.WriteLine($"Theme toggled to {(newTheme ? "Dark" : "Light")} mode");
            }
            catch (Exception ex)
            {
                IsLoading = false;
                Debug.WriteLine($"Error toggling theme: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Initializes the ViewModel asynchronously with performance optimizations
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                // Set loading state
                IsLoading = true;
                IsPageLoading = true;
                
                // Update theme information - don't change themes, just read state
                IsDarkTheme = ThemeManager.IsDarkTheme;
                UpdateThemeText();
                
                // Ensure all properties are properly notified
                OnPropertyChanged(nameof(IsNotUsingSystemTheme));
                
                // Give UI time to render before finishing initialization
                await Task.Delay(100);
                
                // Force theme icon to be visible on initial load
                RefreshThemeSpecificUIElements();
                
                // Update loading state - but don't load any components yet
                IsPageLoading = false;
                
                // Don't automatically select any component
                // Let the user choose which one to load
                IsComponentLoading = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing themes page: {ex.Message}");
                IsPageLoading = false;
                IsComponentLoading = false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Signals component loading is complete
        /// </summary>
        public void ComponentLoadingComplete()
        {
            IsComponentLoading = false;
        }
        
        /// <summary>
        /// Cleans up resources when the ViewModel is no longer needed
        /// </summary>
        public void Cleanup()
        {
            Debug.WriteLine("ThemesPageViewModel: Cleanup");
            
            // Unsubscribe from events
            ThemeManager.ThemeChanged -= OnThemeChanged;
            
            // Clear any references that might hold resources
            LastCopiedColor = null;
            
            // Clear icons collection to free memory
            if (_icons != null)
            {
                _icons.Clear();
            }
            
            // Reset loaded component tracking
            foreach (var key in _loadedComponents.Keys.ToList())
            {
                _loadedComponents[key] = false;
            }
            
            _iconsInitialized = false;
        }
        
        #region Icon Methods
        
        /// <summary>
        /// Copies icon code to clipboard
        /// </summary>
        private async Task CopyIconCodeAsync(string iconCode)
        {
            if (string.IsNullOrEmpty(iconCode))
                return;
            
            try
            {
                await Clipboard.SetTextAsync(iconCode);
                IconStatusMessage = $"Copied icon code: {iconCode}";
                Debug.WriteLine($"Copied to clipboard: {iconCode}");
                
                // Reset status after a delay
                await Task.Delay(2000);
                IconStatusMessage = "Select an icon to copy its code";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error copying to clipboard: {ex.Message}");
                IconStatusMessage = "Failed to copy to clipboard";
            }
        }

        /// <summary>
        /// Generates debugging information about fonts
        /// </summary>
        private void GenerateFontDebugInfo()
        {
            // Generate only when needed - don't create in constructor
            var debugInfo = 
                "Font Information:\n" +
                "- FontAwesome-Solid\n" +
                "- FontAwesome-Regular\n" +
                "- FontAwesome-Brands\n" +
                "- OpenSansRegular\n" +
                "- OpenSansSemiBold\n" +
                "- OpenSansBold\n\n" +
                "If you can see icons on this page, FontAwesome is working correctly.";
            
            FontDebugInfo = debugInfo;
        }

        /// <summary>
        /// Initializes the collection of FontAwesome icons - only when needed
        /// </summary>
        private void InitializeIcons()
        {
            Debug.WriteLine("ThemesPageViewModel: Initializing icons on demand");
            
            // Only initialize if not already done
            if (_iconsInitialized)
                return;
                
            Icons = new ObservableCollection<IconItem>
            {
                new IconItem { Name = "Home", Code = "\uf015", Category = "Navigation" },
                new IconItem { Name = "Search", Code = "\uf002", Category = "Actions" },
                new IconItem { Name = "User", Code = "\uf007", Category = "User Interface" },
                new IconItem { Name = "Settings", Code = "\uf013", Category = "User Interface" },
                new IconItem { Name = "Check", Code = "\uf00c", Category = "Actions" },
                new IconItem { Name = "Times", Code = "\uf00d", Category = "Actions" },
                new IconItem { Name = "Comment", Code = "\uf075", Category = "Communication" },
                new IconItem { Name = "Comments", Code = "\uf086", Category = "Communication" },
                new IconItem { Name = "Star", Code = "\uf005", Category = "Shapes" },
                new IconItem { Name = "Heart", Code = "\uf004", Category = "Shapes" },
                new IconItem { Name = "Bell", Code = "\uf0f3", Category = "User Interface" },
                new IconItem { Name = "Calendar", Code = "\uf133", Category = "User Interface" },
                new IconItem { Name = "Paper Plane", Code = "\uf1d8", Category = "Communication" },
                new IconItem { Name = "Envelope", Code = "\uf0e0", Category = "Communication" },
                new IconItem { Name = "Pencil", Code = "\uf303", Category = "Actions" }
            };
            
            // Also generate font debug info when icons are loaded
            GenerateFontDebugInfo();
            
            _iconsInitialized = true;
        }

        // Update when theme changes
        private void UpdateThemeIcon()
        {
            ThemeIconText = Application.Current.RequestedTheme == AppTheme.Dark ? "\uf186" : "\uf185";
            // f186 is moon, f185 is sun
        }
        
        #endregion
    }

    /// <summary>
    /// Represents a FontAwesome icon item
    /// </summary>
    public class IconItem
    {
        /// <summary>
        /// Name of the icon
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Unicode character code for the icon
        /// </summary>
        public string Code { get; set; }
        
        /// <summary>
        /// Category the icon belongs to
        /// </summary>
        public string Category { get; set; }
    }
}
