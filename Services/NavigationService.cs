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
                Routing.RegisterRoute(nameof(Views.Pages.DevTools.ThemesPage), typeof(Views.Pages.DevTools.ThemesPage));
                Routing.RegisterRoute(nameof(Views.Pages.DevTools.DatabaseViewerPage), typeof(Views.Pages.DevTools.DatabaseViewerPage));
                Routing.RegisterRoute(nameof(Views.Pages.DevTools.ModelTestingPage), typeof(Views.Pages.DevTools.ModelTestingPage));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error registering routes: {ex.Message}");
            }
        }
    }
}
