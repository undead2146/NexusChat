using System;
using System.Globalization;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls;
using NexusChat.Core.Models;

namespace NexusChat.Views.Converters
{
    /// <summary>
    /// Converts model status properties to appropriate border colors
    /// Priority: Selected (blue) > Default (green) > Favorite (yellow) > None (transparent)
    /// </summary>
    public class ModelStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not AIModel model)
                return Colors.Transparent;

            // Hierarchy: Selected (blue) > Default (green) > Favorite (gold) > None (transparent)
            if (model.IsSelected)
                return Colors.Blue;
            
            if (model.IsDefault)
                return Colors.Green;
            
            if (model.IsFavorite)
                return Colors.Gold;
            
            return Colors.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
