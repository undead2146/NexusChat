using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexusChat.Data.Context;
using NexusChat.Helpers;
using Microsoft.Maui.Controls;

namespace NexusChat.Core.ViewModels.DevTools
{
    /// <summary>
    /// ViewModel for the database viewer page
    /// </summary>
    public partial class DatabaseViewerViewModel : ObservableObject, IDisposable
    {
        // Helper components
        private readonly DatabaseService _databaseService;
        private readonly DatabaseDataProvider _dataProvider;
        private readonly DatabaseSearchService _searchService;
        private readonly DataObjectConverter _converter;
        
        // UI components
        private DataGridRenderer _dataGridRenderer;
        private Grid _dataGrid;
        
        // Async operation management
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationTokenSource _searchDebounceTokenSource;

        #region Observable Properties

        // Pagination properties
        [ObservableProperty]
        private int _pageSize = 20;

        [ObservableProperty]
        private int _currentPage = 1;

        [ObservableProperty]
        private int _totalPages = 1;

        [ObservableProperty]
        private int _totalRecords = 0;

        // UI State properties
        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private bool _hasData;

        // Data properties
        [ObservableProperty]
        private ObservableCollection<string> _tables = new ObservableCollection<string>();

        [ObservableProperty]
        private string _selectedTable;

        [ObservableProperty]
        private string _statusMessage = "Select a table to view data";

        [ObservableProperty]
        private string _recordCount = "Records: 0";

        [ObservableProperty]
        private ObservableCollection<string> _columnNames = new ObservableCollection<string>();

        [ObservableProperty]
        private ObservableCollection<Dictionary<string, object>> _records = new ObservableCollection<Dictionary<string, object>>();

        // Search functionality
        [ObservableProperty]
        private string _searchText;

        #endregion

        /// <summary>
        /// Gets whether the ViewModel is not busy
        /// </summary>
        public bool IsNotBusy => !IsBusy;

        /// <summary>
        /// Gets whether we should show "No Data" message (only when not busy and no data)
        /// </summary>
        public bool HasNoDataAndNotBusy => !IsBusy && !HasData;

        #region Commands

        /// <summary>
        /// Command to refresh the current data
        /// </summary>
        public IAsyncRelayCommand RefreshDataCommand { get; }

        /// <summary>
        /// Command to navigate back to previous page
        /// </summary>
        public IAsyncRelayCommand GoBackCommand { get; }

        /// <summary>
        /// Command executed when table selection changes
        /// </summary>
        public IAsyncRelayCommand<string> TableChangedCommand { get; }

        /// <summary>
        /// Command to clear the database
        /// </summary>
        public IAsyncRelayCommand ClearDatabaseCommand { get; }

        /// <summary>
        /// Command to navigate to the next page
        /// </summary>
        public IRelayCommand NextPageCommand { get; }

        /// <summary>
        /// Command to navigate to the previous page
        /// </summary>
        public IRelayCommand PreviousPageCommand { get; }

        /// <summary>
        /// Command to cancel the current operation
        /// </summary>
        public IRelayCommand CancelOperationCommand { get; }

        #endregion

        /// <summary>
        /// Initializes a new instance of DatabaseViewerViewModel
        /// </summary>
        /// <param name="databaseService">Database service for data access</param>
        public DatabaseViewerViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            
            // Initialize helper components
            _converter = new DataObjectConverter();
            _dataProvider = new DatabaseDataProvider(databaseService);
            _searchService = new DatabaseSearchService(databaseService, _converter);
            
            // Initialize commands
            RefreshDataCommand = new AsyncRelayCommand(RefreshData);
            GoBackCommand = new AsyncRelayCommand(GoBack);
            TableChangedCommand = new AsyncRelayCommand<string>(LoadTable);
            ClearDatabaseCommand = new AsyncRelayCommand(ClearDatabase);
            
            // Changed from AsyncRelayCommand to RelayCommand with direct method calls
            // Don't use IsNotBusy condition for IsEnabled - this makes the buttons appear disabled
            NextPageCommand = new RelayCommand(LoadNextPage, () => CurrentPage < TotalPages);
            PreviousPageCommand = new RelayCommand(LoadPreviousPage, () => CurrentPage > 1);
            
            CancelOperationCommand = new RelayCommand(CancelCurrentOperation, () => IsBusy);
            
            // Add available tables
            Tables.Add("User");
            Tables.Add("Conversation");
            Tables.Add("Message");
            Tables.Add("AIModel");
            
            if (Tables.Count > 0)
            {
                SelectedTable = Tables[0];
            }
        }
        
        /// <summary>
        /// Initialize the view's data grid
        /// </summary>
        public void InitializeDataGrid(Grid dataGrid)
        {
            _dataGrid = dataGrid;
            _dataGridRenderer = new DataGridRenderer(_dataGrid, Records, ColumnNames);
        }
        
        /// <summary>
        /// Loads data from selected table with pagination
        /// </summary>
        private async Task LoadTable(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
                return;
                
            // Reset pagination
            CurrentPage = 1;
            
            // Cancel any ongoing operations
            CancelOngoingOperations();
            
            IsBusy = true;
            
            // Don't set HasData to false here, so we don't flash "No data" message
            // HasData = false;
            
            StatusMessage = $"Loading {tableName} table...";
            
            try
            {
                // Clear previous data on UI thread
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Records.Clear();
                    ColumnNames.Clear();
                });
                
                // Get total record count first (to set up pagination)
                TotalRecords = await _dataProvider.GetTableRecordCountAsync(tableName);
                TotalPages = (int)Math.Ceiling(TotalRecords / (double)PageSize);
                
                // If no records, show empty state
                if (TotalRecords == 0)
                {
                    StatusMessage = $"No records found in {tableName} table.";
                    RecordCount = "Records: 0";
                    HasData = false;
                    
                    // Make sure the data grid is cleared when empty
                    if (_dataGridRenderer != null)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() => _dataGridRenderer.RebuildDataGrid());
                    }
                    return;
                }
                
                // Load the first page of data
                await LoadPageData(tableName, CurrentPage);
                
                // Update UI state
                RecordCount = $"Records: {Records.Count} of {TotalRecords} (Page {CurrentPage}/{TotalPages})";
                StatusMessage = $"Loaded page {CurrentPage} of {TotalPages} from {tableName}.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading table: {ex.Message}";
                Debug.WriteLine($"Error loading table {tableName}: {ex}");
                HasData = false;
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        /// <summary>
        /// Load a specific page of data with improved performance
        /// </summary>
        private async Task LoadPageData(string tableName, int page, bool append = false)
        {
            if (string.IsNullOrEmpty(tableName) || page < 1)
                return;
                
            try
            {
                // Reset cancellation token
                _cancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = _cancellationTokenSource.Token;
                
                // Calculate offset
                int offset = (page - 1) * PageSize;
                
                StatusMessage = $"Loading page {page} of {TotalPages}...";
                IsBusy = true;
                
                // Load table data in a truly background thread to prevent UI freezing
                var pageData = await Task.Run(async () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return await _dataProvider.LoadTableDataPagedAsync(
                        tableName, PageSize, offset, cancellationToken, _converter);
                }).ConfigureAwait(false); // Use ConfigureAwait(false) for better thread management
                
                // Extract column names if this is the first data we're loading
                if (ColumnNames.Count == 0 && pageData.Any() && pageData.First().Count > 0)
                {
                    var firstRecord = pageData.First();
                    var columns = firstRecord.Keys.ToList();
                    
                    await MainThread.InvokeOnMainThreadAsync(() => 
                    {
                        ColumnNames.Clear();
                        foreach (var key in columns)
                        {
                            ColumnNames.Add(key);
                        }
                    });
                }
                
                // Add records to observable collection on the UI thread
                await MainThread.InvokeOnMainThreadAsync(() => 
                {
                    // Clear if this is a new page or replace if we're filtering
                    if (!append)
                    {
                        Records.Clear();
                    }
                });

                // Add records in batches to prevent UI freezing
                const int batchSize = 20;
                for (int i = 0; i < pageData.Count; i += batchSize)
                {
                    // Get a batch of records
                    var batch = pageData.Skip(i).Take(batchSize).ToList();
                    
                    // Add the batch to the records collection on the UI thread
                    await MainThread.InvokeOnMainThreadAsync(() => 
                    {
                        foreach (var record in batch)
                        {
                            Records.Add(record);
                        }
                    });
                    
                    // Allow UI to update
                    await Task.Delay(1);
                }
                
                // Update UI state
                await MainThread.InvokeOnMainThreadAsync(() => 
                {
                    HasData = Records.Count > 0;
                    RecordCount = append 
                        ? $"Records: {Records.Count} of {TotalRecords} (Pages 1-{page}/{TotalPages})"
                        : $"Records: {Records.Count} of {TotalRecords} (Page {page}/{TotalPages})";
                    
                    // Rebuild the grid
                    _dataGridRenderer?.RebuildDataGrid();
                });
                
                StatusMessage = append 
                    ? $"Loaded records 1-{Records.Count} of {TotalRecords}"
                    : $"Loaded page {page} of {TotalPages} from {tableName}.";
            }
            catch (OperationCanceledException)
            {
                // Operation was cancelled, which is expected during fast navigation
                Debug.WriteLine("Loading operation was cancelled");
                StatusMessage = "Operation canceled.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading page {page}: {ex.Message}";
                Debug.WriteLine($"Error loading page {page}: {ex}");
            }
            finally
            {
                await MainThread.InvokeOnMainThreadAsync(() => IsBusy = false);
                
                // Force command availability update
                await MainThread.InvokeOnMainThreadAsync(() => 
                {
                    (NextPageCommand as RelayCommand)?.NotifyCanExecuteChanged();
                    (PreviousPageCommand as RelayCommand)?.NotifyCanExecuteChanged();
                });
            }
        }
        
        /// <summary>
        /// Searches for data in the current table
        /// </summary>
        private async Task PerformSearch(string searchText)
        {
            if (string.IsNullOrEmpty(SelectedTable) || string.IsNullOrWhiteSpace(searchText))
                return;
            
            try
            {
                IsBusy = true;
                CancelOngoingOperations();
                
                StatusMessage = $"Searching for '{searchText}'...";
                
                // Reset to first page for search
                CurrentPage = 1;
                
                // Get search results with improved performance
                _cancellationTokenSource = new CancellationTokenSource();
                var token = _cancellationTokenSource.Token;
                
                var searchResults = await Task.Run(async () =>
                {
                    token.ThrowIfCancellationRequested();
                    return await _searchService.SearchTableAsync(
                        SelectedTable, searchText, PageSize, token);
                }, token);

                // Clear existing records on UI thread
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Records.Clear();
                });

                // Add records in batches to prevent UI freezing
                const int batchSize = 20;
                for (int i = 0; i < searchResults.Count; i += batchSize)
                {
                    // Get a batch of records
                    var batch = searchResults.Skip(i).Take(batchSize).ToList();
                    
                    // Add the batch to the records collection on the UI thread
                    await MainThread.InvokeOnMainThreadAsync(() => 
                    {
                        foreach (var record in batch)
                        {
                            Records.Add(record);
                        }
                    });
                    
                    // Allow UI to update
                    await Task.Delay(1);
                }
                
                // Update UI state
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    HasData = Records.Count > 0;
                    RecordCount = $"Records: {Records.Count} (search results)";
                    
                    // Rebuild grid
                    _dataGridRenderer?.RebuildDataGrid();
                });
                
                StatusMessage = $"Found {Records.Count} records matching '{searchText}'.";
            }
            catch (OperationCanceledException)
            {
                // Search was cancelled
                StatusMessage = "Search canceled.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error during search: {ex.Message}";
                Debug.WriteLine($"Search error: {ex}");
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        #region Navigation Commands
        
        /// <summary>
        /// Move to the next page - changed to use non-async pattern to avoid UI freezing
        /// </summary>
        private void LoadNextPage()
        {
            if (CurrentPage >= TotalPages)
                return;
                
            CurrentPage++;
            
            // Start task without awaiting to avoid UI freeze
            Task.Run(() => LoadPageData(SelectedTable, CurrentPage));
        }
        
        /// <summary>
        /// Move to the previous page - changed to use non-async pattern to avoid UI freezing
        /// </summary>
        private void LoadPreviousPage()
        {
            if (CurrentPage <= 1)
                return;
                
            CurrentPage--;
            
            // Start task without awaiting to avoid UI freeze
            Task.Run(() => LoadPageData(SelectedTable, CurrentPage));
        }
        
        #endregion
        
        #region Command Handlers
        
        /// <summary>
        /// Refreshes the data for the current table
        /// </summary>
        private async Task RefreshData()
        {
            // Reset to first page and reload on background thread to prevent UI freezing
            CurrentPage = 1;
            
            // Use Task.Run to ensure this runs on a background thread
            await Task.Run(() => LoadTable(SelectedTable));
        }
        
        /// <summary>
        /// Navigates back to the previous page
        /// </summary>
        private async Task GoBack()
        {
            await Shell.Current.GoToAsync("..");
        }
        
        /// <summary>
        /// Cancels the current operation
        /// </summary>
        private void CancelCurrentOperation()
        {
            CancelOngoingOperations();
        }
        
        /// <summary>
        /// Clears all data from the database
        /// </summary>
        private async Task ClearDatabase()
        {
            if (IsBusy) return;
            
            // Confirm with user
            bool confirm = await Application.Current.MainPage.DisplayAlert(
                "Clear Database?", 
                "Are you sure you want to clear all data from the database? This action cannot be undone.", 
                "Yes", "No");
                
            if (!confirm) return;
            
            IsBusy = true;
            StatusMessage = "Clearing database...";
            
            try
            {
                // Use background thread to avoid UI freeze
                await Task.Run(async () => await _dataProvider.ClearDatabaseAsync());
                
                // Update UI state
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Records.Clear();
                    HasData = false;
                    TotalRecords = 0;
                    TotalPages = 1;
                    CurrentPage = 1;
                    RecordCount = "Records: 0";
                    
                    // Rebuild UI
                    _dataGridRenderer?.RebuildDataGrid();
                });
                
                StatusMessage = "Database cleared successfully.";
                await Application.Current.MainPage.DisplayAlert("Success", "Database cleared successfully.", "OK");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error clearing database: {ex.Message}";
                Debug.WriteLine($"Error clearing database: {ex}");
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to clear database: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        #endregion
        
        /// <summary>
        /// Initialize the ViewModel
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                IsBusy = true;
                
                // Set HasData to false during initialization
                HasData = false;
                
                if (Tables.Count > 0 && string.IsNullOrEmpty(SelectedTable))
                {
                    // Set default table selection if none selected
                    SelectedTable = Tables[0];
                    
                    // Load the first table immediately
                    await LoadTable(SelectedTable);
                }
                else if (!string.IsNullOrEmpty(SelectedTable))
                {
                    // If there's already a selected table, load it
                    await LoadTable(SelectedTable);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in InitializeAsync: {ex.Message}");
                StatusMessage = $"Error initializing: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        /// <summary>
        /// Cancels any ongoing database operations
        /// </summary>
        private void CancelOngoingOperations()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = new CancellationTokenSource();
                
                _searchDebounceTokenSource?.Cancel();
                _searchDebounceTokenSource?.Dispose();
                _searchDebounceTokenSource = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error cancelling operations: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Clean up any resources used by the ViewModel
        /// </summary>
        public void Cleanup()
        {
            CancelOngoingOperations();
        }
        
        /// <summary>
        /// Clean up any resources used by the ViewModel
        /// </summary>
        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
            _searchDebounceTokenSource?.Dispose();
            
            // Dispose data provider (which contains the database lock)
            if (_dataProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
            
            GC.SuppressFinalize(this);
        }
        
        #region Property Change Handlers
        
        // Make sure IsBusy changes notify IsNotBusy changes and command availability
        partial void OnIsBusyChanged(bool value)
        {
            OnPropertyChanged(nameof(IsNotBusy));
            OnPropertyChanged(nameof(HasNoDataAndNotBusy));
            
            // Also update command availability
            (NextPageCommand as RelayCommand)?.NotifyCanExecuteChanged();
            (PreviousPageCommand as RelayCommand)?.NotifyCanExecuteChanged();
            (CancelOperationCommand as RelayCommand)?.NotifyCanExecuteChanged();
        }

        // Add debounced search functionality with improved throttling
        partial void OnSearchTextChanged(string value)
        {
            // Cancel any previous search operation
            _searchDebounceTokenSource?.Cancel();
            _searchDebounceTokenSource?.Dispose();
            _searchDebounceTokenSource = new CancellationTokenSource();
            
            // If search text is empty, reload first page
            if (string.IsNullOrWhiteSpace(value))
            {
                // Schedule reload without awaiting to avoid UI freeze
                Task.Run(() => LoadTable(SelectedTable));
                return;
            }
            
            // For short search terms, wait longer to avoid too many searches
            int delayMs = value.Length < 3 ? 700 : 500;
            
            // Schedule search with delay to debounce input - run in background
            Task.Delay(delayMs, _searchDebounceTokenSource.Token)
                .ContinueWith(t => 
                {
                    if (t.IsCompletedSuccessfully)
                    {
                        // Execute search in background without awaiting
                        Task.Run(() => PerformSearch(value));
                    }
                }, TaskContinuationOptions.OnlyOnRanToCompletion);
        }
        
        // Update pagination properties when page size changes
        partial void OnPageSizeChanged(int value)
        {
            if (value <= 0)
            {
                PageSize = 100; // Reset to default if invalid
                return;
            }
            
            // Recalculate total pages
            if (TotalRecords > 0)
            {
                TotalPages = (int)Math.Ceiling(TotalRecords / (double)PageSize);
                
                // Ensure current page is still valid
                if (CurrentPage > TotalPages)
                {
                    CurrentPage = TotalPages;
                }
                   
                // Reload data if we have a selected table
                if (!string.IsNullOrEmpty(SelectedTable))
                {
                    // Force reload with the new page size - run on a background thread
                    Task.Run(() => LoadPageData(SelectedTable, CurrentPage));
                }
            }
        }

        // Add property change handlers for CurrentPage and TotalPages
        partial void OnCurrentPageChanged(int value)
        {
            // Update command availability when current page changes
            (NextPageCommand as RelayCommand)?.NotifyCanExecuteChanged();
            (PreviousPageCommand as RelayCommand)?.NotifyCanExecuteChanged();
        }

        partial void OnTotalPagesChanged(int value)
        {
            // Update command availability when total pages changes
            (NextPageCommand as RelayCommand)?.NotifyCanExecuteChanged();
            (PreviousPageCommand as RelayCommand)?.NotifyCanExecuteChanged();
        }
        
        // Make sure HasData changes notify HasNoDataAndNotBusy
        partial void OnHasDataChanged(bool value)
        {
            OnPropertyChanged(nameof(HasNoDataAndNotBusy));
        }
        
        #endregion
    }
}
