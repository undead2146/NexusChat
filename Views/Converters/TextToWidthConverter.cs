using System.Globalization;

namespace NexusChat.Views.Converters
{
    public class TextToWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text && !string.IsNullOrEmpty(text))
            {
                // Get the multiplier from parameter or use default
                double multiplier = 20; // Default multiplier
                if (parameter is string paramStr && double.TryParse(paramStr, out double paramValue))
                {
                    multiplier = paramValue;
                }

                // Calculate width based on text length
                double baseWidth = 80; // Base width for padding and margins
                double calculatedWidth = baseWidth + (text.Length * multiplier);
                
                // Set minimum and maximum constraints
                double minWidth = 120;
                double maxWidth = 300;

                return Math.Clamp(calculatedWidth, minWidth, maxWidth);
            }

            return 120; // Default minimum width for empty or null text
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
