using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace NexusChat.Views.Converters
{
    /// <summary>
    /// Converter to transform a boolean value into a color or other type
    /// </summary>
    public class BoolToColorConverter : IValueConverter
    {
        /// <summary>
        /// Converts a boolean to a color or other value based on parameter
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = false;
            
            if (value is bool b)
            {
                boolValue = b;
            }
            
            // Handle parameters
            if (parameter is string paramString)
            {
                // Check if we have a comma-separated parameter
                string[] parts = paramString.Split(',');
                if (parts.Length == 2)
                {
                    // Handle different target types
                    if (targetType == typeof(Color))
                    {
                        // Parse colors
                        string colorStr = boolValue ? parts[0] : parts[1];
                        return Color.Parse(colorStr);
                    }
                    else if (targetType == typeof(int))
                    {
                        // Parse integers
                        return boolValue 
                            ? int.Parse(parts[0]) 
                            : int.Parse(parts[1]);
                    }
                    else if (targetType == typeof(double))
                    {
                        // Parse doubles
                        return boolValue 
                            ? double.Parse(parts[0], CultureInfo.InvariantCulture) 
                            : double.Parse(parts[1], CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        // Return the string directly
                        return boolValue ? parts[0] : parts[1];
                    }
                }
            }
            
            // Default values for various types
            if (targetType == typeof(Color))
            {
                return boolValue ? Colors.Blue : Colors.Gray;
            }
            else if (targetType == typeof(int))
            {
                return boolValue ? 1 : 0;
            }
            else if (targetType == typeof(double))
            {
                return boolValue ? 1.0 : 0.0;
            }
            
            return boolValue;
        }

        /// <summary>
        /// Converts back from a value to a boolean
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
