using Microsoft.Maui.Controls;
using NexusChat.Core.ViewModels;
using System;
using System.Diagnostics;

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
            
            ConfigureStatusBar();
        }

        private void ConfigureStatusBar()
        {
#if ANDROID
            Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.Application.SetWindowSoftInputModeAdjust(
                this, Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.WindowSoftInputModeAdjust.Resize);
#endif
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
        }

        public void Dispose()
        {
            _viewModel?.Dispose();
        }
    }
}
