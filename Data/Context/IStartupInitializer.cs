using System.Threading.Tasks;

namespace NexusChat.Data.Context
{
    /// <summary>
    /// Interface for startup initialization
    /// </summary>
    public interface IStartupInitializer
    {
        /// <summary>
        /// Initializes required components during app startup
        /// </summary>
        Task InitializeAsync();
    }
}
