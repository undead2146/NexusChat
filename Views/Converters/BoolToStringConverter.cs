using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace NexusChat.Views.Converters
{
    /// <summary>
    /// Converts a boolean value to a string
    /// </summary>
    public class BoolToStringConverter : IValueConverter
    {
        /// <summary>
        /// Gets or sets the string to use when the input is true
        /// </summary>
        public string TrueValue { get; set; } = "True";
        
        /// <summary>
        /// Gets or sets the string to use when the input is false
        /// </summary>
        public string FalseValue { get; set; } = "False";

        /// <summary>
        /// Converts a boolean to a string value
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? TrueValue : FalseValue;
            }
            
            return string.Empty; // Default string for non-boolean values
        }

        /// <summary>
        /// Converts back from a string to a boolean
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                if (stringValue.Equals(TrueValue, StringComparison.OrdinalIgnoreCase))
                    return true;
                if (stringValue.Equals(FalseValue, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            
            return false;
        }
    }
}
