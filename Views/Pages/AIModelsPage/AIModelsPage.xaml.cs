using System;
using System.Diagnostics;
using Microsoft.Maui.Controls;
using NexusChat.Core.ViewModels;
using NexusChat.Core.Models;
using NexusChat.Services.Interfaces;

namespace NexusChat.Views.Pages
{
    /// <summary>
    /// Page for browsing and selecting AI models
    /// </summary>
    public partial class AIModelsPage : ContentPage
    {
        private readonly AIModelsViewModel _viewModel;
        private readonly IApiKeyManager _apiKeyManager;

        /// <summary>
        /// Creates a new AIModelsPage
        /// </summary>
        public AIModelsPage(AIModelsViewModel viewModel, IApiKeyManager apiKeyManager)
        {
            InitializeComponent();
            
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _apiKeyManager = apiKeyManager ?? throw new ArgumentNullException(nameof(apiKeyManager));
            
            BindingContext = _viewModel;
            
            ConfigureStatusBar();
            
            // Subscribe to scroll to model requests
            _viewModel.ScrollToModelRequested += OnScrollToModelRequested;
        }

        /// <summary>
        /// Configures status bar and safe area behavior
        /// </summary>
        private void ConfigureStatusBar()
        {
#if ANDROID
            Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.Application.SetWindowSoftInputModeAdjust(
                this, Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.WindowSoftInputModeAdjust.Resize);
#endif
        }

        /// <summary>
        /// Called when the page appears
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
#if IOS
            var statusBarManager = Microsoft.Maui.Controls.Application.Current?.Handler?.PlatformView;
            if (statusBarManager != null)
            {
                // Handle iOS status bar
            }
#endif
            
            // The PageAppearing command will handle all initialization
            if (BindingContext is AIModelsViewModel viewModel)
            {
                await viewModel.PageAppearingCommand.ExecuteAsync(null);
            }
        }

        /// <summary>
        /// Called when the page disappears
        /// </summary>
        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            
            // Cancel any ongoing loading when leaving the page
            if (BindingContext is AIModelsViewModel viewModel)
            {
                await viewModel.PageDisappearingCommand.ExecuteAsync(null);
            }
        }

        /// <summary>
        /// Scrolls to the specified model for UI interaction
        /// </summary>
        private void OnScrollToModelRequested(AIModel model)
        {
            try
            {
                if (model == null)
                {
                    Debug.WriteLine("AIModelsPage: ScrollToModelRequested with null model");
                    return;
                }

                Debug.WriteLine($"AIModelsPage: Scrolling to model {model.ProviderName}/{model.ModelName}");
                
                // Since we removed x:Name attributes, we need to find the CollectionView programmatically
                var collectionView = FindCollectionViewInVisualTree(this);
                if (collectionView != null)
                {
                    collectionView.ScrollTo(model, position: ScrollToPosition.MakeVisible, animate: true);
                }
                else
                {
                    Debug.WriteLine("AIModelsPage: CollectionView not found in visual tree");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AIModelsPage: Error scrolling to model: {ex.Message}");
            }
        }

        private CollectionView FindCollectionViewInVisualTree(Element element)
        {
            if (element is CollectionView collectionView)
                return collectionView;

            if (element is Layout layout)
            {
                foreach (var child in layout.Children)
                {
                    if (child is Element childElement)
                    {
                        var result = FindCollectionViewInVisualTree(childElement);
                        if (result != null)
                            return result;
                    }
                }
            }
            else if (element is ContentView contentView && contentView.Content is Element contentElement)
            {
                return FindCollectionViewInVisualTree(contentElement);
            }
            else if (element is ScrollView scrollView && scrollView.Content is Element scrollContentElement)
            {
                return FindCollectionViewInVisualTree(scrollContentElement);
            }

            return null;
        }

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();
            
            if (BindingContext is AIModelsViewModel viewModel)
            {
                Debug.WriteLine("AIModelsPage: Binding context set to AIModelsViewModel");
            }
        }

        ~AIModelsPage()
        {
            // Unsubscribe from events to prevent memory leaks
            if (_viewModel != null)
            {
                _viewModel.ScrollToModelRequested -= OnScrollToModelRequested;
            }
        }
    }
}



