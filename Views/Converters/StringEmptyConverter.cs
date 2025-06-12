using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace NexusChat.Views.Converters
{
    public class StringEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string stringValue = value?.ToString() ?? "";
            bool isEmpty = string.IsNullOrWhiteSpace(stringValue);
            
            string param = parameter?.ToString()?.ToLowerInvariant();
            bool invert = param == "invert";
            
            return invert ? !isEmpty : isEmpty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
