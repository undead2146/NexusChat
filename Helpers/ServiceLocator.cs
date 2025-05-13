using System;
using Microsoft.Extensions.DependencyInjection;
using NexusChat.Services.AIManagement;
using NexusChat.Services.AIProviders;
using NexusChat.Services.Interfaces;

namespace NexusChat.Helpers
{
    /// <summary>
    /// Simple service locator for accessing services from non-DI contexts like XAML
    /// </summary>
    public static class ServiceLocator
    {
        private static IServiceProvider _serviceProvider;

        /// <summary>
        /// Initialize the service locator with the application's service provider
        /// </summary>
        public static void Initialize(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Get a service of the specified type
        /// </summary>
        public static T GetService<T>() where T : class
        {
            if (_serviceProvider == null)
            {
                throw new InvalidOperationException("ServiceLocator has not been initialized. Call Initialize first.");
            }
            
            return _serviceProvider.GetService<T>();
        }
    }
}
