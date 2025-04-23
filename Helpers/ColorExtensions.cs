using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace NexusChat.Helpers
{
    /// <summary>
    /// Extensions for working with colors in the application
    /// </summary>
    public static class ColorExtensions
    {
        // Cache for frequently accessed colors to avoid repeated lookups
        private static readonly Dictionary<string, Color> _colorCache = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
        
        /// <summary>
        /// Gets a color from application resources
        /// </summary>
        /// <param name="resourceKey">The resource key of the color</param>
        /// <returns>The color from resources or Gray if not found</returns>
        public static Color GetResourceColor(string resourceKey)
        {
            if (string.IsNullOrEmpty(resourceKey))
                return Colors.Gray;
                
            // Check cache first
            if (_colorCache.TryGetValue(resourceKey, out Color cachedColor))
                return cachedColor;
                
            try
            {
                // Try to get the color from application resources
                if (Application.Current?.Resources.TryGetValue(resourceKey, out var resource) == true)
                {
                    if (resource is Color color)
                    {
                        // Cache the result
                        _colorCache[resourceKey] = color;
                        return color;
                    }
                }
                
                // Handle built-in color names
                if (TryGetColorByName(resourceKey, out Color namedColor))
                {
                    _colorCache[resourceKey] = namedColor;
                    return namedColor;
                }
                
                // Try to parse as hex color
                if (resourceKey.StartsWith("#") && Color.TryParse(resourceKey, out Color parsedColor))
                {
                    _colorCache[resourceKey] = parsedColor;
                    return parsedColor;
                }
                
                // Special handling for common color names
                switch (resourceKey.ToLowerInvariant())
                {
                    case "primary": return GetResourceColor("Primary");
                    case "secondary": return GetResourceColor("Secondary");
                    case "accent": return GetResourceColor("Accent");
                    case "success": return GetResourceColor("Success");
                    case "warning": return GetResourceColor("Warning");
                    case "danger": return GetResourceColor("Danger");
                    case "error": return GetResourceColor("Error");
                    case "info": return GetResourceColor("Info");
                    case "transparent": return Colors.Transparent;
                    case "white": return Colors.White;
                    case "black": return Colors.Black;
                    case "gray":
                    case "grey": return Colors.Gray;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting resource color '{resourceKey}': {ex.Message}");
            }
            
            return Colors.Gray;
        }
        
        /// <summary>
        /// Attempts to get a color by name from the Colors class
        /// </summary>
        private static bool TryGetColorByName(string colorName, out Color color)
        {
            try
            {
                // First try exact match
                var property = typeof(Colors).GetProperty(colorName,
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    
                if (property != null)
                {
                    color = (Color)property.GetValue(null);
                    return true;
                }
                
                // Then try case-insensitive match
                foreach (var prop in typeof(Colors).GetProperties(
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
                {
                    if (prop.PropertyType == typeof(Color) &&
                        prop.Name.Equals(colorName, StringComparison.OrdinalIgnoreCase))
                    {
                        color = (Color)prop.GetValue(null);
                        return true;
                    }
                }
            }
            catch
            {
                // Ignore reflection errors
            }
            
            color = Colors.Gray;
            return false;
        }
    }
}
