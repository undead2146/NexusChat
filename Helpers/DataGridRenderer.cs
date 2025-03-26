using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Graphics;

namespace NexusChat.Helpers
{
    /// <summary>
    /// Helper class for rendering data in a grid format
    /// </summary>
    public class DataGridRenderer
    {
        private readonly Grid _grid;
        private readonly ObservableCollection<Dictionary<string, object>> _data;
        private readonly ObservableCollection<string> _columns;
        private const int MaxVisibleRows = 100; // Limit visible rows to improve performance

        public DataGridRenderer(Grid grid, ObservableCollection<Dictionary<string, object>> data, ObservableCollection<string> columns)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _columns = columns ?? throw new ArgumentNullException(nameof(columns));
        }

        /// <summary>
        /// Rebuilds the grid with improved performance
        /// </summary>
        public void RebuildDataGrid()
        {
            try
            {
                if (_grid == null) return;

                // Clear existing content
                _grid.Children.Clear();
                _grid.RowDefinitions.Clear();
                _grid.ColumnDefinitions.Clear();

                // If no columns or data, just return
                if (_columns.Count == 0 || _data.Count == 0) return;

                // Create the header row and column definitions
                _grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                
                // Add column definitions
                int columnIndex = 0;
                foreach (var column in _columns)
                {
                    // Make ID columns narrower, content columns wider
                    GridLength colWidth = column.EndsWith("Id") || column == "Id" 
                        ? new GridLength(80) 
                        : (column == "Content" || column == "RawResponse" || column == "Description" || column == "Summary")
                            ? new GridLength(300)
                            : new GridLength(150);
                            
                    _grid.ColumnDefinitions.Add(new ColumnDefinition { Width = colWidth });

                    // Add header cell
                    var headerFrame = new Frame
                    {
                        BackgroundColor = Color.FromArgb("#007BFF"),
                        Padding = new Thickness(5),
                        Margin = new Thickness(0),
                        HasShadow = false,
                        BorderColor = Color.FromArgb("#FFFFFF")
                    };

                    var headerLabel = new Label
                    {
                        Text = column,
                        TextColor = Colors.White,
                        FontAttributes = FontAttributes.Bold,
                        FontSize = 14,
                        LineBreakMode = LineBreakMode.TailTruncation
                    };

                    headerFrame.Content = headerLabel;
                    _grid.Add(headerFrame, columnIndex, 0);

                    columnIndex++;
                }

                // Limit the number of rows to display for better performance
                int rowsToDisplay = Math.Min(_data.Count, MaxVisibleRows);
                
                // Add data rows - with performance optimizations
                for (int rowIndex = 0; rowIndex < rowsToDisplay; rowIndex++)
                {
                    // Add row definition
                    _grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    
                    // Get the data for this row
                    var rowData = _data[rowIndex];
                    
                    // Add cells
                    for (int colIndex = 0; colIndex < _columns.Count; colIndex++)
                    {
                        var column = _columns[colIndex];

                        // Create the cell frame
                        var cellFrame = new Frame
                        {
                            BackgroundColor = rowIndex % 2 == 0 
                                ? Color.FromArgb("#FFFFFF") 
                                : Color.FromArgb("#F8F9FA"),
                            Padding = new Thickness(5),
                            Margin = new Thickness(0),
                            HasShadow = false,
                            BorderColor = Color.FromArgb("#DEE2E6")
                        };

                        // Create cell content
                        object cellValue = rowData.ContainsKey(column) ? rowData[column] : null;
                        string cellText = cellValue?.ToString() ?? "";
                        
                        // Truncate very long text for performance
                        if (cellText.Length > 500)
                        {
                            cellText = cellText.Substring(0, 500) + "...";
                        }

                        var cellLabel = new Label
                        {
                            Text = cellText,
                            TextColor = Color.FromArgb("#212529"),
                            FontSize = 13,
                            LineBreakMode = LineBreakMode.WordWrap,
                            MaxLines = 2, // Limit lines for better performance
                        };

                        cellFrame.Content = cellLabel;
                        _grid.Add(cellFrame, colIndex, rowIndex + 1); // +1 to account for header row
                    }
                }
                
                // If we limited the number of rows, add a note at the bottom
                if (rowsToDisplay < _data.Count)
                {
                    // Add a row for the note
                    _grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    
                    // Create a frame for the note
                    var noteFrame = new Frame
                    {
                        BackgroundColor = Color.FromArgb("#FFF3CD"),
                        Padding = new Thickness(10),
                        Margin = new Thickness(0),
                        HasShadow = false,
                        BorderColor = Color.FromArgb("#FFEEBA")
                    };
                    
                    // Create note label
                    var noteLabel = new Label
                    {
                        Text = $"Showing {rowsToDisplay} of {_data.Count} records. Use paging controls to see more.",
                        TextColor = Color.FromArgb("#856404"),
                        FontSize = 14,
                        HorizontalOptions = LayoutOptions.Center
                    };
                    
                    noteFrame.Content = noteLabel;
                    
                    // Add the note - span across all columns
                    Grid.SetColumnSpan(noteFrame, _columns.Count);
                    _grid.Add(noteFrame, 0, rowsToDisplay + 1); // +1 for header row
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error rebuilding data grid: {ex.Message}");
            }
        }
    }
}
