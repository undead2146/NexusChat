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
            if (value is int intValue && parameter is string paramStr && int.TryParse(paramStr, out int threshold))
            {
                return intValue > threshold;
            }
            
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
