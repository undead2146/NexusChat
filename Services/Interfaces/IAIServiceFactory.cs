using System.Collections.Generic;
using System.Threading.Tasks;
using NexusChat.Core.Models;

namespace NexusChat.Services.Interfaces
{
    /// <summary>
    /// Factory for creating AI services
    /// </summary>
    public interface IAIServiceFactory
    {
        /// <summary>
        /// Creates a service synchronously by model name
        /// </summary>
        IAIService CreateService(string modelName);
        
        /// <summary>
        /// Creates a service asynchronously from provider and configuration
        /// </summary>
        Task<IAIService> CreateServiceAsync(AIProvider provider, ModelConfiguration config);
        
        /// <summary>
        /// Gets all supported provider names
        /// </summary>
        IEnumerable<string> GetSupportedProviders();
    }
}
