using System;
using System.Diagnostics;

using NexusChat.Core.ViewModels.DevTools;
using NexusChat.Data.Context;
using NexusChat.Helpers;

namespace NexusChat.Views.Pages.DevTools
{
    /// <summary>
    /// Page for viewing and managing database contents
    /// </summary>
    public partial class DatabaseViewerPage : ContentPage
    {
        private readonly DatabaseViewerViewModel _viewModel;
        
        /// <summary>
        /// Initializes a new instance of DatabaseViewerPage with injected ViewModel
        /// </summary>
        public DatabaseViewerPage(DatabaseViewerViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }
        
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            try
            {
                // First initialize the DataGrid in ViewModel using the direct reference
                _viewModel.InitializeDataGrid(DataGrid);
                
                // Then load the data
                await _viewModel.InitializeAsync();
                
                // Ensure UI state is refreshed after loading
                if (_viewModel.HasData)
                {
                    Debug.WriteLine("Data is available, showing grid");
                }
                else
                {
                    Debug.WriteLine("No data available, showing empty message");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnAppearing: {ex.Message}");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _viewModel.Cleanup();
        }
    }
}
