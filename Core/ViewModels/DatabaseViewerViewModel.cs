using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexusChat.Data;
using NexusChat.Models;
using SQLite;
using System.Diagnostics;

namespace NexusChat.ViewModels
{
    /// <summary>
    /// ViewModel for the database viewer page
    /// </summary>
    public partial class DatabaseViewerViewModel : ObservableObject
    {
        private readonly DatabaseService _databaseService;
        private CancellationTokenSource _cancellationTokenSource;
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool _isBusy;

        public bool IsNotBusy => !IsBusy;
        
        [ObservableProperty]
        private string _selectedTable = "Users";
        
        [ObservableProperty]
        private ObservableCollection<string> _tables = new() { "Users", "Conversations", "Messages" };
        
        [ObservableProperty]
        private ObservableCollection<Dictionary<string, object>> _records = new();
        
        [ObservableProperty]
        private ObservableCollection<string> _columnNames = new();
        
        [ObservableProperty]
        private string _recordCount = "0 records";
        
        [ObservableProperty]
        private string _statusMessage = "Select a table to view data";
        
        [ObservableProperty]
        private bool _hasData;
        
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
        /// Command to clear all data from the database
        /// </summary>
        public IAsyncRelayCommand ClearDatabaseCommand { get; }
        
        /// <summary>
        /// Initializes a new instance of the DatabaseViewerViewModel class
        /// </summary>
        /// <param name="databaseService">Database service for data access</param>
        public DatabaseViewerViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _cancellationTokenSource = new CancellationTokenSource();
            
            // Initialize commands
            RefreshDataCommand = new AsyncRelayCommand(RefreshData);
            GoBackCommand = new AsyncRelayCommand(GoBack);
            TableChangedCommand = new AsyncRelayCommand<string>(async (t) => { 
                if (t != null) {
                    SelectedTable = t;
                    await RefreshData(); 
                }
            });
            ClearDatabaseCommand = new AsyncRelayCommand(ClearDatabase);
        }
        
        /// <summary>
        /// Initialize data when view appears
        /// </summary>
        public async Task InitializeData()
        {
            await RefreshData();
        }
        
        /// <summary>
        /// Cancel any running operations
        /// </summary>
        public void CancelOperations()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Ensures that the database is properly initialized
        /// </summary>
        private async Task EnsureDatabaseInitialized()
        {
            try
            {
                await _databaseService.Initialize();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to initialize database: {ex.Message}";
                throw; // Re-throw to propagate the error
            }
        }

        /// <summary>
        /// Refreshes the current table data
        /// </summary>
        private async Task RefreshData()
        {
            if (IsBusy) return;
            
            try
            {
                IsBusy = true;
                StatusMessage = $"Loading {SelectedTable}...";
                
                // Ensure database is initialized before proceeding
                await EnsureDatabaseInitialized();
                
                // Use a cancellation token for the operation
                var cancellationToken = _cancellationTokenSource.Token;
                
                // Clear previous data
                await MainThread.InvokeOnMainThreadAsync(() => {
                    Records.Clear();
                    ColumnNames.Clear();
                });
                
                if (string.IsNullOrEmpty(SelectedTable))
                {
                    RecordCount = "No table selected";
                    HasData = false;
                    return;
                }
                
                // Limit data loading to avoid memory issues
                const int maxRecords = 100;
                
                // Load appropriate data for selected table
                switch (SelectedTable)
                {
                    case "Users":
                        await LoadUsers(maxRecords, cancellationToken);
                        break;
                    case "Conversations":
                        await LoadConversations(maxRecords, cancellationToken);
                        break;
                    case "Messages":
                        await LoadMessages(maxRecords, cancellationToken);
                        break;
                    default:
                        StatusMessage = $"Unknown table: {SelectedTable}";
                        RecordCount = "0 records";
                        HasData = false;
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Operation cancelled";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                RecordCount = "Error";
                HasData = false;
                Debug.WriteLine($"Error refreshing data: {ex}");
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        /// <summary>
        /// Loads Users table data
        /// </summary>
        private async Task LoadUsers(int maxRecords, CancellationToken cancellationToken)
        {
            // Query with limit to avoid memory issues
            var users = await _databaseService.Database.Table<User>().Take(maxRecords).ToListAsync();
            
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException("Users query was cancelled");
            
            int totalCount = await _databaseService.Database.Table<User>().CountAsync();
            RecordCount = $"{users.Count} user(s)" + (totalCount > maxRecords ? $" (showing {maxRecords} of {totalCount})" : "");
            HasData = users.Count > 0;
            
            if (!HasData)
            {
                StatusMessage = "No users found. Try creating sample data.";
                return;
            }
            
            StatusMessage = "Users loaded successfully";
            
            await ExtractSchema<User>(users, cancellationToken);
        }
        
        /// <summary>
        /// Loads Conversations table data
        /// </summary>
        private async Task LoadConversations(int maxRecords, CancellationToken cancellationToken)
        {
            var conversations = await _databaseService.Database.Table<Conversation>().Take(maxRecords).ToListAsync();
            
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException("Conversations query was cancelled");
            
            int totalCount = await _databaseService.Database.Table<Conversation>().CountAsync();
            RecordCount = $"{conversations.Count} conversation(s)" + (totalCount > maxRecords ? $" (showing {maxRecords} of {totalCount})" : "");
            HasData = conversations.Count > 0;
            
            if (!HasData)
            {
                StatusMessage = "No conversations found. Try creating sample data.";
                return;
            }
            
            StatusMessage = "Conversations loaded successfully";
            
            await ExtractSchema<Conversation>(conversations, cancellationToken);
        }
        
        /// <summary>
        /// Loads Messages table data
        /// </summary>
        private async Task LoadMessages(int maxRecords, CancellationToken cancellationToken)
        {
            var messages = await _databaseService.Database.Table<Message>().Take(maxRecords).ToListAsync();
            
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException("Messages query was cancelled");
            
            int totalCount = await _databaseService.Database.Table<Message>().CountAsync();
            RecordCount = $"{messages.Count} message(s)" + (totalCount > maxRecords ? $" (showing {maxRecords} of {totalCount})" : "");
            HasData = messages.Count > 0;
            
            if (!HasData)
            {
                StatusMessage = "No messages found. Try creating sample data.";
                return;
            }
            
            StatusMessage = "Messages loaded successfully";
            
            await ExtractSchema<Message>(messages, cancellationToken);
        }
        
        /// <summary>
        /// Generic method to extract schema from entity records and populate view collections
        /// </summary>
        private async Task ExtractSchema<T>(IList<T> entities, CancellationToken cancellationToken) where T : new()
        {
            if (entities.Count == 0) return;
            
            try
            {
                // Prepare collections to avoid multiple UI updates
                var columns = new List<string>();
                var records = new List<Dictionary<string, object>>();
                
                await Task.Run(() => {
                    // Get column names from properties
                    var propertyNames = typeof(T).GetProperties()
                        .Where(p => p.PropertyType.IsPrimitive || 
                                  p.PropertyType == typeof(string) || 
                                  p.PropertyType == typeof(DateTime) ||
                                  p.PropertyType == typeof(DateTime?))
                        .Select(p => p.Name)
                        .ToList();
                    
                    columns.AddRange(propertyNames);
                    
                    // Check for cancellation
                    if (cancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException();
                    
                    // Convert entities to dictionaries for displaying
                    foreach (var entity in entities)
                    {
                        var record = new Dictionary<string, object>();
                        foreach (var name in columns)
                        {
                            // Check for cancellation periodically
                            if (cancellationToken.IsCancellationRequested)
                                throw new OperationCanceledException();
                                
                            var property = typeof(T).GetProperty(name);
                            if (property != null)
                            {
                                try
                                {
                                    var value = property.GetValue(entity);
                                    
                                    // Truncate long strings
                                    if (value is string str && str.Length > 50)
                                    {
                                        value = str.Substring(0, 47) + "...";
                                    }
                                    
                                    record[name] = value ?? "null";
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"Error getting property {name}: {ex.Message}");
                                    record[name] = "[error]";
                                }
                            }
                        }
                        records.Add(record);
                    }
                }, cancellationToken);
                
                // Update UI collections once with all data
                if (!cancellationToken.IsCancellationRequested)
                {
                    await MainThread.InvokeOnMainThreadAsync(() => 
                    {
                        ColumnNames.Clear();
                        foreach (var col in columns)
                        {
                            ColumnNames.Add(col);
                        }
                        
                        Records.Clear();
                        foreach (var rec in records)
                        {
                            Records.Add(rec);
                        }
                    });
                }
            }
            catch (OperationCanceledException)
            {
                throw; // Re-throw for handling in the caller
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error extracting schema: {ex}");
                StatusMessage = $"Error extracting data schema: {ex.Message}";
            }
        }
        
        /// <summary>
        /// Clears all data from the database
        /// </summary>
        private async Task ClearDatabase()
        {
            if (IsBusy) return;
            
            // Ask for confirmation
            bool confirmed = await Application.Current.MainPage.DisplayAlert(
                "Clear Database?", 
                "This will delete ALL data from the database. This action cannot be undone. Continue?",
                "Yes, Clear All Data", 
                "Cancel");
                
            if (!confirmed) return;
            
            try
            {
                IsBusy = true;
                StatusMessage = "Clearing database...";
                
                // Ensure database is initialized before proceeding
                await EnsureDatabaseInitialized();
                
                // Clear all data
                await _databaseService.ClearAllData();
                
                StatusMessage = "Database cleared successfully";
                
                // Make sure UI gets updated by forcing a refresh
                _hasData = false;
                RecordCount = "0 records";
                
                // Clear our local collections immediately to show empty state
                await MainThread.InvokeOnMainThreadAsync(() => {
                    Records.Clear();
                    ColumnNames.Clear();
                });
                
                // Refresh the data to show empty state properly
                await RefreshData();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error clearing database: {ex.Message}";
                Debug.WriteLine($"Error clearing database: {ex}");
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        /// <summary>
        /// Navigates back to the previous page
        /// </summary>
        private async Task GoBack()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
