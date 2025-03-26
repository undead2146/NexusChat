using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using NexusChat.Core.Models;

namespace NexusChat.Views.Converters
{
    /// <summary>
    /// Converts a value to a boolean indicating whether it's null/empty/zero
    /// </summary>
    public class NotNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Special case for Message types
            if (parameter is Message paramMessage && value is Message valueMessage)
            {
                // Check if the typing message is the current message
                return paramMessage.Id == valueMessage.Id;
            }
            
            // If parameter is null, just check if value is not null
            if (parameter == null)
            {
                return value != null;
            }
            
            // Default case - parameter exists but doesn't match specific conditions
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
