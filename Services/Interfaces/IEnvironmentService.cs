using System.Collections.Generic;
using System.Threading.Tasks;

namespace NexusChat.Services.Interfaces
{
    /// <summary>
    /// Service to interact with environment variables and settings
    /// </summary>
    public interface IEnvironmentService : IStartupInitializer
    {
        // Common prefixes as constants
        string API_KEY_PREFIX { get; }
        string MODEL_KEY_PREFIX { get; }
        
        /// <summary>
        /// Gets the value of an environment variable
        /// </summary>
        string GetValue(string name);
        
        /// <summary>
        /// Sets an environment variable
        /// </summary>
        void SetValue(string name, string value);
        
        /// <summary>
        /// Gets an API key for a provider
        /// </summary>
        string GetApiKey(string providerName);
        
        /// <summary>
        /// Gets a model-specific API key
        /// </summary>
        string GetModelApiKey(string providerName, string modelName);
        
        /// <summary>
        /// Gets all available API keys
        /// </summary>
        Dictionary<string, string> GetAllApiKeys();
        
        /// <summary>
        /// Gets all available model configurations
        /// </summary>
        Dictionary<string, string> GetModelConfigurations();
        
        /// <summary>
        /// Gets all environment variables
        /// </summary>
        Dictionary<string, string> GetAllEnvironmentVariables();
        
        /// <summary>
        /// Gets all environment variables (alias for GetAllEnvironmentVariables)
        /// </summary>
        Dictionary<string, string> GetAllVariables();
        
        /// <summary>
        /// Gets all environment variables with the given prefix
        /// </summary>
        Dictionary<string, string> GetVariablesWithPrefix(string prefix);
        
        /// <summary>
        /// Gets a specific environment variable
        /// </summary>
        string GetEnvironmentVariable(string name);
        
        /// <summary>
        /// Tries to get an environment variable, returns success status
        /// </summary>
        bool TryGetEnvironmentVariable(string name, out string value);
        
        /// <summary>
        /// Loads environment variables
        /// </summary>
        void LoadEnvironmentVariables();
    }
}
