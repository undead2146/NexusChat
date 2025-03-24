using System;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Core.ViewModels.DevTools;
using NexusChat.Views.Controls;
using Microsoft.Maui.Dispatching;
using System.Diagnostics;

namespace NexusChat.Views.Pages.DevTools
{
    /// <summary>
    /// Page to test theme functionality and showcase UI components
    /// </summary>
    public partial class ThemesPage : ContentPage
    {
        private readonly ThemesPageViewModel _viewModel;
        private bool _isFirstLoad = true;
        private CancellationTokenSource _cts = new CancellationTokenSource();

        /// <summary>
        /// Initializes a new instance of ThemesPage with injected ViewModel
        /// </summary>
        public ThemesPage(ThemesPageViewModel viewModel)
        {
            try
            {
                Debug.WriteLine("ThemesPage: Constructor start");
                InitializeComponent();
                _viewModel = viewModel;
                BindingContext = _viewModel;

                // Set up the ViewModel's component changed event
                _viewModel.ComponentChanged += OnComponentChanged;
                
                Debug.WriteLine("ThemesPage: Constructor completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing ThemesPage: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles theme switch toggle events
        /// </summary>
        private void ThemeSwitch_Toggled(object sender, ToggledEventArgs e)
        {
            try
            {
                if (_viewModel != null && !_viewModel.UseSystemTheme)
                {
                    // Update ViewModel for UI consistency
                    _viewModel.IsDarkTheme = e.Value;
                    
                    // Toggle theme (uses ThemeManager)
                    _viewModel.ToggleTheme();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ThemeSwitch_Toggled: {ex.Message}");
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            Debug.WriteLine("ThemesPage: OnAppearing");
            
            try
            {
                // Create a new cancellation token
                _cts = new CancellationTokenSource();
                
                if (_isFirstLoad)
                {
                    _isFirstLoad = false;
                    
                    // Initialize the ViewModel
                    await _viewModel.InitializeAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnAppearing: {ex.Message}");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Debug.WriteLine("ThemesPage: OnDisappearing");
            
            try
            {
                // Cancel any pending operations
                _cts.Cancel();
                _cts.Dispose();
                
                // Clean up the ViewModel
                _viewModel.Cleanup();
                
                // Clean up component references
                ComponentDisplay.Content = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnDisappearing: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles component selection changes
        /// </summary>
        private async void OnComponentChanged(object sender, string componentName)
        {
            if (string.IsNullOrEmpty(componentName))
                return;
                
            Debug.WriteLine($"ThemesPage: OnComponentChanged to {componentName}");
            
            try
            {
                // Clear current content first
                ComponentDisplay.Content = null;
                
                // Small delay to ensure UI updates
                await Task.Delay(10, _cts.Token);
                
                // Create component based on selection
                View component = null;
                
                switch (componentName)
                {
                    case "Colors":
                        component = new ColorPalette();
                        break;
                    case "Typography":
                        component = new Typography();
                        break;
                    case "Buttons":
                        component = new Buttons();
                        break;
                    case "InputControls":
                        component = new InputControls();
                        break;
                    case "ChatComponents":
                        component = new ChatComponents();
                        break;
                    case "StatusIndicators":
                        component = new StatusIndicators();
                        break;
                    case "LayoutComponents":
                        component = new LayoutComponents();
                        break;
                    case "FormComponents":
                        component = new FormComponents();
                        break;
                    case "Accessibility":
                        component = new Accessibilities();
                        break;
                    case "Icons":
                        component = new Icons();
                        break;
                    default:
                        component = new Label 
                        { 
                            Text = $"Component '{componentName}' not available",
                            HorizontalOptions = LayoutOptions.Center
                        };
                        break;
                }
                
                // Set binding context to our ViewModel
                if (component != null)
                {
                    component.BindingContext = _viewModel;
                }
                
                // Add to display
                ComponentDisplay.Content = component;
                
                // Mark loading complete
                _viewModel.ComponentLoadingComplete();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading component {componentName}: {ex.Message}");
                _viewModel.ComponentLoadingComplete();
            }
        }
    }
}
