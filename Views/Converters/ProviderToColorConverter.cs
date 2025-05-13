using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace NexusChat.Views.Converters
{
    /// <summary>
    /// Converts provider names to brand-appropriate colors
    /// </summary>
    public class ProviderToColorConverter : IValueConverter
    {
        /// <summary>
        /// Converts a provider name to a brand color
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string providerName)
            {
                // Return appropriate brand colors based on provider name
                return providerName.ToLowerInvariant() switch
                {
                    "openai" => Color.Parse("#10A37F"), // OpenAI green
                    "openrouter" => Color.Parse("#9661FC"), // Purple
                    "groq" => Color.Parse("#FF6600"),     // Orange
                    "claude" => Color.Parse("#A166FF"),    // Purple
                    "anthropic" => Color.Parse("#A166FF"), // Purple
                    "google" => Color.Parse("#4285F4"),    // Google blue
                    "azure" => Color.Parse("#00A4EF"),     // Azure blue
                    "dummy" => Color.Parse("#888888"),     // Gray
                    _ => Color.Parse("#0078D7"),           // Default blue
                };
            }
            
            // Default fallback color
            return Color.Parse("#999999");
        }

        /// <summary>
        /// Not implemented for this converter
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
