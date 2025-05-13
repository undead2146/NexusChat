using System;
using System.Text.RegularExpressions;

namespace NexusChat.Helpers
{
    /// <summary>
    /// Extension methods for string manipulation
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Converts a string to display name format (CamelCase to "Camel Case")
        /// </summary>
        public static string ToDisplayName(this string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
                
            // Add spaces before capital letters
            string result = Regex.Replace(text, "([a-z])([A-Z])", "$1 $2");
            
            // Capitalize first letter
            if (result.Length > 0)
                result = char.ToUpper(result[0]) + result.Substring(1);
                
            return result;
        }
        
        /// <summary>
        /// Normalizes a string for consistent comparison
        /// </summary>
        public static string NormalizeForComparison(this string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
                
            // Remove spaces, convert to lowercase
            return Regex.Replace(text, @"\s+", "").ToLowerInvariant();
        }
        
        /// <summary>
        /// Converts a provider name to a standardized format
        /// </summary>
        public static string StandardizeProviderName(this string providerName)
        {
            if (string.IsNullOrEmpty(providerName))
                return "unknown";
                
            providerName = providerName.Trim();
            
            // Convert to lowercase for consistent lookup
            string normalized = providerName.ToLowerInvariant();
            
            // Return capitalized version
            if (normalized.Length > 0)
                return char.ToUpper(normalized[0]) + normalized.Substring(1);
                
            return normalized;
        }
        
        /// <summary>
        /// Converts a model name to a consistent format for database storage
        /// </summary>
        public static string NormalizeModelName(this string modelName)
        {
            if (string.IsNullOrEmpty(modelName))
                return string.Empty;
                
            // Replace slashes with underscores for storage
            return modelName.Replace('/', '_').ToLowerInvariant();
        }
        
        /// <summary>
        /// Checks if a string contains another string (case insensitive)
        /// </summary>
        public static bool ContainsIgnoreCase(this string source, string value)
        {
            return source != null && value != null && 
                   source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
