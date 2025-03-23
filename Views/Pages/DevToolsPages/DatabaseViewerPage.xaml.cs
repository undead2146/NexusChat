using System;
using System.Diagnostics;
using Microsoft.Maui.Controls;
using NexusChat.Data;
using NexusChat.Helpers;
using NexusChat.ViewModels;

namespace NexusChat.Views
{
    /// <summary>
    /// Page for viewing and exploring database contents
    /// </summary>
    public partial class DatabaseViewerPage : ContentPage
    {
        private DatabaseViewerViewModel _viewModel;
        private DataGridRenderer _dataGridRenderer;
        private readonly DatabaseService _databaseService;
        
        /// <summary>
        /// Creates a new instance of the DatabaseViewerPage
        /// </summary>
        /// <param name="databaseService">Database service dependency</param>
        public DatabaseViewerPage(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            
            try
            {
                InitializeComponent();
                // No need to show a loading indicator during initialization
                // It will be controlled by the IsBusy property
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing DatabaseViewerPage: {ex.Message}");
            }
        }
        
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            try
            {
                // One-time initialization
                if (_viewModel == null)
                {
                    // Initialize ViewModel with dependencies
                    _viewModel = new DatabaseViewerViewModel(_databaseService);
                    BindingContext = _viewModel;
                    
                    // Create data grid renderer
                    _dataGridRenderer = new DataGridRenderer(
                        DataGrid, 
                        _viewModel.Records, 
                        _viewModel.ColumnNames
                    );
                    
                    // Load initial data - this will set IsBusy appropriately
                    await _viewModel.InitializeData();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting up DatabaseViewer: {ex.Message}");
                await DisplayAlert("Error", $"Failed to load database viewer: {ex.Message}", "OK");
            }
        }
        
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            
            // Clean up renderer
            if (_dataGridRenderer != null)
            {
                _dataGridRenderer.CleanUp();
            }
            
            // Cancel any running operations
            _viewModel?.CancelOperations();
        }
    }
}
