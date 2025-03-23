using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace NexusChat.Views.Converters
{
    /// <summary>
    /// Converts a DateTime to a friendly time string
    /// </summary>
    public class DateTimeToTimeStringConverter : IValueConverter
    {
        /// <summary>
        /// Converts a DateTime to a user-friendly time string
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                var now = DateTime.Now;
                var date = dateTime.Date;
                
                // Today - just show time
                if (date.Date == now.Date)
                {
                    return dateTime.ToString("t"); // Short time pattern (e.g., 3:15 PM)
                }
                
                // Yesterday
                if (date.Date == now.Date.AddDays(-1))
                {
                    return "Yesterday " + dateTime.ToString("t");
                }
                
                // Within last 7 days
                if ((now.Date - date.Date).TotalDays < 7)
                {
                    return dateTime.ToString("ddd t"); // Day name + time (e.g., Mon 3:15 PM)
                }
                
                // This year
                if (date.Year == now.Year)
                {
                    return dateTime.ToString("MMM d, t"); // Month day + time (e.g., Jan 15, 3:15 PM)
                }
                
                // Different year
                return dateTime.ToString("MMM d, yyyy, t"); // Full date + time (e.g., Jan 15, 2023, 3:15 PM)
            }
            
            return string.Empty;
        }

        /// <summary>
        /// Converts back from a string to a DateTime (not implemented)
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
