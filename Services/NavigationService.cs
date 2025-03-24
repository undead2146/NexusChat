using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NexusChat.Services
{
    /// <summary>
    /// Service for handling application navigation
    /// </summary>
    public class NavigationService
    {
        private readonly Dictionary<string, Type> _routes = new Dictionary<string, Type>();
        private bool _isNavigating = false;

        /// <summary>
        /// Registers available routes for navigation
        /// </summary>
        public void RegisterRoutes()
        {
            // Register all app routes here
            foreach (var route in GetRoutes())
            {
                Routing.RegisterRoute(route.Key, route.Value);
            }
        }

        /// <summary>
        /// Gets the dictionary of route mappings
        /// </summary>
        private Dictionary<string, Type> GetRoutes()
        {
            // Centralize route definitions here
            return new Dictionary<string, Type>
            {
                { "ChatPage", typeof(NexusChat.Views.Pages.ChatPage) },
                { "ThemesPage", typeof(NexusChat.Views.Pages.DevTools.ThemesPage) },
                { "ModelTestingPage", typeof(NexusChat.Views.Pages.DevTools.ModelTestingPage) },
                { "DatabaseViewerPage", typeof(NexusChat.Views.Pages.DevTools.DatabaseViewerPage) }
            };
        }

        /// <summary>
        /// Navigate to a specific route
        /// </summary>
        public async Task NavigateToAsync(string route, Dictionary<string, object> parameters = null)
        {
            if (_isNavigating)
                return;

            try
            {
                _isNavigating = true;

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
                Debug.WriteLine($"Navigation error to {route}: {ex.Message}");
            }
            finally
            {
                // Add a small delay to prevent rapid navigation attempts
                await Task.Delay(300);
                _isNavigating = false;
            }
        }

        /// <summary>
        /// Navigate back to the previous page
        /// </summary>
        public async Task NavigateBackAsync()
        {
            if (_isNavigating)
                return;

            try
            {
                _isNavigating = true;
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Navigation back error: {ex.Message}");
            }
            finally
            {
                _isNavigating = false;
            }
        }
    }
}
