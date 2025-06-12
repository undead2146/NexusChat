using Microsoft.Maui.Controls;
using NexusChat.Core.ViewModels;
using System;

namespace NexusChat.Views.Pages
{
    public partial class MainPage : ContentPage, IDisposable
    {
        private readonly MainPageViewModel _viewModel;

        public MainPage(MainPageViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            InitializeComponent();
            BindingContext = _viewModel;
        }

        public void Dispose()
        {
            _viewModel?.Dispose();
        }
    }
}
