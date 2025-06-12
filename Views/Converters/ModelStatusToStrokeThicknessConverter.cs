using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using NexusChat.Core.Models;

namespace NexusChat.Views.Converters
{
    /// <summary>
    /// Converts model status to border stroke thickness
    /// Shows thicker border for models with special status
    /// </summary>
    public class ModelStatusToStrokeThicknessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not AIModel model)
                return 1.0;

            // Thicker border for models with special status
            if (model.IsSelected || model.IsDefault || model.IsFavorite)
                return 2.0;
            
            return 1.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
