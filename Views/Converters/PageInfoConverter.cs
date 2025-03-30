using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace NexusChat.Views.Converters
{
    /// <summary>
    /// Multi-value converter to format current page and total pages into a display string
    /// </summary>
    public class PageInfoConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Ensure we have the two values we need (current page and total pages)
            if (values == null || values.Length < 2)
                return "Page ? of ?";
            
            // Extract values with proper null handling
            string currentPage = values[0]?.ToString() ?? "?";
            string totalPages = values[1]?.ToString() ?? "?";
            
            // Format the page info text
            return $"Page {currentPage} of {totalPages}";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
