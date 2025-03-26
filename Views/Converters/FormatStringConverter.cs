using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace NexusChat.Views.Converters
{
    /// <summary>
    /// Converter to format a string with a parameter
    /// </summary>
    public class FormatStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;

            // If no parameter provided, just return the value as string
            if (parameter == null)
                return value.ToString();

            // Extract the parameter value if it's a binding
            string paramValue = parameter?.ToString() ?? "?";
            
            // Try to format using the parameter as format specifier
            try
            {
                return string.Format("Page {0} of {1}", value, paramValue);
            }
            catch (Exception)
            {
                return $"Page {value} of {paramValue}";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
