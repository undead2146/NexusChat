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
            bool isFavorite = value is bool favorite && favorite;
            string param = parameter?.ToString() ?? "";
            
            if (param.Equals("Icon", StringComparison.OrdinalIgnoreCase))
            {
                return isFavorite ? "\uf005" : "\uf006"; // Solid star or outlined star
            }
            
            return isFavorite ? Colors.Gold : Colors.Gray;
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
