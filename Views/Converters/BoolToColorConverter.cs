using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace NexusChat.Views.Converters
{
    /// <summary>
    /// Converter that changes color based on a boolean value
    /// </summary>
    public class BoolToColorConverter : IValueConverter
    {
        /// <summary>
        /// Gets or sets the color to use when the value is true
        /// </summary>
        public object TrueValue { get; set; }
        
        /// <summary>
        /// Gets or sets the color to use when the value is false
        /// </summary>
        public object FalseValue { get; set; }
        
        /// <summary>
        /// Converts a boolean to a color
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // If the parameter is true, invert the logic
                if (parameter is bool invertLogic && invertLogic)
                {
                    boolValue = !boolValue;
                }
                
                return boolValue ? TrueValue : FalseValue;
            }
            
            // Default to the false value if the input isn't a boolean
            return FalseValue;
        }
        
        /// <summary>
        /// Not implemented - converting from color to boolean
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
