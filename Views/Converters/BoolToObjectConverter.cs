
using System;
using System.Globalization;
using System.Diagnostics;
using Microsoft.Maui.Controls;

namespace NexusChat.Views.Converters
{
    /// <summary>
    /// A value converter that converts a boolean value to an object based on a comma-separated string parameter.
    /// The parameter should contain two resource keys, the first for when the boolean value is true, and the second for when the boolean value is false.
    /// The converter attempts to find the resource in the following order:
    /// 1. Application.Current.Resources
    /// 2. MainPage.Resources
    /// 3. If MainPage is a NavigationPage, NavigationPage.CurrentPage.Resources
    /// 4. If MainPage is a Shell, Shell.CurrentPage.Resources
    /// If the resource is found, it is returned. Otherwise, null is returned.
    /// </summary>
    public class BoolToObjectConverter : IValueConverter, IMarkupExtension
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is not bool boolValue || parameter is not string paramString)
                    return null;

                string[] parameters = paramString.Split(',');
                if (parameters.Length != 2)
                {
                    Debug.WriteLine("BoolToObjectConverter: Parameter string must contain two comma-separated values");
                    return null;
                }

                // Get the resource key based on the bool value
                string resourceKey = boolValue ? parameters[0].Trim() : parameters[1].Trim();

                // Try to find the resource directly in Application.Current.Resources
                if (Application.Current?.Resources?.TryGetValue(resourceKey, out object resource) == true)
                {
                    Debug.WriteLine($"BoolToObjectConverter: Found resource {resourceKey} in application resources");
                    return resource;
                }

                // If not found in app resources, check page resources
                if (Application.Current?.MainPage is Page mainPage)
                {
                    // Check page resources
                    if (mainPage.Resources?.TryGetValue(resourceKey, out object pageResource) == true)
                    {
                        Debug.WriteLine($"BoolToObjectConverter: Found resource {resourceKey} in page resources");
                        return pageResource;
                    }

                    // Check current page if we're in a navigation stack
                    if (mainPage is NavigationPage navPage && 
                        navPage.CurrentPage?.Resources?.TryGetValue(resourceKey, out object currentPageResource) == true)
                    {
                        Debug.WriteLine($"BoolToObjectConverter: Found resource {resourceKey} in current page resources");
                        return currentPageResource;
                    }

                    // Check shell content if we're using Shell
                    if (mainPage is Shell shell && 
                        shell.CurrentPage?.Resources?.TryGetValue(resourceKey, out object shellPageResource) == true)
                    {
                        Debug.WriteLine($"BoolToObjectConverter: Found resource {resourceKey} in shell page resources");
                        return shellPageResource;
                    }
                }

                Debug.WriteLine($"BoolToObjectConverter: Resource {resourceKey} not found");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in BoolToObjectConverter: {ex.Message}");
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}
