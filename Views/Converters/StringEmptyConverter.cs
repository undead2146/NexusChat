using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace NexusChat.Views.Converters
{
    public class StringEmptyConverter : IValueConverter
    {
        public string FallbackValue { get; set; } = "New Chat";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                return string.IsNullOrEmpty(stringValue) ? FallbackValue : stringValue;
            }
            
            return FallbackValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
