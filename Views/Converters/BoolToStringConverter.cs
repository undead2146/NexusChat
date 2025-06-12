using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace NexusChat.Views.Converters
{
    /// <summary>
    /// Converts boolean values to string based on parameter format "TrueValue|FalseValue"
    /// </summary>
    public class BoolToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not bool boolValue)
                return "Select";

            string param = parameter?.ToString() ?? "True|False";
            string[] parts = param.Split('|');
            
            if (parts.Length != 2)
                return boolValue ? "True" : "False";
            
            return boolValue ? parts[0] : parts[1];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
