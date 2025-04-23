using System;
using System.Globalization;
using System.Diagnostics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace NexusChat.Views.Converters
{
    /// <summary>
    /// Improved converter to ensure consistent color values based on boolean values
    /// </summary>
    public class ImprovedBoolToColorConverter : IValueConverter
    {
        /// <summary>
        /// Converts a boolean value to a color
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                // Handle null case to prevent exceptions
                if (value == null)
                {
                    return Colors.Gray;
                }

                // Make sure we have a boolean value
                bool boolValue = false;
                if (value is bool b)
                {
                    boolValue = b;
                }
                else if (value is string strValue && bool.TryParse(strValue, out bool parsedBool))
                {
                    boolValue = parsedBool;
                }

                // Special case for star coloring - always return gold for default models
                if (parameter is string paramString && paramString.Contains("Gold"))
                {
                    if (boolValue)
                    {
                        return Colors.Gold; // Always use gold - never convert this
                    }
                    else
                    {
                        return Colors.Gray;
                    }
                }

                // Handle the parameter string
                if (parameter is string paramStr)
                {
                    // Look for comma-separated color names
                    if (paramStr.Contains(","))
                    {
                        var colorNames = paramStr.Split(',');
                        if (colorNames.Length == 2)
                        {
                            string colorName = boolValue ? colorNames[0].Trim() : colorNames[1].Trim();
                            return GetResourceColor(colorName);
                        }
                    }
                }
                
                // Default colors if no parameters
                return boolValue ? Colors.Green : Colors.Red;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ImprovedBoolToColorConverter: {ex.Message}");
                return Colors.Gray; // Fallback color
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// Gets a color from application resources with better error handling and fallbacks
        /// </summary>
        private Color GetResourceColor(string resourceName)
        {
            try
            {
                if (string.IsNullOrEmpty(resourceName))
                    return Colors.Gray;
                    
                // Special case for gold
                if (resourceName.Equals("Gold", StringComparison.OrdinalIgnoreCase))
                    return Colors.Gold;
                    
                // First check if it's a direct resource from Application.Current.Resources
                if (Application.Current?.Resources.TryGetValue(resourceName, out var resource) == true)
                {
                    if (resource is Color color)
                    {
                        return color;
                    }
                }
                
                // Handle common built-in colors by name
                if (resourceName.Equals("Transparent", StringComparison.OrdinalIgnoreCase))
                    return Colors.Transparent;
                if (resourceName.Equals("White", StringComparison.OrdinalIgnoreCase))
                    return Colors.White;
                if (resourceName.Equals("Black", StringComparison.OrdinalIgnoreCase))
                    return Colors.Black;
                
                // Try with common color names from Colors class
                var property = typeof(Colors).GetProperty(resourceName, 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | 
                    System.Reflection.BindingFlags.IgnoreCase);
                    
                if (property != null && property.PropertyType == typeof(Color))
                {
                    return (Color)property.GetValue(null);
                }
                
                // Try parsing hex color if it starts with #
                if (resourceName.StartsWith("#"))
                {
                    try {
                        return Color.FromArgb(resourceName);
                    }
                    catch {
                        // Ignore parsing errors
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting resource color: {ex.Message}");
            }
            
            return Colors.Gray; // Final fallback
        }
    }
}
