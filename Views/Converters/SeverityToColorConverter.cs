using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace NexusChat.Views.Converters
{
    /// <summary>
    /// Converts a severity value (integer) to an appropriate color
    /// </summary>
    public class SeverityToColorConverter : IValueConverter
    {
        /// <summary>
        /// Color to use for high severity (4-5)
        /// </summary>
        public Color HighSeverityColor { get; set; } = Colors.LightPink;

        /// <summary>
        /// Color to use for normal severity (1-3)
        /// </summary>
        public Color NormalSeverityColor { get; set; } = Colors.LightGray;

        /// <summary>
        /// Threshold value - severity values >= this threshold will use HighSeverityColor
        /// </summary>
        public int Threshold { get; set; } = 4;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int severityValue)
            {
                if (parameter is string paramStr && paramStr.Equals("text", StringComparison.OrdinalIgnoreCase))
                {
                    // For text color
                    return severityValue >= Threshold ? Colors.DarkRed : Colors.DimGray;
                }
                else
                {
                    // For background color
                    return severityValue >= Threshold ? HighSeverityColor : NormalSeverityColor;
                }
            }
            
            return Colors.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
