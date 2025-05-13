using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace NexusChat.Views.Converters
{
    /// <summary>
    /// Converter that translates a boolean value to a visibility value
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
