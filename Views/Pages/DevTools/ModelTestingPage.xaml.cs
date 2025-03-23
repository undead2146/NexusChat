using NexusChat.Core.ViewModels.DevTools;

namespace NexusChat.Views.Pages.DevTools
{
    /// <summary>
    /// Page for testing AI models
    /// </summary>
    public partial class ModelTestingPage : ContentPage
    {
        private readonly ModelTestingViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance of ModelTestingPage with injected ViewModel
        /// </summary>
        public ModelTestingPage(ModelTestingViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.InitializeAsync();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _viewModel.Cleanup();
        }
    }
}
