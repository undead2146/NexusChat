using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace NexusChat.Views.Converters
{
    /// <summary>
    /// Converter that inverts a boolean value
    /// </summary>
    public class InvertedBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            
            return false;
        }
    }
}
