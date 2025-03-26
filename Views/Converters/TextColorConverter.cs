using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace NexusChat.Views.Converters
{
    /// <summary>
    /// Converts a bool value to a text color (for message bubbles)
    /// </summary>
    public class TextColorConverter : IValueConverter
    {
        /// <summary>
        /// Converts bool (isAI) to appropriate text color
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isAI)
            {
                if (isAI)
                {
                    // AI messages use darker text
                    return Application.Current?.RequestedTheme == AppTheme.Dark 
                        ? Colors.White 
                        : Colors.Black;
                }
                else
                {
                    // User messages always use white text on colored background
                    return Colors.White;
                }
            }

            // Default
            return Colors.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
