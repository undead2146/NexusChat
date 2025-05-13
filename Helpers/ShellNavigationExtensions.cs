using System;
using System.Collections.Specialized;
using System.Web;
using Microsoft.Maui.Controls;

namespace NexusChat.Helpers
{
    /// <summary>
    /// Extension methods for shell navigation
    /// </summary>
    public static class ShellNavigationExtensions
    {
        /// <summary>
        /// Gets a navigation parameter from the shell navigation state
        /// </summary>
        /// <typeparam name="T">Type to convert the parameter to</typeparam>
        /// <param name="state">The shell navigation state</param>
        /// <param name="paramName">The parameter name</param>
        /// <param name="defaultValue">Default value if parameter not found</param>
        /// <returns>The parameter value or default</returns>
        public static T GetNavigationParameter<T>(this ShellNavigationState state, string paramName, T defaultValue = default)
        {
            try
            {
                var uri = new Uri(state.Location.ToString());
                
                // Parse the query string
                var queryString = HttpUtility.ParseQueryString(uri.Query);
                string value = queryString[paramName];
                
                if (string.IsNullOrEmpty(value))
                    return defaultValue;
                
                try
                {
                    // Handle different types
                    if (typeof(T) == typeof(string))
                        return (T)(object)value;
                    
                    if (typeof(T) == typeof(int))
                        return (T)(object)int.Parse(value);
                        
                    if (typeof(T) == typeof(bool))
                        return (T)(object)bool.Parse(value);
                    
                    if (typeof(T) == typeof(int?))
                    {
                        if (int.TryParse(value, out int result))
                            return (T)(object)(int?)result;
                        return defaultValue;
                    }
                    
                    // Try type conversion for other types
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}
