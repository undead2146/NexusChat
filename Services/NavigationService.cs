using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NexusChat.Services.Interfaces;

namespace NexusChat.Services
{
    /// <summary>
    /// Service for handling application navigation
    /// </summary>
    public class NavigationService : INavigationService
    {
        /// <summary>
        /// Navigates to a route
        /// </summary>
        /// <param name="route">The route</param>
        public async Task NavigateToAsync(string route)
        {
            try
            {
                await Shell.Current.GoToAsync(route);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Navigation error to {route}: {ex.Message}");
            }
        }

        /// <summary>
        /// Navigates to a route with parameter
        /// </summary>
        /// <param name="route">The route</param>
        /// <param name="parameter">Route parameter</param>
        public async Task NavigateToAsync(string route, object parameter)
        {
            try
            {
                await Shell.Current.GoToAsync(route, new Dictionary<string, object>
                {
                    { "Parameter", parameter }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Navigation error to {route} with parameter: {ex.Message}");
            }
        }

        /// <summary>
        /// Navigates back
        /// </summary>
        public async Task GoBackAsync()
        {
            try
            {
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Navigation back error: {ex.Message}");
            }
        }

        /// <summary>
        /// Registers routes for navigation
        /// </summary>
        public void RegisterRoutes()
        {
            try
            {
                Routing.RegisterRoute(nameof(Views.Pages.ChatPage), typeof(Views.Pages.ChatPage));
                Routing.RegisterRoute(nameof(Views.Pages.AIModelsPage), typeof(Views.Pages.AIModelsPage));

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error registering routes: {ex.Message}");
            }
        }

        /// <summary>
        /// Navigates to a route with immediate execution
        /// </summary>
        /// <param name="route">The route</param>
        /// <param name="parameters">Route parameters</param>
        public async Task NavigateToAsync(string route, IDictionary<string, object> parameters = null)
        {
            try
            {
                // Navigate immediately without waiting for page initialization
                if (parameters != null)
                {
                    await Shell.Current.GoToAsync(route, parameters);
                }
                else
                {
                    await Shell.Current.GoToAsync(route);
                }
            }
            catch (Exception ex)
            {
                // Handle navigation errors
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
            }
        }
    }
}
