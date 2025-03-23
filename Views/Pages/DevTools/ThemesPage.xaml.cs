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
        private bool _isNavigatingAway = false;
        private bool _isComponentLoading = false;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        
        // Use lazy initialization for all components
        private Lazy<ColorPalette> _colorPalette;
        private Lazy<FunctionalColors> _functionalColors;
        private Lazy<Typography> _typography;
        private Lazy<Buttons> _buttons;
        private Lazy<InputControls> _inputControls; 
        private Lazy<ChatComponents> _chatComponents;
        private Lazy<StatusIndicators> _statusIndicators;
        private Lazy<LayoutComponents> _layoutComponents;
        private Lazy<FormComponents> _formComponents;
        private Lazy<Accessibilities> _accessibility;
        private Lazy<Icons> _icons; 

        /// <summary>
        /// Initializes a new instance of ThemesPage with injected ViewModel
        /// </summary>
        public ThemesPage(ThemesPageViewModel viewModel)
        {
            try
            {
                Debug.WriteLine("ThemesPage: Constructor start - OPTIMIZED VERSION");
                
                // Just do the essential UI initialization - nothing else
                InitializeComponent();
                _viewModel = viewModel;
                BindingContext = _viewModel;
                
                // Prevent resources from being initialized immediately
                _isComponentLoading = false;
                _isFirstLoad = true;
                
                Debug.WriteLine("ThemesPage: Constructor completed successfully");
            }
            catch (Exception ex)
            {
                // Log the error but don't crash
                Debug.WriteLine($"Critical error initializing ThemesPage: {ex.Message}");
            }
        }

        /// <summary>
        /// Initialize all components with lazy loading for maximum performance
        /// </summary>
        private void InitializeLazyComponents()
        {
            // Only called when needed, not during constructor
            Debug.WriteLine("ThemesPage: Initializing lazy components on demand");
            
            try
            {
                // Use lazy initialization for everything
                _colorPalette = new Lazy<ColorPalette>(() => new ColorPalette { BindingContext = _viewModel });
                _typography = new Lazy<Typography>(() => new Typography { BindingContext = _viewModel });
                _buttons = new Lazy<Buttons>(() => new Buttons { BindingContext = _viewModel });
                _functionalColors = new Lazy<FunctionalColors>(() => new FunctionalColors { BindingContext = _viewModel });
                _inputControls = new Lazy<InputControls>(() => new InputControls { BindingContext = _viewModel });
                _chatComponents = new Lazy<ChatComponents>(() => new ChatComponents { BindingContext = _viewModel });
                _statusIndicators = new Lazy<StatusIndicators>(() => new StatusIndicators { BindingContext = _viewModel });
                _layoutComponents = new Lazy<LayoutComponents>(() => new LayoutComponents { BindingContext = _viewModel });
                _formComponents = new Lazy<FormComponents>(() => new FormComponents { BindingContext = _viewModel });
                _accessibility = new Lazy<Accessibilities>(() => new Accessibilities { BindingContext = _viewModel });
                _icons = new Lazy<Icons>(() => new Icons { BindingContext = _viewModel });
                
                // Set up component change handler
                _viewModel.ComponentChanged += OnComponentChanged;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in InitializeLazyComponents (continuing): {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handles theme switch toggle events
        /// </summary>
        private void ThemeSwitch_Toggled(object sender, ToggledEventArgs e)
        {
            if (_viewModel != null && !_viewModel.UseSystemTheme)
            {
                _viewModel.IsDarkTheme = e.Value;
                _viewModel.ToggleTheme();
            }
        }
        
        /// <summary>
        /// Handles system theme switch toggle events
        /// </summary>
        private void SystemThemeSwitch_Toggled(object sender, ToggledEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.ToggleUseSystemTheme();
            }
        }
        
        /// <summary>
        /// Apply optimizations when page appears to prevent UI freezes
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();
            Debug.WriteLine("ThemesPage: OnAppearing - ULTRA MINIMAL");
            
            try
            {
                _cts = new CancellationTokenSource();
                
                // Show the page immediately
                _viewModel.IsPageLoading = false;
                
                // Defer almost all work to be done after UI is visible
                if (_isFirstLoad)
                {
                    _isFirstLoad = false;
                    
                    // Dispatch with delay to ensure UI thread isn't blocked
                    Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(200), () => 
                    {
                        try
                        {
                            // Do only essential operations
                            _viewModel.InitializeEmergency();
                            
                            // Initialize component references only when needed
                            InitializeEssentialComponents();
                            
                            // Later, select first component
                            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(500), () =>
                            {
                                if (!_isNavigatingAway && !_cts.IsCancellationRequested)
                                {
                                    _viewModel.SelectComponent("Colors");
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error in delayed initialization: {ex.Message}");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnAppearing: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Initialize only essential components to avoid memory pressure
        /// </summary>
        private void InitializeEssentialComponents()
        {
            Debug.WriteLine("ThemesPage: Initializing essential components only");
            
            try
            {
                // Only create the color palette initially - others will be created on demand
                _colorPalette = new Lazy<ColorPalette>(() => new ColorPalette { BindingContext = _viewModel });
                
                // Set up component change handler
                _viewModel.ComponentChanged += OnComponentChanged;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing components: {ex.Message}");
            }
        }

        /// <summary>
        /// Lazy initialization for a specific component type
        /// </summary>
        private T GetOrCreateComponent<T>(ref Lazy<T> component) where T : ContentView, new()
        {
            if (component == null)
            {
                Debug.WriteLine($"Creating component: {typeof(T).Name}");
                component = new Lazy<T>(() => new T { BindingContext = _viewModel });
            }
            return component.Value;
        }
        
        /// <summary>
        /// Handles component selection changes with optimized loading
        /// </summary>
        private void OnComponentChanged(object sender, string componentName)
        {
            // Prevent loading if navigating away or already loading
            if (_isNavigatingAway || _isComponentLoading || _cts.IsCancellationRequested)
                return;
                
            _isComponentLoading = true;
            Debug.WriteLine($"ThemesPage: OnComponentChanged to {componentName}");
            
            // Use the Dispatcher for better UI responsiveness
            Dispatcher.Dispatch(async () => 
            {
                try
                {
                    // Clear current content first for better performance
                    ComponentDisplay.Content = null;
                    await Task.Delay(10, _cts.Token);
                    
                    if (_cts.IsCancellationRequested)
                        return;
                    
                    // Show the component based on selection
                    switch (componentName)
                    {
                        case "Colors":
                            if (_colorPalette == null)
                                _colorPalette = new Lazy<ColorPalette>(() => new ColorPalette { BindingContext = _viewModel });
                            ComponentDisplay.Content = _colorPalette.Value;
                            break;
                        case "FunctionalColors":
                            if (_functionalColors == null)
                                _functionalColors = new Lazy<FunctionalColors>(() => new FunctionalColors { BindingContext = _viewModel });
                            ComponentDisplay.Content = _functionalColors.Value;
                            break;
                        case "Typography":
                            if (_typography == null)
                                _typography = new Lazy<Typography>(() => new Typography { BindingContext = _viewModel });
                            ComponentDisplay.Content = _typography.Value;
                            break;
                        case "Buttons":
                            if (_buttons == null)
                                _buttons = new Lazy<Buttons>(() => new Buttons { BindingContext = _viewModel });
                            ComponentDisplay.Content = _buttons.Value;
                            break;
                        case "InputControls":
                            if (_inputControls == null)
                                _inputControls = new Lazy<InputControls>(() => new InputControls { BindingContext = _viewModel });
                            ComponentDisplay.Content = _inputControls.Value;
                            break;
                        case "ChatComponents":
                            if (_chatComponents == null)
                                _chatComponents = new Lazy<ChatComponents>(() => new ChatComponents { BindingContext = _viewModel });
                            ComponentDisplay.Content = _chatComponents.Value;
                            break;
                        case "StatusIndicators":
                            if (_statusIndicators == null)
                                _statusIndicators = new Lazy<StatusIndicators>(() => new StatusIndicators { BindingContext = _viewModel });
                            ComponentDisplay.Content = _statusIndicators.Value;
                            break;
                        case "LayoutComponents":
                            if (_layoutComponents == null)
                                _layoutComponents = new Lazy<LayoutComponents>(() => new LayoutComponents { BindingContext = _viewModel });
                            ComponentDisplay.Content = _layoutComponents.Value;
                            break;
                        case "FormComponents":
                            if (_formComponents == null)
                                _formComponents = new Lazy<FormComponents>(() => new FormComponents { BindingContext = _viewModel });
                            ComponentDisplay.Content = _formComponents.Value;
                            break;
                        case "Accessibility":
                            if (_accessibility == null)
                                _accessibility = new Lazy<Accessibilities>(() => new Accessibilities { BindingContext = _viewModel });
                            ComponentDisplay.Content = _accessibility.Value;
                            break;
                        case "Icons": // Add the new Icons case
                            if (_icons == null)
                                _icons = new Lazy<Icons>(() => new Icons { BindingContext = _viewModel });
                            ComponentDisplay.Content = _icons.Value;
                            break;
                    }
                    
                    Debug.WriteLine($"ThemesPage: Component {componentName} loaded");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error changing component: {ex.Message}");
                }
                finally
                {
                    // Signal completion
                    _isComponentLoading = false;
                    _viewModel.ComponentLoadingComplete();
                }
            });
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Debug.WriteLine("ThemesPage: OnDisappearing");
            
            // Cancel any pending operations first
            _cts.Cancel();
            _cts.Dispose();
            
            // Mark as navigating away to prevent more operations
            _isNavigatingAway = true;
            
            // Ensure cleanup when page is removed
            _viewModel.Cleanup();
            
            // Unsubscribe from events
            _viewModel.ComponentChanged -= OnComponentChanged;
            
            // Clear content reference
            ComponentDisplay.Content = null;
            
            // Force memory cleanup
            DisposeComponents();
            
            // Force GC to release memory
            GC.Collect(0, GCCollectionMode.Forced);
            
            Debug.WriteLine("ThemesPage: Cleanup completed");
        }

        /// <summary>
        /// Properly dispose of all component instances
        /// </summary>
        private void DisposeComponents()
        {
            // Clear lazy loading components and ensure they're released
            if (_colorPalette?.IsValueCreated == true && _colorPalette.Value is IDisposable disposablePalette)
                disposablePalette.Dispose();
                
            if (_typography?.IsValueCreated == true && _typography.Value is IDisposable disposableTypography)
                disposableTypography.Dispose();
                
            if (_buttons?.IsValueCreated == true && _buttons.Value is IDisposable disposableButtons)
                disposableButtons.Dispose();
                
            if (_functionalColors?.IsValueCreated == true && _functionalColors.Value is IDisposable disposableFunctional)
                disposableFunctional.Dispose();
                
            if (_inputControls?.IsValueCreated == true && _inputControls.Value is IDisposable disposableInput)
                disposableInput.Dispose();
                
            if (_chatComponents?.IsValueCreated == true && _chatComponents.Value is IDisposable disposableChat)
                disposableChat.Dispose();
                
            if (_statusIndicators?.IsValueCreated == true && _statusIndicators.Value is IDisposable disposableStatus)
                disposableStatus.Dispose();
                
            if (_layoutComponents?.IsValueCreated == true && _layoutComponents.Value is IDisposable disposableLayout)
                disposableLayout.Dispose();
                
            if (_formComponents?.IsValueCreated == true && _formComponents.Value is IDisposable disposableForm)
                disposableForm.Dispose();
                
            if (_accessibility?.IsValueCreated == true && _accessibility.Value is IDisposable disposableAccess)
                disposableAccess.Dispose();
                
            if (_icons?.IsValueCreated == true && _icons.Value is IDisposable disposableIcons)
                disposableIcons.Dispose();
                
            // Clear all references
            _colorPalette = null;
            _typography = null;
            _buttons = null;
            _functionalColors = null;
            _inputControls = null;
            _chatComponents = null;
            _statusIndicators = null;
            _layoutComponents = null;
            _formComponents = null;
            _accessibility = null;
            _icons = null;
        }
    }
}
