using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace NexusChat.Views.Converters
{
    /// <summary>
    /// Converts an integer value to a boolean by comparing with a parameter
    /// </summary>
    public class IntToBoolConverter : IValueConverter
    {
        /// <summary>
        /// Converts an integer to a boolean
        /// </summary>
        /// <param name="value">The integer value (typically a count)</param>
        /// <param name="targetType">The target type (bool)</param>
        /// <param name="parameter">The comparison value - if null, checks if value equals 0</param>
        /// <param name="culture">The culture info</param>
        /// <returns>True if value equals parameter, otherwise false</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return true; // If null, there are no items
                
            if (value is int count)
            {
                // If parameter is provided, compare with it
                if (parameter != null && int.TryParse(parameter.ToString(), out int paramValue))
                {
                    return count == paramValue;
                }
                
                // Otherwise, check if empty
                return count == 0;
            }
            
            return false;
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
