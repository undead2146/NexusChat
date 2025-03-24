using System;
using System.Collections.ObjectModel;
using System.Diagnostics; // Add this for Debug class
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Maui.Alerts; // Add this for Toast class
using CommunityToolkit.Maui.Core; // Add this for Toast dependencies
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;

namespace NexusChat.Core.ViewModels.DevTools
{
    /// <summary>
    /// ViewModel for theme testing functionality and UI component showcase
    /// </summary>
    public partial class ThemesPageViewModel : ObservableObject
    {
        #region Observable Properties
        
        /// <summary>
        /// Gets or sets whether to use system theme
        /// </summary>
        [ObservableProperty]
        private bool _useSystemTheme;
        
        /// <summary>
        /// Gets or sets whether dark theme is enabled
        /// </summary>
        [ObservableProperty]
        private bool _isDarkTheme;
        
        /// <summary>
        /// Gets or sets the icon text representation for the current theme
        /// </summary>
        [ObservableProperty]
        private string _themeIconText;
        
        /// <summary>
        /// Gets or sets the toggle button text for switching themes
        /// </summary>
        [ObservableProperty]
        private string _themeToggleText;
        
        /// <summary>
        /// Gets or sets the text displaying current theme
        /// </summary>
        [ObservableProperty]
        private string _currentThemeText;
        
        /// <summary>
        /// Gets or sets the text displaying system theme info
        /// </summary>
        [ObservableProperty]
        private string _systemThemeText;
        
        /// <summary>
        /// Gets or sets the currently selected AI model
        /// </summary>
        [ObservableProperty]
        private string _selectedModel = "GPT-4 Turbo";
        
        /// <summary>
        /// Gets or sets whether components are currently loading
        /// </summary>
        [ObservableProperty]
        private bool _isLoading;
        
        /// <summary>
        /// Gets or sets the last copied color
        /// </summary>
        [ObservableProperty]
        private string _lastCopiedColor;
        
        /// <summary>
        /// Gets or sets the currently selected component
        /// </summary>
        [ObservableProperty]
        private string _selectedComponent;

        [ObservableProperty]
        private bool _isComponentLoading;

        [ObservableProperty]
        private bool _isPageLoading = true;

        [ObservableProperty]
        private ObservableCollection<string> _componentCategories;
        
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
            
            // Initialize theme state
            _isDarkTheme = ThemeManager.IsDarkTheme;
            _useSystemTheme = Preferences.Default.Get("theme", "System") == "System";
            
            // Initialize text properties
            UpdateThemeText();
            
            // Initialize icons collection
            InitializeIcons();
            
            // Generate font debug info
            GenerateFontDebugInfo();
            
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
        }
        
        /// <summary>
        /// Selects a component to display
        /// </summary>
        public void SelectComponent(string componentName)
        {
            Debug.WriteLine($"ThemesPageViewModel: SelectComponent {componentName}");
            IsComponentLoading = true;
            SelectedComponent = componentName;
            ComponentChanged?.Invoke(this, componentName);
        }
        
        /// <summary>
        /// Toggles between light and dark themes
        /// </summary>
        public void ToggleTheme()
        {
            try
            {
                // Directly use ThemeManager to change the theme
                // This will affect the entire application, not just the current page
                bool newTheme = !IsDarkTheme; 
                ThemeManager.SetTheme(newTheme);
                
                // Local UI will be updated via ThemeChanged event that we're subscribed to
                // No need to manually set IsDarkTheme here as the event handler will do it
                Debug.WriteLine($"Theme toggled to {(newTheme ? "Dark" : "Light")} mode");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error toggling theme: {ex.Message}");
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
            CurrentThemeText = IsDarkTheme ? "Dark" : "Light";
            
            // Use direct FontAwesome characters, not HTML entities
            ThemeIconText = UseSystemTheme ? "\uf042" : (IsDarkTheme ? "\uf186" : "\uf185");
            ThemeToggleText = IsDarkTheme ? "Switch to Light" : "Switch to Dark";
            
            var systemTheme = Application.Current.RequestedTheme == AppTheme.Dark ? "Dark" : "Light";
            SystemThemeText = systemTheme;
        }
        
        /// <summary>
        /// React to theme changes from other sources
        /// </summary>
        private void OnThemeChanged(object sender, bool isDark)
        {
            IsDarkTheme = isDark;
            UpdateThemeText();
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
        /// Initializes the ViewModel asynchronously
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                // Set loading state
                IsLoading = true;
                IsPageLoading = true;
                
                // Update theme information - don't change themes, just read state
                UpdateThemeText();
                
                // Ensure all properties are properly notified
                OnPropertyChanged(nameof(IsNotUsingSystemTheme));
                
                // Give UI time to render before loading components
                await Task.Delay(300);
                
                // Update loading state
                IsPageLoading = false;
                
                // Select default component - delay actual component loading
                await Task.Delay(100);
                SelectComponent("Colors");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing themes page: {ex.Message}");
                IsPageLoading = false;
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
        /// Initializes only basic properties to speed up page loading
        /// </summary>
        public async Task InitializeBasicPropertiesAsync()
        {
            Debug.WriteLine("ThemesPageViewModel: InitializeBasicPropertiesAsync");
            
            try
            {
                // Set loading state
                IsPageLoading = true;
                
                // Only read theme properties - don't change themes
                _isDarkTheme = ThemeManager.IsDarkTheme;
                CurrentThemeText = _isDarkTheme ? "Dark" : "Light";
                
                var systemTheme = Application.Current.RequestedTheme == AppTheme.Dark ? "Dark" : "Light";
                SystemThemeText = systemTheme;
                
                // Read system theme setting from preferences
                _useSystemTheme = Preferences.Default.Get("theme", "System") == "System";
                
                // Update FontAwesome icon representing theme
                ThemeIconText = UseSystemTheme ? "\uf042" : (_isDarkTheme ? "\uf186" : "\uf185");
                ThemeToggleText = _isDarkTheme ? "Switch to Light" : "Switch to Dark";
                
                // Notify calculated properties
                OnPropertyChanged(nameof(IsNotUsingSystemTheme));
                
                // Subscribe to theme changes
                ThemeManager.ThemeChanged += OnThemeChanged;
                
                Debug.WriteLine("ThemesPageViewModel: Basic properties initialized");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing basic properties: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Completes the initialization after the UI is stable
        /// </summary>
        public async Task CompleteInitializationAsync()
        {
            try
            {
                // Update theme state
                _isDarkTheme = ThemeManager.IsDarkTheme;
                
                // Update FontAwesome icon representing theme
                ThemeIconText = UseSystemTheme ? "\uf042" : (IsDarkTheme ? "\uf186" : "\uf185");
                ThemeToggleText = IsDarkTheme ? "Switch to Light" : "Switch to Dark";
                
                // Subscribe to theme changes
                ThemeManager.ThemeChanged += OnThemeChanged;
                
                // Small delay to ensure UI is ready
                await Task.Delay(50);
                
                // Complete loading
                IsPageLoading = false;
                
                // Select default component after page is loaded
                await Task.Delay(200);
                SelectComponent("Colors");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error completing initialization: {ex.Message}");
                IsPageLoading = false;
            }
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
        }
        
        /// <summary>
        /// Initializes properties without changing theme
        /// </summary>
        public void InitializeWithoutThemeChanges()
        {
            Debug.WriteLine("ThemesPageViewModel: InitializeWithoutThemeChanges");
            
            try
            {
                // Only read theme properties - don't change themes
                _isDarkTheme = ThemeManager.IsDarkTheme;
                CurrentThemeText = _isDarkTheme ? "Dark" : "Light";
                
                var systemTheme = Application.Current.RequestedTheme == AppTheme.Dark ? "Dark" : "Light";
                SystemThemeText = systemTheme;
                
                // Read system theme setting from preferences without changing it
                _useSystemTheme = Preferences.Default.Get("theme", "System") == "System";
                
                // Update UI without changing theme
                ThemeIconText = UseSystemTheme ? "\uf042" : (_isDarkTheme ? "\uf186" : "\uf185");
                ThemeToggleText = _isDarkTheme ? "Switch to Light" : "Switch to Dark";
                
                OnPropertyChanged(nameof(IsNotUsingSystemTheme));
                
                // Subscribe to theme changes but don't trigger any
                ThemeManager.ThemeChanged -= OnThemeChanged; // Remove existing listener to prevent duplicates
                ThemeManager.ThemeChanged += OnThemeChanged;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in InitializeWithoutThemeChanges: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Emergency lightweight initialization
        /// </summary>
        public void InitializeEmergency()
        {
            Debug.WriteLine("ThemesPageViewModel: Emergency initialization");
            
            try
            {
                // Set all essential properties without any heavy operations
                IsPageLoading = false;
                IsComponentLoading = false;
                
                // Just get current theme info - don't try to change it
                IsDarkTheme = Application.Current?.RequestedTheme == AppTheme.Dark;
                CurrentThemeText = IsDarkTheme ? "Dark" : "Light";
                SystemThemeText = Application.Current?.RequestedTheme == AppTheme.Dark ? "Dark" : "Light";
                ThemeIconText = IsDarkTheme ? "\uf186" : "\uf185"; 
                ThemeToggleText = IsDarkTheme ? "Switch to Light" : "Switch to Dark";
                UseSystemTheme = true;  // Default to system theme for safety
                
                // Initialize minimal icon list
                if (Icons == null)
                {
                    Icons = new ObservableCollection<IconItem> 
                    {
                        new IconItem { Name = "Star", Code = "\uf005", Category = "Shapes" },
                        new IconItem { Name = "Home", Code = "\uf015", Category = "Navigation" }
                        // Only include the minimal set needed for initial display
                    };
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in emergency initialization: {ex.Message}");
            }
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
        /// Initializes the collection of FontAwesome icons
        /// </summary>
        private void InitializeIcons()
        {
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
