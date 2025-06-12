using System;
using System.Collections.Generic;
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
        private static readonly Dictionary<string, Color> _providerColors = new(StringComparer.OrdinalIgnoreCase)
        {
            ["openai"] = Color.FromArgb("#10A37F"),
            ["openrouter"] = Color.FromArgb("#9661FC"),
            ["groq"] = Color.FromArgb("#FF6600"),
            ["claude"] = Color.FromArgb("#A166FF"),
            ["anthropic"] = Color.FromArgb("#A166FF"),
            ["google"] = Color.FromArgb("#4285F4"),
            ["azure"] = Color.FromArgb("#00A4EF"),
            ["dummy"] = Color.FromArgb("#888888")
        };

        private static readonly Color _defaultColor = Color.FromArgb("#0078D7");

        /// <summary>
        /// Converts a provider name to a brand color
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string providerName && !string.IsNullOrEmpty(providerName))
            {
                return _providerColors.TryGetValue(providerName, out var color) ? color : _defaultColor;
            }
            
            return _defaultColor;
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
