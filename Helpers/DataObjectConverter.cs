using System;
using System.Collections.Generic;
using System.Reflection;

namespace NexusChat.Helpers
{
    /// <summary>
    /// Helper class to convert objects to dictionaries for display
    /// </summary>
    public class DataObjectConverter
    {
        /// <summary>
        /// Converts an object to a dictionary of property names and values
        /// </summary>
        public Dictionary<string, object> ObjectToDictionary(object obj)
        {
            var dict = new Dictionary<string, object>();
            
            if (obj == null) return dict;
            
            // Use reflection to get properties and values
            foreach (var prop in obj.GetType().GetProperties())
            {
                try
                {
                    var value = prop.GetValue(obj);
                    dict[prop.Name] = value;
                }
                catch
                {
                    // If we can't get a property value, store null
                    dict[prop.Name] = null;
                }
            }
            
            return dict;
        }
    }
}
