using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace NexusChat.Views.Converters
{
    /// <summary>
    /// Converter to check if a value is greater than a parameter
    /// </summary>
    public class GreaterThanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            // Try to parse both values as numbers
            if (int.TryParse(value.ToString(), out int intValue) && 
                int.TryParse(parameter.ToString(), out int intParam))
            {
                return intValue > intParam;
            }

            // For non-integer types, fall back to string comparison
            return value.ToString().CompareTo(parameter.ToString()) > 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
