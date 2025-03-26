using System.Globalization;

/// <summary>
/// Converts text length to a width value for UI elements, typically used to dynamically adjust the width of a control based on the length of the text it contains.
/// </summary>
/// <remarks>
/// This converter calculates a width based on the length of the input text, a multiplier, and base width.
/// It also enforces minimum and maximum width constraints. The multiplier can be passed as a converter parameter.
/// </remarks>
/// <example>
/// Usage in XAML:
/// <code>
/// &lt;Label Text="{Binding MyText}"&gt;
///     &lt;Label.Width&gt;
///         &lt;Binding Path="MyText" Converter="{StaticResource TextToWidthConverter}" ConverterParameter="10"/&gt;
///     &lt;/Label.Width&gt;
/// &lt;/Label&gt;
/// </code>
/// In this example, the width of the Label will be dynamically adjusted based on the length of the 'MyText' property.
/// The 'ConverterParameter' is set to "10", which will be used as the multiplier in the width calculation.
/// </example>
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
