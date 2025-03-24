using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexusChat.Core.Models;
using NexusChat.Data.Context;
using NexusChat.Helpers;
using SQLite;
using Microsoft.Maui.Controls;

namespace NexusChat.Core.ViewModels.DevTools
{
    /// <summary>
    /// ViewModel for the database viewer page
    /// </summary>
    public partial class DatabaseViewerViewModel : ObservableObject
    {
        private readonly DatabaseService _databaseService;
        private DataGridRenderer _dataGridRenderer;
        private Grid _dataGrid;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _hasData;

        [ObservableProperty]
        private ObservableCollection<string> _tables = new ObservableCollection<string>();

        [ObservableProperty]
        private ObservableCollection<string> _tableNames = new ObservableCollection<string>();

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

        [ObservableProperty]
        private ObservableCollection<object> _tableRecords = new ObservableCollection<object>();

        // Add search functionality
        [ObservableProperty]
        private string _searchText;

        // Add field to store all records
        private List<Dictionary<string, object>> _allRecords = new List<Dictionary<string, object>>();

        /// <summary>
        /// Gets whether the ViewModel is not busy
        /// </summary>
        public bool IsNotBusy => !IsBusy;

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
        /// Initializes a new instance of DatabaseViewerViewModel
        /// </summary>
        /// <param name="databaseService">Database service for data access</param>
        public DatabaseViewerViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            
            RefreshDataCommand = new AsyncRelayCommand(RefreshData);
            GoBackCommand = new AsyncRelayCommand(GoBack);
            TableChangedCommand = new AsyncRelayCommand<string>(LoadTable);
            ClearDatabaseCommand = new AsyncRelayCommand(ClearDatabase);
            
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
        /// Loads data from selected table
        /// </summary>
        private async Task LoadTable(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
                return;
                
            IsBusy = true;
            HasData = false; // Set to false initially while loading
            StatusMessage = $"Loading {tableName} table...";
            
            try
            {
                await _databaseService.Initialize();
                
                // Clear previous data
                Records.Clear();
                ColumnNames.Clear();
                _allRecords.Clear();
                
                // Load table data
                var tableData = await LoadTableData(tableName);
                _allRecords = tableData.ToList();
                
                if (_allRecords.Count == 0)
                {
                    StatusMessage = $"No records found in {tableName} table.";
                    RecordCount = "Records: 0";
                    HasData = false; // No data available
                    
                    // Make sure the data grid is cleared when empty
                    if (_dataGridRenderer != null)
                    {
                        _dataGridRenderer.RebuildDataGrid();
                    }
                    return;
                }
                
                // Extract column names from first record
                if (_allRecords.Any() && _allRecords.First().Count > 0)
                {
                    var firstRecord = _allRecords.First();
                    foreach (var key in firstRecord.Keys)
                    {
                        ColumnNames.Add(key);
                    }
                }
                
                // Add all records to observable collection
                Records.Clear();
                foreach (var record in _allRecords)
                {
                    Records.Add(record);
                }
                
                // Update UI - important to set HasData to true AFTER adding records
                RecordCount = $"Records: {Records.Count}";
                StatusMessage = $"Loaded {Records.Count} records from {tableName}.";
                
                // Rebuild the grid BEFORE setting HasData to true
                if (_dataGridRenderer != null)
                {
                    _dataGridRenderer.RebuildDataGrid();
                }
                
                // Set HasData after rebuilding grid to ensure UI switches at right time
                HasData = Records.Count > 0;
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
        /// Helper method to load table data based on table name
        /// </summary>
        private async Task<List<Dictionary<string, object>>> LoadTableData(string tableName)
        {
            var result = new List<Dictionary<string, object>>();
            
            try
            {
                // Use SQLite-net-pcl's QueryAsync<object> for raw SQL
                string sql = $"SELECT * FROM {tableName}";
                
                // Use proper typed approach based on table name
                switch (tableName.ToLower())
                {
                    case "user":
                        var users = await _databaseService.Database.Table<NexusChat.Core.Models.User>().ToListAsync();
                        foreach (var user in users)
                        {
                            result.Add(ObjectToDictionary(user));
                        }
                        break;
                    case "conversation":
                        var conversations = await _databaseService.Database.Table<NexusChat.Core.Models.Conversation>().ToListAsync();
                        foreach (var conversation in conversations)
                        {
                            result.Add(ObjectToDictionary(conversation));
                        }
                        break;
                    case "message":
                        var messages = await _databaseService.Database.Table<NexusChat.Core.Models.Message>().ToListAsync();
                        foreach (var message in messages)
                        {
                            result.Add(ObjectToDictionary(message));
                        }
                        break;
                    case "aimodel":
                        var models = await _databaseService.Database.Table<NexusChat.Core.Models.AIModel>().ToListAsync();
                        foreach (var model in models)
                        {
                            result.Add(ObjectToDictionary(model));
                        }
                        break;
                    default:
                        // Try to execute a raw query as a fallback
                        try
                        {
                            // For generic tables, we can execute raw SQL and map manually
                            var query = await _databaseService.Database.QueryAsync<Dictionary<string, object>>(sql);
                            if (query != null)
                            {
                                result.AddRange(query);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Fallback generic query failed: {ex.Message}");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in LoadTableData: {ex.Message}");
            }
            
            return result;
        }
        
        /// <summary>
        /// Converts an object to a dictionary of property names and values
        /// </summary>
        private Dictionary<string, object> ObjectToDictionary(object obj)
        {
            var dict = new Dictionary<string, object>();
            
            if (obj == null) return dict;
            
            // Use reflection to get properties and values
            foreach (var prop in obj.GetType().GetProperties())
            {
                try
                {
                    var value = prop.GetValue(obj);
                    dict[prop.Name] = value;
                }
                catch
                {
                    // If we can't get a property value, store null
                    dict[prop.Name] = null;
                }
            }
            
            return dict;
        }
        
        /// <summary>
        /// Refreshes the data for the current table
        /// </summary>
        private async Task RefreshData()
        {
            await LoadTable(SelectedTable);
        }
        
        /// <summary>
        /// Navigates back to the previous page
        /// </summary>
        private async Task GoBack()
        {
            await Shell.Current.GoToAsync("..");
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
                // Drop tables
                await _databaseService.Initialize();
                await _databaseService.Database.DropTableAsync<NexusChat.Core.Models.Message>();
                await _databaseService.Database.DropTableAsync<NexusChat.Core.Models.Conversation>();
                await _databaseService.Database.DropTableAsync<NexusChat.Core.Models.User>();
                await _databaseService.Database.DropTableAsync<NexusChat.Core.Models.AIModel>();
                
                // Re-create tables
                await _databaseService.Database.CreateTablesAsync(SQLite.CreateFlags.None,
                    typeof(NexusChat.Core.Models.User),
                    typeof(NexusChat.Core.Models.Conversation),
                    typeof(NexusChat.Core.Models.Message),
                    typeof(NexusChat.Core.Models.AIModel));
                
                // Clear local data
                Records.Clear();
                HasData = false;
                RecordCount = "Records: 0";
                StatusMessage = "Database cleared successfully.";
                
                // Rebuild UI
                if (_dataGridRenderer != null)
                {
                    _dataGridRenderer.RebuildDataGrid();
                }
                
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
        /// Clean up any resources used by the ViewModel
        /// </summary>
        public void Cleanup()
        {
            // Clean up any resources or event handlers
            // Currently nothing to clean up
        }
        
        private async Task LoadTableAsync(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
                return;
                
            try
            {
                IsLoading = true;
                SelectedTable = tableName;
                
                // Clear existing records
                TableRecords.Clear();
                
                // Simulate loading records
                await Task.Delay(500);
                
                // For now, just add some dummy data based on the table name
                switch (tableName)
                {
                    case "Users":
                        // Add dummy user data
                        for (int i = 1; i <= 5; i++)
                        {
                            TableRecords.Add(new User
                            {
                                Id = i,
                                Username = $"user{i}",
                                DisplayName = $"User {i}",
                                Email = $"user{i}@example.com"
                            });
                        }
                        break;
                        
                    case "Conversations":
                        // Add dummy conversation data
                        for (int i = 1; i <= 3; i++)
                        {
                            TableRecords.Add(new Conversation
                            {
                                Id = i,
                                UserId = 1,
                                Title = $"Conversation {i}",
                                CreatedAt = DateTime.Now.AddDays(-i)
                            });
                        }
                        break;
                        
                    case "Messages":
                        // Add dummy message data
                        for (int i = 1; i <= 8; i++)
                        {
                            TableRecords.Add(new Message
                            {
                                Id = i,
                                ConversationId = (i % 3) + 1,
                                Content = $"This is message {i}",
                                IsAI = i % 2 == 0,
                                Timestamp = DateTime.Now.AddMinutes(-i * 5)
                            });
                        }
                        break;
                        
                    case "AIModels":
                        // Add dummy AI model data
                        TableRecords.Add(new AIModel
                        {
                            Id = 1,
                            ProviderName = "OpenAI",
                            ModelName = "GPT-4",
                            MaxTokens = 8192
                        });
                        TableRecords.Add(new AIModel
                        {
                            Id = 2,
                            ProviderName = "Anthropic",
                            ModelName = "Claude",
                            MaxTokens = 100000
                        });
                        break;
                }
                
                RecordCount = TableRecords.Count.ToString();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading table {tableName}: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        private async Task RefreshDataAsync()
        {
            await LoadTableAsync(SelectedTable);
        }
        
        // Make sure IsBusy changes notify IsNotBusy changes
        partial void OnIsBusyChanged(bool value)
        {
            OnPropertyChanged(nameof(IsNotBusy));
        }

        // Update property change handler to trigger search
        partial void OnSearchTextChanged(string value)
        {
            FilterRecords();
        }

        private void FilterRecords()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    // No filter, show all records
                    HasData = _allRecords.Count > 0;
                    
                    Records.Clear();
                    foreach (var record in _allRecords)
                    {
                        Records.Add(record);
                    }
                    RecordCount = $"Records: {Records.Count}";
                }
                else
                {
                    // Filter records that contain the search text in any field
                    var filteredRecords = _allRecords.Where(record => 
                        record.Values.Any(value => 
                            value?.ToString()?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true));
                    
                    Records.Clear();
                    foreach (var record in filteredRecords)
                    {
                        Records.Add(record);
                    }
                    
                    HasData = Records.Count > 0;
                    RecordCount = $"Records: {Records.Count} (filtered)";
                }
                
                // Rebuild data grid with filtered records
                _dataGridRenderer?.RebuildDataGrid();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error filtering records: {ex.Message}");
            }
        }
    }
}
