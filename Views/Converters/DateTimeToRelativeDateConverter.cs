using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace NexusChat.Views.Converters
{
    /// <summary>
    /// Converts a DateTime to a relative date string (e.g., "2 days ago")
    /// </summary>
    public class DateTimeToRelativeDateConverter : IValueConverter
    {
        /// <summary>
        /// Converts a DateTime to a relative date string
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not DateTime dateTime)
                return string.Empty;
                
            var now = DateTime.Now;
            var timeSpan = now - dateTime;
            
            if (timeSpan.TotalMinutes < 1)
                return "Just now";
                
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes}m ago";
                
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours}h ago";
                
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays}d ago";
                
            if (timeSpan.TotalDays < 30)
                return $"{(int)(timeSpan.TotalDays / 7)}w ago";
                
            if (timeSpan.TotalDays < 365)
                return dateTime.ToString("MMM d");
                
            return dateTime.ToString("MMM d, yyyy");
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
