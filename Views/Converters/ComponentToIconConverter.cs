using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace NexusChat.Views.Converters
{
    /// <summary>
    /// Converts component names to appropriate  icons
    /// </summary>
    public class ComponentToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string componentName)
            {
                // Map component names to Unicode characters
                return componentName switch
                {
                    "Buttons" => "\uf0a6", // f0a6 - hand-point-up
                    "Typography" => "\uf031", // f031 - font
                    "Icons" => "\uf0c9", // f0c9 - bars
                    "FormComponents" => "\uf022", // f022 - list-alt
                    "ChatComponents" => "\uf086", // f086 - comments
                    "Colors" => "\uf53f", // f53f - palette
                    "Accessibilities" => "\uf29d", // f29d - universal-access
                    "StatusIndicators" => "\uf05a", // f05a - info-circle
                    "LayoutComponents" => "\uf0db", // f0db - table
                    "InputControls" => "\uf044", // f044 - edit
                    _ => "\uf05a", // Default icon - info-circle
                };
            }

            return "\uf05a"; // Default icon
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
