using Microsoft.Maui.Controls;
using System;
using System.Globalization;

namespace NexusChat.Views.Converters
{
    // Converter that calculates width based on text length
    public class TextToWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text && !string.IsNullOrEmpty(text))
            {
                // Basic calculation: character count * estimated character width
                double baseWidth = text.Length * 8;
                double minWidth = 100;
                double maxWidth = 300;
                
                return Math.Max(minWidth, Math.Min(maxWidth, baseWidth));
            }
            
            return 100; // Default width
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
