using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using NexusChat.Core.Models;

namespace NexusChat.Views.Converters
{
    /// <summary>
    /// Converts a value to a boolean indicating whether it's null/empty/zero
    /// </summary>
    public class NotNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return true;
                
            if (value is int intValue)
                return intValue == 0;
                
            if (value is string strValue)
                return string.IsNullOrEmpty(strValue);
                
            if (value is bool boolValue)
                return !boolValue;
                
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
