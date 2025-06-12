using System;
using System.Collections.Generic;
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
        private static readonly Dictionary<string, (object TrueValue, object FalseValue)> _cachedParameters = new();

        /// <summary>
        /// Color to use when the boolean value is true
        /// </summary>
        public Color TrueColor { get; set; } = Colors.Blue;

        /// <summary>
        /// Color to use when the boolean value is false
        /// </summary>
        public Color FalseColor { get; set; } = Colors.Gray;

        /// <summary>
        /// Converts a boolean to a color or other value based on parameter
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = value is bool b && b;
            
            // If target type is Color and no parameter is provided, use the properties
            if (targetType == typeof(Color) && parameter == null)
            {
                return boolValue ? TrueColor : FalseColor;
            }
            
            // Handle parameters with caching
            if (parameter is string paramString && !string.IsNullOrEmpty(paramString))
            {
                if (!_cachedParameters.TryGetValue(paramString, out var cachedValues))
                {
                    cachedValues = ParseParameterString(paramString, targetType);
                    _cachedParameters[paramString] = cachedValues;
                }
                
                return boolValue ? cachedValues.TrueValue : cachedValues.FalseValue;
            }
            
            // Default values for various types
            return targetType.Name switch
            {
                nameof(Color) => boolValue ? Colors.Blue : Colors.Gray,
                nameof(Int32) => boolValue ? 1 : 0,
                nameof(Double) => boolValue ? 1.0 : 0.0,
                _ => boolValue
            };
        }

        private static (object TrueValue, object FalseValue) ParseParameterString(string paramString, Type targetType)
        {
            string[] parts = paramString.Split(',');
            if (parts.Length != 2)
            {
                return (Colors.Blue, Colors.Gray);
            }

            try
            {
                return targetType.Name switch
                {
                    nameof(Color) => (Color.Parse(parts[0].Trim()), Color.Parse(parts[1].Trim())),
                    nameof(Int32) => (int.Parse(parts[0].Trim()), int.Parse(parts[1].Trim())),
                    nameof(Double) => (double.Parse(parts[0].Trim(), CultureInfo.InvariantCulture), 
                                     double.Parse(parts[1].Trim(), CultureInfo.InvariantCulture)),
                    _ => (parts[0].Trim(), parts[1].Trim())
                };
            }
            catch
            {
                return (Colors.Blue, Colors.Gray);
            }
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
