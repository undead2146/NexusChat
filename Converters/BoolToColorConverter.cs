using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace NexusChat.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // Parse parameter if provided (format: "TrueColor|FalseColor")
                string[] colorNames = parameter?.ToString().Split('|');
                
                if (colorNames != null && colorNames.Length >= 2)
                {
                    // Look up colors by name
                    string colorName = boolValue ? colorNames[0] : colorNames[1];
                    if (Application.Current.Resources.TryGetValue(colorName, out var color))
                        return color;
                }
                
                // Default colors if parameter parsing fails
                return boolValue ? Colors.Green : Colors.Red;
            }
            
            return Colors.Gray; // Default for non-boolean values
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
