using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace NexusChat.Views.Converters
{
    /// <summary>
    /// Converts component names to appropriate FontAwesome icons
    /// </summary>
    public class ComponentToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string componentName)
                return "\uf1c5"; // Default file image icon

            return componentName switch
            {
                "Colors" => "\uf53f",        // Palette icon
                "Typography" => "\uf031",     // Text/font icon
                "Buttons" => "\uf0a6",       // Hand pointer icon
                "InputControls" => "\uf11c",  // Keyboard icon
                "ChatComponents" => "\uf4ad", // Comment dots icon
                "StatusIndicators" => "\uf0e7", // Bolt icon
                "LayoutComponents" => "\uf0db", // Columns icon
                "FormComponents" => "\uf046",  // Check square icon
                "Accessibility" => "\uf2a2",  // Universal access icon
                "Icons" => "\uf0c3",          // Flask icon
                _ => "\uf059"                // Question mark icon for unknown
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
