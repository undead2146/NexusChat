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
            
            ConfigureStatusBar();
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

        protected override void OnAppearing()
        {
            base.OnAppearing();
            
#if IOS
            var statusBarManager = Microsoft.Maui.Controls.Application.Current?.Handler?.PlatformView;
            if (statusBarManager != null)
            {
                // Handle iOS status bar
            }
#endif
        }

        public void Dispose()
        {
            _viewModel?.Dispose();
        }
    }
}
