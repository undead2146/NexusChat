using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace NexusChat.Views.Converters
{
    /// <summary>
    /// Converts an AI provider name to a brand color
    /// </summary>
    public class ProviderToColorConverter : IValueConverter
    {
        // Provider brand colors
        private static readonly Dictionary<string, Color> ProviderColors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
        {
            { "Groq", Color.FromArgb("#25D7FD") },        // Light blue
            { "OpenRouter", Color.FromArgb("#FF5733") },  // Orange
            { "OpenAI", Color.FromArgb("#10a37f") },      // Green
            { "Anthropic", Color.FromArgb("#b83280") },   // Purple
            { "Google", Color.FromArgb("#4285F4") },      // Blue
            { "Mistral", Color.FromArgb("#5F4B8B") },     // Purple-blue
            { "Dummy", Color.FromArgb("#808080") }        // Gray
        };
        
        /// <summary>
        /// Gets or sets the fallback color for unknown providers
        /// </summary>
        public Color FallbackColor { get; set; } = Colors.SlateGray;
        
        /// <summary>
        /// Converts a provider name to a color
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string providerName && !string.IsNullOrEmpty(providerName))
            {
                if (ProviderColors.TryGetValue(providerName, out Color color))
                {
                    return color;
                }
                
                // Handle OpenRouter's models that might be prefixed with provider/model
                if (providerName.Contains('/'))
                {
                    var parts = providerName.Split('/');
                    if (parts.Length >= 2 && ProviderColors.TryGetValue(parts[0], out color))
                    {
                        return color;
                    }
                }
            }
            
            return FallbackColor;
        }
        
        /// <summary>
        /// Converts a color back to a provider name (not implemented)
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
