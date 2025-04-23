using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Converters;

namespace NexusChat.Views.Converters
{
    /// <summary>
    /// Converts a boolean value to a star color for favorite model indication
    /// </summary>
    public class StarColorConverter : IValueConverter
    {
        /// <summary>
        /// Converts a boolean value to a star color
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isFavorite)
            {
                // Use gold color for favorite models
                return isFavorite ? Colors.Gold : Colors.Gray;
            }
            
            return Colors.Gray;
        }
        
        /// <summary>
        /// Converts a color back to a boolean (not implemented)
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
