using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace NexusChat.Views.Converters
{
    /// <summary>
    /// Checks if a value equals the parameter
    /// </summary>
    public class EqualityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Get the inversion flag if provided
            bool invert = false;
            if (parameter is IConvertible[] parameters && parameters.Length > 1)
            {
                parameter = parameters[0];
                if (parameters[1] is bool invertParam)
                {
                    invert = invertParam;
                }
            }

            // Check equality
            bool result = (value?.ToString() == parameter?.ToString());
            
            // Apply inversion if needed
            return invert ? !result : result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
