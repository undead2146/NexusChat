using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexusChat.Helpers;
using Microsoft.Maui.Controls;

namespace NexusChat.ViewModels
{
    /// <summary>
    /// ViewModel for the theme testing page
    /// </summary>
    public partial class ThemeTestPageViewModel : ObservableObject, IDisposable
    {
        [ObservableProperty]
        private string _themeToggleText;

        [ObservableProperty]
        private string _themeIconText;

        [ObservableProperty] 
        private Color _iconTextColor;
        
        /// <summary>
        /// Command to toggle between themes
        /// </summary>
        public ICommand ToggleThemeCommand { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ThemeTestPageViewModel()
        {
            ToggleThemeCommand = new RelayCommand(ThemeManager.ToggleTheme);
            ThemeManager.ThemeChanged += OnThemeChanged;
            UpdateThemeUI();
        }

        /// <summary>
        /// Updates UI when theme changes
        /// </summary>
        private void OnThemeChanged(object sender, bool isDark) => UpdateThemeUI();

        /// <summary>
        /// Updates the UI based on current theme
        /// </summary>
        public void UpdateThemeUI()
        {
            MainThread.BeginInvokeOnMainThread(() => 
            {
                bool isDark = ThemeManager.IsDarkTheme;
                
                ThemeToggleText = isDark ? "Switch to Light Theme" : "Switch to Dark Theme";
                ThemeIconText = isDark ? "\uf185" : "\uf186"; // Sun/Moon icon
                IconTextColor = isDark ? Colors.White : Colors.Black;
            });
        }
        
        /// <summary>
        /// Cleanup resources
        /// </summary>
        public void Dispose()
        {
            ThemeManager.ThemeChanged -= OnThemeChanged;
        }
    }
}
