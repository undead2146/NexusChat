using System;
using System.Diagnostics;
using NexusChat.Core.ViewModels.DevTools;
using System.Threading.Tasks;
using NexusChat.Views.Converters;
using Microsoft.Maui.Controls;

namespace NexusChat.Views.Pages.DevTools
{
    /// <summary>
    /// Page for testing AI models and generating test data
    /// </summary>
    public partial class ModelTestingPage : ContentPage
    {
        private readonly ModelTestingViewModel _viewModel;
        private bool _isInitialized = false;

        /// <summary>
        /// Initializes a new instance of ModelTestingPage with injected ViewModel
        /// </summary>
        public ModelTestingPage(ModelTestingViewModel viewModel)
        {
            try
            {
                InitializeComponent();
                _viewModel = viewModel;
                BindingContext = _viewModel;

                // Add required converters to resources
                Resources.Add("NotNullConverter", new NotNullConverter());
                Resources.Add("MessageIconConverter", new MessageIconConverter());
                Resources.Add("MessageAuthorConverter", new MessageAuthorConverter());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing ModelTestingPage: {ex.Message}");
                DisplayAlert("Error", "Failed to initialize the page", "OK");
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                if (!_isInitialized)
                {
                    await _viewModel.InitializeAsync();
                    _isInitialized = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ModelTestingPage OnAppearing: {ex.Message}");
                await DisplayAlert("Error", "An error occurred while loading the page. Some features may not be available.", "OK");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            try
            {
                _viewModel.Cleanup();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnDisappearing: {ex.Message}");
            }
        }
    }
}
