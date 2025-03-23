using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using NexusChat.Core.Models;

namespace NexusChat.Views.Converters
{
    /// <summary>
    /// Converter that returns true if the value is not null
    /// </summary>
    public class NotNullConverter : IValueConverter
    {
        /// <summary>
        /// Gets or sets a value indicating whether the result should be inverted (return true if null)
        /// </summary>
        public bool Invert { get; set; }

        /// <summary>
        /// Also check for empty content in Messages
        /// </summary>
        public bool CheckEmptyContent { get; set; } = true;

        /// <summary>
        /// Converts a value to a boolean indicating whether it's not null
        /// </summary>
        /// <param name="value">The object to check for null</param>
        /// <param name="targetType">The type of the binding target property</param>
        /// <param name="parameter">An optional parameter (not used)</param>
        /// <param name="culture">The culture to use in the converter</param>
        /// <returns>True if the value is not null (unless inverted), otherwise false</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return false;

            // Special handling for Message objects
            if (CheckEmptyContent && value is Message message)
                return message != null && !string.IsNullOrWhiteSpace(message.Content);

            bool isNotNull = value != null;
            return Invert ? !isNotNull : isNotNull;
        }

        /// <summary>
        /// Converts back from a boolean to an object (not implemented)
        /// </summary>
        /// <param name="value">The value to convert back</param>
        /// <param name="targetType">The type to convert to</param>
        /// <param name="parameter">An optional parameter (not used)</param>
        /// <param name="culture">The culture to use in the converter</param>
        /// <returns>Throws NotImplementedException</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("NotNullConverter does not support converting back");
        }
    }
}
