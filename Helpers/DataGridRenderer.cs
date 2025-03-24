using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Maui.Graphics;

namespace NexusChat.Helpers
{
    /// <summary>
    /// Helper class for rendering database data in a Grid with proper theme support
    /// </summary>
    public class DataGridRenderer
    {
        private readonly Grid _grid;
        private readonly IEnumerable<Dictionary<string, object>> _data;
        private readonly IEnumerable<string> _columns;
        private const int MaxDisplayLength = 150;
        private const int MinColumnWidth = 120;

        /// <summary>
        /// Creates a new DataGridRenderer
        /// </summary>
        public DataGridRenderer(Grid grid, IEnumerable<Dictionary<string, object>> data, IEnumerable<string> columns)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _data = data ?? new List<Dictionary<string, object>>();
            _columns = columns ?? new List<string>();
        }

        /// <summary>
        /// Rebuilds the data grid with current data
        /// </summary>
        public void RebuildDataGrid()
        {
            try
            {
                // Clear existing grid
                _grid.Children.Clear();
                _grid.ColumnDefinitions.Clear();
                _grid.RowDefinitions.Clear();

                // If no data, nothing to render
                if (!_columns.Any())
                {
                    return;
                }

                // Create column definitions
                int columnIndex = 0;
                foreach (var column in _columns)
                {
                    // Fix: Remove MinimumWidth property and use Width with a fixed value
                    _grid.ColumnDefinitions.Add(new ColumnDefinition
                    {
                        Width = new GridLength(MinColumnWidth, GridUnitType.Absolute)
                    });

                    // Add header cell
                    var headerFrame = CreateCell(column, true, columnIndex, 0);
                    Grid.SetColumn(headerFrame, columnIndex);
                    Grid.SetRow(headerFrame, 0);
                    _grid.Children.Add(headerFrame);

                    columnIndex++;
                }

                // Add header row definition
                _grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                // Create data rows
                int rowIndex = 1;
                foreach (var row in _data)
                {
                    _grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    // Create cells for each column
                    columnIndex = 0;
                    foreach (var column in _columns)
                    {
                        object cellValue = row.TryGetValue(column, out var value) ? value : null;
                        string cellText = FormatCellValue(cellValue);

                        var cellFrame = CreateCell(cellText, false, columnIndex, rowIndex);
                        Grid.SetColumn(cellFrame, columnIndex);
                        Grid.SetRow(cellFrame, rowIndex);
                        _grid.Children.Add(cellFrame);

                        columnIndex++;
                    }
                    rowIndex++;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in RebuildDataGrid: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a cell for the data grid
        /// </summary>
        private Frame CreateCell(string text, bool isHeader, int column, int row)
        {
            // Fix: Use ResourceDictionary properly
            var resourceDict = Application.Current.Resources;
            
            var headerColor = Application.Current.RequestedTheme == AppTheme.Dark ? 
                resourceDict["PrimaryDark"] : resourceDict["Primary"];
                
            var evenRowColor = Application.Current.RequestedTheme == AppTheme.Dark ? 
                resourceDict["CardBackgroundDark"] : resourceDict["CardBackground"];
                
            var oddRowColor = Application.Current.RequestedTheme == AppTheme.Dark ? 
                resourceDict["BackgroundDark"] : resourceDict["Background"];
            
            var headerBorderColor = Application.Current.RequestedTheme == AppTheme.Dark ? 
                resourceDict["Gray600"] : resourceDict["Gray400"];
                
            var dataBorderColor = Application.Current.RequestedTheme == AppTheme.Dark ? 
                resourceDict["Gray600"] : (row % 2 == 0 ? resourceDict["Gray300"] : resourceDict["Gray200"]);
            
            var frame = new Frame
            {
                Padding = new Thickness(10, 5),
                BorderColor = isHeader ? (Color)headerBorderColor : (Color)dataBorderColor,
                CornerRadius = 0,
                HasShadow = false,
                BackgroundColor = isHeader ? 
                    (Color)headerColor : 
                    (row % 2 == 0 ? (Color)evenRowColor : (Color)oddRowColor),
                Margin = 0
            };

            var stackLayout = new VerticalStackLayout
            {
                Spacing = 0,
                HorizontalOptions = LayoutOptions.Fill
            };

            var textColor = isHeader ? 
                Colors.White : 
                (Application.Current.RequestedTheme == AppTheme.Dark ? 
                    (Color)resourceDict["PrimaryTextColorDark"] : 
                    (Color)resourceDict["PrimaryTextColor"]);

            var label = new Label
            {
                Text = text,
                FontAttributes = isHeader ? FontAttributes.Bold : FontAttributes.None,
                TextColor = textColor,
                LineBreakMode = LineBreakMode.TailTruncation, // Fixed to TailTruncation instead of TailAndTruncation
                MaxLines = 3
            };

            stackLayout.Children.Add(label);

            // If content is longer than threshold, add an expand button
            if (text != null && text.Length > MaxDisplayLength)
            {
                var buttonColor = (Color)resourceDict["Secondary"];
                var buttonTextColor = Application.Current.RequestedTheme == AppTheme.Dark ? 
                    Colors.White : Colors.Black;
                    
                var expandButton = new Button
                {
                    Text = "View Full",
                    BackgroundColor = buttonColor,
                    TextColor = buttonTextColor,
                    FontSize = 10,
                    Padding = new Thickness(5),
                    HeightRequest = 25,
                    HorizontalOptions = LayoutOptions.Start,
                    Margin = new Thickness(0, 5, 0, 0)
                };

                expandButton.Clicked += (s, e) => ShowFullTextPopup(text, column.ToString());
                stackLayout.Children.Add(expandButton);
            }

            frame.Content = stackLayout;
            return frame;
        }

        /// <summary>
        /// Shows a popup with the full text content
        /// </summary>
        private async void ShowFullTextPopup(string text, string columnName)
        {
            // Create a popup to display the full text
            await Application.Current.MainPage.DisplayAlert(
                $"Full Content - Column {columnName}",
                text,
                "Close");
        }

        /// <summary>
        /// Formats cell value for display
        /// </summary>
        private string FormatCellValue(object value)
        {
            if (value == null)
                return "[NULL]";

            if (value is DateTime dateTime)
                return dateTime.ToString("yyyy-MM-dd HH:mm:ss");

            if (value is bool boolean)
                return boolean ? "True" : "False";

            return value.ToString();
        }
    }
}
