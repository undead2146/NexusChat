using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace NexusChat.Views.Converters
{
    /// <summary>
    /// Converts boolean value to star color (gold for favorites)
    /// </summary>
    public class StarColorConverter : IValueConverter
    {
        /// <summary>
        /// Convert bool to color
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isFavorite && isFavorite)
            {
                return Color.FromArgb("#FFD700"); // Gold
            }
            return Color.FromArgb("#CCCCCC"); // Gray
        }

        /// <summary>
        /// Convert back - not implemented
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
