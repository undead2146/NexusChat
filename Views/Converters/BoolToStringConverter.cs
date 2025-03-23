using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace NexusChat.Converters
{
    public class BoolToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // Parse parameter if provided (format: "TrueString|FalseString")
                string[] strings = parameter?.ToString().Split('|');
                
                if (strings != null && strings.Length >= 2)
                {
                    return boolValue ? strings[0] : strings[1];
                }
                
                // Default values if parameter parsing fails
                return boolValue ? "Yes" : "No";
            }
            
            return "N/A"; // Default for non-boolean values
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
