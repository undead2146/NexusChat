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
            await _viewModel.InitializeAsync();
            
            // Initialize the data grid if needed
            if (Content is Grid mainGrid && mainGrid.FindByName("DataGrid") is Grid dataGrid)
            {
                _viewModel.InitializeDataGrid(dataGrid);
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _viewModel.Cleanup();
        }
    }
}
