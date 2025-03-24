using System;
using System.Diagnostics;
using NexusChat.Core.ViewModels.DevTools;
using System.Threading.Tasks;

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
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing ModelTestingPage: {ex.Message}");
                DisplayAlert("Initialization Error", "There was an error initializing the page. Please try again.", "OK");
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                if (!_isInitialized)
                {
                    // Use a timeout to prevent hanging
                    var initTask = _viewModel.InitializeAsync();
                    
                    // Add a 5-second timeout
                    var timeoutTask = Task.Delay(5000);
                    
                    var completedTask = await Task.WhenAny(initTask, timeoutTask);
                    
                    if (completedTask == timeoutTask)
                    {
                        Debug.WriteLine("ViewModel initialization timed out");
                        await DisplayAlert("Warning", "Page initialization timed out. Some features may not work properly.", "OK");
                    }
                    else
                    {
                        _isInitialized = true;
                    }
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
