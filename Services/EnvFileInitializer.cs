using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NexusChat.Services.Interfaces;

namespace NexusChat.Services
{
    /// <summary>
    /// Service for initializing environment variables from .env files
    /// </summary>
    public class EnvFileInitializer : IStartupInitializer, IEnvironmentService
    {
        private const string ENV_FILE = ".env";
        private const string LOCAL_ENV_FILE = "Local/.env";

        private const string _apiKeyPrefix = "AI_KEY_";
        private const string _modelKeyPrefix = "AI_MODEL_";

        // Implement as properties (not constants) to satisfy interface
        public string API_KEY_PREFIX => _apiKeyPrefix;
        public string MODEL_KEY_PREFIX => _modelKeyPrefix;

        // Cache of environment variables to avoid repeated lookups
        private readonly Dictionary<string, string> _envCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private bool _isInitialized = false;
        private readonly object _lockObj = new object();

        /// <summary>
        /// Initializes environment variables from .env files
        /// </summary>
        public Task InitializeAsync()
        {
            if (_isInitialized)
            {
                return Task.CompletedTask;
            }

            lock (_lockObj)
            {
                if (_isInitialized)
                {
                    return Task.CompletedTask;
                }

                try
                {
                    // Load from the main .env file first
                    var mainEnvPath = GetEnvFilePath(ENV_FILE);
                    bool mainEnvLoaded = LoadEnvFile(mainEnvPath);
                    Debug.WriteLine($"Main .env file loaded: {mainEnvLoaded} from {mainEnvPath}");

                    // Then try to load from Local/.env which can override settings
                    var localEnvPath = GetEnvFilePath(LOCAL_ENV_FILE);
                    bool localEnvLoaded = LoadEnvFile(localEnvPath);
                    Debug.WriteLine($"Local .env file loaded: {localEnvLoaded} from {localEnvPath}");

                    // Try fallback locations if neither file was found
                    if (!mainEnvLoaded && !localEnvLoaded)
                    {
                        LoadFromFallbackLocations();
                    }

                    // Fill the cache with all environment variables
                    PopulateCache();

                    // Count environment variables for debug
                    int keyCount = _envCache.Count(x => x.Key.StartsWith(_apiKeyPrefix, StringComparison.OrdinalIgnoreCase));
                    int modelCount = _envCache.Count(x => x.Key.StartsWith(_modelKeyPrefix, StringComparison.OrdinalIgnoreCase));

                    Debug.WriteLine($"Environment loaded: {keyCount} API keys and {modelCount} model configurations");

                    // Output some sample variables for debugging (securely)
                    LogSampleVariables();

                    _isInitialized = true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error loading .env file: {ex.Message}");
                }

                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Populates the environment variable cache
        /// </summary>
        private void PopulateCache()
        {
            _envCache.Clear();
            foreach (var key in Environment.GetEnvironmentVariables().Keys)
            {
                string keyStr = key.ToString();
                string value = Environment.GetEnvironmentVariable(keyStr);
                if (!string.IsNullOrEmpty(value))
                {
                    _envCache[keyStr] = value;
                }
            }
        }

        /// <summary>
        /// Logs sample variables for debugging
        /// </summary>
        private void LogSampleVariables()
        {
            // Log a few API keys (securely)
            var apiKeys = _envCache.Keys
                .Where(k => k.StartsWith(_apiKeyPrefix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(k => k)
                .Take(3);

            foreach (var key in apiKeys)
            {
                var value = _envCache[key];
                // Show only first few characters of key for security
                Debug.WriteLine($"  {key}: {value.Substring(0, Math.Min(4, value.Length))}...[MASKED]");
            }

            // Log a few model configurations
            var modelConfigs = _envCache.Keys
                .Where(k => k.StartsWith(_modelKeyPrefix, StringComparison.OrdinalIgnoreCase) &&
                       !k.StartsWith(_modelKeyPrefix + "DESC_", StringComparison.OrdinalIgnoreCase))
                .OrderBy(k => k)
                .Take(5);

            foreach (var key in modelConfigs)
            {
                Debug.WriteLine($"  {key}: {_envCache[key]}");
            }
        }

        /// <summary>
        /// Gets the value of an environment variable
        /// </summary>
        public string GetValue(string name)
        {
            EnsureInitialized();
            return GetEnvironmentVariable(name);
        }

        /// <summary>
        /// Sets an environment variable
        /// </summary>
        public void SetValue(string name, string value)
        {
            Environment.SetEnvironmentVariable(name, value);
        }

        /// <summary>
        /// Gets an API key for a provider
        /// </summary>
        public string GetApiKey(string providerName)
        {
            if (string.IsNullOrEmpty(providerName))
                return null;

            // Try the standard format
            var key = GetValue($"{API_KEY_PREFIX}{providerName.ToUpperInvariant()}");

            // If not found, try alternative formats
            if (string.IsNullOrEmpty(key))
            {
                key = GetValue($"{providerName.ToLowerInvariant()}_api_key");
            }

            return key;
        }

        /// <summary>
        /// Gets a model-specific API key
        /// </summary>
        public string GetModelApiKey(string providerName, string modelName)
        {
            if (string.IsNullOrEmpty(providerName) || string.IsNullOrEmpty(modelName))
                return null;

            // Normalize model name by replacing slashes with underscores
            var normalizedModelName = modelName.Replace('/', '_').ToUpperInvariant();

            // Try the standard format
            return GetValue($"{API_KEY_PREFIX}{providerName.ToUpperInvariant()}_{normalizedModelName}");
        }

        /// <summary>
        /// Gets all available API keys
        /// </summary>
        public Dictionary<string, string> GetAllApiKeys()
        {
            EnsureInitialized();

            var result = new Dictionary<string, string>();

            // Look for AI_KEY_ pattern keys
            foreach (var key in _envCache.Keys)
            {
                if (key.StartsWith(API_KEY_PREFIX, StringComparison.OrdinalIgnoreCase))
                {
                    var value = _envCache[key];
                    if (!string.IsNullOrEmpty(value))
                    {
                        result[key] = value;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets all available model configurations
        /// </summary>
        public Dictionary<string, string> GetModelConfigurations()
        {
            EnsureInitialized();

            var result = new Dictionary<string, string>();

            // Look for AI_MODEL_ pattern keys
            foreach (var key in _envCache.Keys)
            {
                if (key.StartsWith(MODEL_KEY_PREFIX, StringComparison.OrdinalIgnoreCase))
                {
                    var value = _envCache[key];
                    if (!string.IsNullOrEmpty(value))
                    {
                        result[key] = value;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets all environment variables
        /// </summary>
        public Dictionary<string, string> GetAllVariables()
        {
            EnsureInitialized();
            return new Dictionary<string, string>(_envCache, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets all environment variables - alias method
        /// </summary>
        public Dictionary<string, string> GetAllEnvironmentVariables()
        {
            // Delegate to existing method
            return GetAllVariables();
        }

        /// <summary>
        /// Gets all environment variables with the given prefix
        /// </summary>
        public Dictionary<string, string> GetVariablesWithPrefix(string prefix)
        {
            EnsureInitialized();

            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var pair in _envCache)
            {
                if (pair.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    result[pair.Key] = pair.Value;
                }
            }

            return result;
        }

        /// <summary>
        /// Gets a specific environment variable
        /// </summary>
        public string GetEnvironmentVariable(string name)
        {
            EnsureInitialized();

            if (_envCache.TryGetValue(name, out string value))
            {
                return value;
            }

            // Fall back to direct environment check
            return Environment.GetEnvironmentVariable(name);
        }

        /// <summary>
        /// Tries to get an environment variable, returns success status
        /// </summary>
        public bool TryGetEnvironmentVariable(string name, out string value)
        {
            EnsureInitialized();

            if (_envCache.TryGetValue(name, out value))
            {
                return true;
            }

            value = Environment.GetEnvironmentVariable(name);
            return value != null;
        }

        /// <summary>
        /// Loads environment variables
        /// </summary>
        public void LoadEnvironmentVariables()
        {
            InitializeAsync().Wait();
        }

        /// <summary>
        /// Loads environment variables from a specific file
        /// </summary>
        private bool LoadEnvFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    Debug.WriteLine($"Loading environment variables from {filePath}");

                    // Use the simplest API for DotNetEnv to avoid version compatibility issues
                    try
                    {
                        // Load environment variables from file
                        DotNetEnv.Env.Load(filePath);

                        // Parse the .env file manually for more control
                        if (File.Exists(filePath))
                        {
                            string[] lines = File.ReadAllLines(filePath);
                            foreach (var line in lines)
                            {
                                // Skip comments and empty lines
                                string trimmedLine = line.Trim();
                                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#") || trimmedLine.StartsWith("//"))
                                    continue;

                                // Parse key=value format
                                int equalsIndex = trimmedLine.IndexOf('=');
                                if (equalsIndex > 0)
                                {
                                    string key = trimmedLine.Substring(0, equalsIndex).Trim();
                                    string value = trimmedLine.Substring(equalsIndex + 1).Trim();

                                    // Remove quotes if present
                                    if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length >= 2)
                                        value = value.Substring(1, value.Length - 2);

                                    if (!string.IsNullOrEmpty(key))
                                    {
                                        try
                                        {
                                            // Set environment variable (this will overwrite existing)
                                            Environment.SetEnvironmentVariable(key, value);
                                        }
                                        catch (Exception ex)
                                        {
                                            Debug.WriteLine($"Could not set environment variable {key}: {ex.Message}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error using DotNetEnv: {ex.Message}");
                        // Continue anyway as we've already tried manual parsing above
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading env file {filePath}: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Attempts to load .env from fallback locations
        /// </summary>
        private void LoadFromFallbackLocations()
        {
            string[] fallbackPaths = {
                Path.Combine(AppContext.BaseDirectory, ENV_FILE),
                Path.Combine(Directory.GetCurrentDirectory(), ENV_FILE),
                Path.Combine(AppContext.BaseDirectory, "Assets", ENV_FILE),
                Path.Combine(AppContext.BaseDirectory, "Resources", ENV_FILE)
            };

            foreach (var path in fallbackPaths)
            {
                if (LoadEnvFile(path))
                {
                    Debug.WriteLine($"Loaded environment from fallback: {path}");
                    return;
                }
            }

            Debug.WriteLine("Could not find any .env file in standard locations");
        }

        /// <summary>
        /// Gets the path to an env file
        /// </summary>
        private string GetEnvFilePath(string filename)
        {
            // For Android
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                // Android needs special handling for assets
                if (filename == ENV_FILE)
                    return "Assets/.env";
                else if (filename == LOCAL_ENV_FILE)
                    return "Assets/Local/.env";
            }

            // For iOS
            if (DeviceInfo.Platform == DevicePlatform.iOS)
            {
                if (filename == ENV_FILE)
                    return "Resources/.env";
                else if (filename == LOCAL_ENV_FILE)
                    return "Resources/Local/.env";
            }

            // For Windows/macOS, we need to check relative paths from app directory
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // For Visual Studio debug, look up several directories
            if (Directory.Exists(Path.Combine(baseDir, "..", "..", "..")))
            {
                var projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));
                var potentialPath = Path.Combine(projectRoot, filename);
                if (File.Exists(potentialPath))
                    return potentialPath;
            }

            // Default to current directory
            return Path.Combine(Directory.GetCurrentDirectory(), filename);
        }

        /// <summary>
        /// Counts how many API keys we have in environment variables
        /// </summary>
        private int CountEnvironmentKeys()
        {
            const string prefix = "AI_KEY_";
            int count = 0;

            foreach (var key in _envCache.Keys)
            {
                if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    var value = _envCache[key];
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Counts how many model configurations we have in environment variables
        /// </summary>
        private int CountEnvironmentModels()
        {
            const string prefix = "AI_MODEL_";
            int count = 0;

            foreach (var key in _envCache.Keys)
            {
                if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    var value = _envCache[key];
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Ensures the service is initialized
        /// </summary>
        private void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                InitializeAsync().Wait();
            }
        }
    }
}
