using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace NexusChat.Views.Converters
{
    /// <summary>
    /// Converts a bool value to a color
    /// </summary>
    public class BoolToColorConverter : IValueConverter
    {
        /// <summary>
        /// Converts bool to Color
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // AI message (true) gets lighter background, user message (false) gets primary color
                if (boolValue)
                {
                    // Return light gray for AI messages
                    if (Application.Current?.RequestedTheme == AppTheme.Dark)
                        return Color.FromArgb("#303030");
                    else
                        return Color.FromArgb("#F0F0F0"); 
                }
                else
                {
                    // Return blue for user messages
                    if (Application.Current?.RequestedTheme == AppTheme.Dark)
                        return Color.FromArgb("#0A84FF");
                    else
                        return Color.FromArgb("#007AFF");
                }
            }

            // Default color if conversion fails
            return Colors.Gray;
        }

        /// <summary>
        /// Converts back from Color to bool (not implemented)
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
