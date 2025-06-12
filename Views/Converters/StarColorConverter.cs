using System;
using System.Globalization;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls;

namespace NexusChat.Views.Converters
{
    /// <summary>
    /// Converts boolean favorite status to star color or icon
    /// </summary>
    public class StarColorConverter : IValueConverter
    {
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

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
