using Microsoft.Maui.Controls;
using NexusChat.Core.ViewModels;
using System;
using System.Diagnostics;
using NexusChat.Services.Interfaces;

namespace NexusChat.Views.Pages
{
    public partial class MainPage : ContentPage, IDisposable
    {
        private readonly MainPageViewModel _viewModel;

        public MainPage(MainPageViewModel viewModel, INavigationService navigationService)
        {
            try
            {
                // Store reference to the ViewModel
                _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
                
                // Initialize the XAML components
                InitializeComponent();
                
                // Set the BindingContext for data binding
                BindingContext = _viewModel;
                
                Debug.WriteLine("MainPage initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing MainPage: {ex.Message}");
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            try
            {
                // Pass UI references to the ViewModel for components that need direct access
                // This is limited to just the Grid and CounterBtn needed for animations
                _viewModel.InitializeUI((Grid)Content, CounterBtn);
                
                Debug.WriteLine("MainPage appeared successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnAppearing: {ex.Message}");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            
            // Notify ViewModel when page is no longer visible
            _viewModel.Cleanup();
        }

        public void Dispose()
        {
            // Clean up ViewModel resources
            _viewModel?.Dispose();
        }
    }
}
