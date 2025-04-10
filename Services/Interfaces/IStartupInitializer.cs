using System.Threading.Tasks;

namespace NexusChat.Services.Interfaces
{
    /// <summary>
    /// Interface for services that need to perform initialization at application startup
    /// </summary>
    public interface IStartupInitializer
    {
        /// <summary>
        /// Initializes the service
        /// </summary>
        Task InitializeAsync();
    }
}
