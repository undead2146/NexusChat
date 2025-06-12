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

            // Selected gets thickest border, then default, then favorite
            if (model.IsSelected)
                return 3.0;
            
            if (model.IsDefault)
                return 2.5;
                
            if (model.IsFavorite)
                return 2.0;
            
            return 1.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
