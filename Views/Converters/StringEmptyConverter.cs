using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace NexusChat.Views.Converters
{
    /// <summary>
    /// Converts null or empty strings to a fallback value
    /// </summary>
    public class StringEmptyConverter : IValueConverter
    {
        /// <summary>
        /// Gets or sets the fallback value to use when the input string is null or empty
        /// </summary>
        public string FallbackValue { get; set; } = "Not Available";

        /// <summary>
        /// Converts an input string to the fallback value if it's null or empty
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                return string.IsNullOrWhiteSpace(stringValue) ? FallbackValue : stringValue;
            }
            
            return FallbackValue;
        }

        /// <summary>
        /// Not implemented for this converter
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
