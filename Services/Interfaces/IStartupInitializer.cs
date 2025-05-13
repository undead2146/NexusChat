using System.Threading.Tasks;

namespace NexusChat.Services.Interfaces
{
    /// <summary>
    /// Interface for services that require initialization on startup
    /// </summary>
    public interface IStartupInitializer
    {
        /// <summary>
        /// Initializes the service
        /// </summary>
        /// <returns>A task representing the initialization process</returns>
        Task InitializeAsync();
    }
}
