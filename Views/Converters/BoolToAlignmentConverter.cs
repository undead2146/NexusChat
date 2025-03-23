using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace NexusChat.Views.Converters
{
    /// <summary>
    /// Converts a boolean value to a LayoutOptions alignment value
    /// </summary>
    public class BoolToAlignmentConverter : IValueConverter
    {
        /// <summary>
        /// Gets or sets the LayoutOptions to use when the input is true
        /// </summary>
        public LayoutOptions TrueValue { get; set; } = LayoutOptions.Start;
        
        /// <summary>
        /// Gets or sets the LayoutOptions to use when the input is false
        /// </summary>
        public LayoutOptions FalseValue { get; set; } = LayoutOptions.End;

        /// <summary>
        /// Converts a boolean to a LayoutOptions value
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? TrueValue : FalseValue;
            }
            
            return LayoutOptions.Center;
        }

        /// <summary>
        /// Converts back from a LayoutOptions to a boolean (not implemented)
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
