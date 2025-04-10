using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace NexusChat.Views.Converters
{
    /// <summary>
    /// Converts a bool value to a color
    /// </summary>
    public class BoolToColorConverter : IValueConverter
    {
        /// <summary>
        /// Gets or sets the color to use when the input is true
        /// </summary>
        public Color TrueValue { get; set; }

        /// <summary>
        /// Gets or sets the color to use when the input is false
        /// </summary>
        public Color FalseValue { get; set; }

        /// <summary>
        /// Converts bool to Color
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? TrueValue : FalseValue;
            }

            // Default color if conversion fails
            return Colors.Gray;
        }

        /// <summary>
        /// Converts back from Color to bool (not implemented)
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
