using Microsoft.Maui.Controls;
using NexusChat.ViewModels;

namespace NexusChat.Views
{
    public partial class ModelTestingPage : ContentPage
    {
        private ModelTestingViewModel _viewModel;
        
        public ModelTestingPage()
        {
            InitializeComponent();
            _viewModel = new ModelTestingViewModel();
            BindingContext = _viewModel;
        }
        
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (_viewModel is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
