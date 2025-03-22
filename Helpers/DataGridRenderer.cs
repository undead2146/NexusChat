using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.Maui.Controls;
using System.Threading.Tasks;

namespace NexusChat.Helpers
{
    /// <summary>
    /// Helper class to render a dynamic data grid from a collection of records
    /// </summary>
    public class DataGridRenderer : IDisposable
    {
        private readonly Grid _grid;
        private readonly ObservableCollection<Dictionary<string, object>> _records;
        private readonly ObservableCollection<string> _columnNames;
        private bool _isUpdating = false;
        private const int UPDATE_THROTTLE_MS = 300;
        
        /// <summary>
        /// Creates a new DataGridRenderer instance
        /// </summary>
        public DataGridRenderer(Grid grid, 
            ObservableCollection<Dictionary<string, object>> records,
            ObservableCollection<string> columnNames)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _records = records ?? throw new ArgumentNullException(nameof(records));
            _columnNames = columnNames ?? throw new ArgumentNullException(nameof(columnNames));
            
            // Subscribe to collection changes
            _records.CollectionChanged += OnRecordsCollectionChanged;
            _columnNames.CollectionChanged += OnColumnsCollectionChanged;
        }
        
        private void OnColumnsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!_isUpdating)
                MainThread.BeginInvokeOnMainThread(ThrottledRebuild);
        }
        
        private void OnRecordsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!_isUpdating)
                MainThread.BeginInvokeOnMainThread(ThrottledRebuild);
        }
        
        /// <summary>
        /// Throttles rebuilds to prevent excessive UI updates
        /// </summary>
        private async void ThrottledRebuild()
        {
            if (_isUpdating)
                return;
                
            _isUpdating = true;
            await Task.Delay(UPDATE_THROTTLE_MS);
            
            try
            {
                RebuildDataGrid();
            }
            finally
            {
                _isUpdating = false;
            }
        }
        
        /// <summary>
        /// Rebuilds the data grid based on current data
        /// </summary>
        public void RebuildDataGrid()
        {
            if (!MainThread.IsMainThread)
            {
                MainThread.BeginInvokeOnMainThread(RebuildDataGrid);
                return;
            }

            try
            {
                _grid.Children.Clear();
                _grid.RowDefinitions.Clear();
                _grid.ColumnDefinitions.Clear();
                
                var columnNames = new List<string>(_columnNames);
                if (columnNames.Count == 0 || _records.Count == 0)
                    return;
                
                // Set up columns
                for (int i = 0; i < columnNames.Count; i++)
                {
                    _grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                }
                
                // Limit number of rows to show for performance
                const int MAX_VISIBLE_ROWS = 100;
                int rowCount = Math.Min(_records.Count, MAX_VISIBLE_ROWS);
                
                // Set up rows (header + data)
                _grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                for (int i = 0; i < rowCount; i++)
                {
                    _grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                }
                
                // Add headers
                for (int col = 0; col < columnNames.Count; col++)
                {
                    var headerLabel = new Label
                    {
                        Text = columnNames[col],
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Colors.White,
                        BackgroundColor = Color.FromArgb("#6c757d"),
                        Padding = new Thickness(8, 6),
                        LineBreakMode = LineBreakMode.TailTruncation
                    };
                    
                    _grid.Add(headerLabel, col, 0);
                }
                
                // Add data rows
                for (int row = 0; row < rowCount; row++)
                {
                    var record = _records[row];
                    var rowColor = row % 2 == 0 ? Colors.White : Color.FromArgb("#f8f9fa");
                    
                    for (int col = 0; col < columnNames.Count; col++)
                    {
                        string key = columnNames[col];
                        string cellValue = record.ContainsKey(key) ? record[key]?.ToString() ?? "null" : "";
                        
                        // Truncate long cell values
                        if (cellValue.Length > 100)
                        {
                            cellValue = cellValue.Substring(0, 97) + "...";
                        }
                        
                        var cellLabel = new Label
                        {
                            Text = cellValue,
                            TextColor = Colors.Black,
                            BackgroundColor = rowColor,
                            Padding = new Thickness(8, 6),
                            LineBreakMode = LineBreakMode.TailTruncation
                        };
                        
                        _grid.Add(cellLabel, col, row + 1); // +1 for header row
                    }
                }
                
                // Add "more rows" indicator if needed
                if (_records.Count > MAX_VISIBLE_ROWS)
                {
                    var moreLabel = new Label
                    {
                        Text = $"+ {_records.Count - MAX_VISIBLE_ROWS} more rows (not shown for performance)",
                        TextColor = Color.FromArgb("#6c757d"),
                        HorizontalOptions = LayoutOptions.Center,
                        Margin = new Thickness(0, 10, 0, 0),
                        FontSize = 12
                    };
                    
                    _grid.Add(moreLabel, 0, rowCount + 1, columnNames.Count, 1); // Span all columns
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error rebuilding grid: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Disposes the DataGridRenderer and unsubscribes from events
        /// </summary>
        public void Dispose()
        {
            if (_records != null)
                _records.CollectionChanged -= OnRecordsCollectionChanged;
                
            if (_columnNames != null)
                _columnNames.CollectionChanged -= OnColumnsCollectionChanged;
        }
        
        /// <summary>
        /// Cleans up event subscriptions (alias for Dispose)
        /// </summary>
        public void CleanUp()
        {
            Dispose();
        }
    }
}
