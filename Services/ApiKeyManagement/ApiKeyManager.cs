using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Services.Interfaces;
using DotNetEnv;

namespace NexusChat.Services.ApiKeyManagement
{
    /// <summary>
    /// Manager for API keys used by AI providers
    /// </summary>
    public class ApiKeyManager : IApiKeyManager
    {
        private readonly IApiKeyProvider _apiKeyProvider;
        private Dictionary<string, string> _cachedApiKeys;
        private readonly SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);
        private bool _isInitialized = false;
        private int _initializeAttempts = 0;
        private const int MAX_INIT_ATTEMPTS = 3;

        /// <summary>
        /// Creates a new instance of ApiKeyManager
        /// </summary>
        /// <param name="apiKeyProvider">API key provider for storage operations</param>
        public ApiKeyManager(IApiKeyProvider apiKeyProvider)
        {
            _apiKeyProvider = apiKeyProvider ?? throw new ArgumentNullException(nameof(apiKeyProvider));
            _cachedApiKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Event triggered when an API key changes
        /// </summary>
        public event EventHandler<string> ApiKeyChanged;

        /// <summary>
        /// Initializes the API key manager with improved error handling
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized)
                return;
                
            await _initLock.WaitAsync();
            
            try
            {
                if (_isInitialized)
                    return;
                
                _initializeAttempts++;
                Debug.WriteLine($"ApiKeyManager: Initializing... (attempt {_initializeAttempts})");
                
                // Load environment variables if available
                Env.Load();
                
                // Initialize the provider
                await _apiKeyProvider.InitializeAsync();
                
                // Cache common keys for performance
                await CacheCommonKeysAsync();
                
                _isInitialized = true;
                Debug.WriteLine("ApiKeyManager initialized");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing ApiKeyManager: {ex.Message}");
                Debug.WriteLine(ex.StackTrace);
                
                // Try to initialize again if we haven't exceeded max attempts
                if (_initializeAttempts < MAX_INIT_ATTEMPTS)
                {
                    _isInitialized = false;
                }
                else
                {
                    // Set initialized to true but with basic functionality
                    Debug.WriteLine("ApiKeyManager: Max initialization attempts reached, proceeding with limited functionality");
                    _isInitialized = true;
                }
            }
            finally
            {
                _initLock.Release();
            }
        }

        /// <summary>
        /// Gets an API key for a provider
        /// </summary>
        public async Task<string> GetApiKeyForProviderAsync(string providerName)
        {
            if (string.IsNullOrEmpty(providerName))
                return null;
                
            try
            {
                // Ensure initialization
                await EnsureInitializedAsync();
                    
                // Get the environment variable name for this provider
                string keyName = GetEnvironmentVariableName(providerName);
                
                // Check cache first
                if (_cachedApiKeys.TryGetValue(keyName, out string cachedKey))
                {
                    return cachedKey;
                }
                
                // Try environment variables
                string apiKey = Environment.GetEnvironmentVariable(keyName);
                
                // If not in environment, try API key provider
                if (string.IsNullOrEmpty(apiKey))
                {
                    apiKey = await _apiKeyProvider.GetApiKeyAsync(keyName);
                }
                
                // Cache if found
                if (!string.IsNullOrEmpty(apiKey))
                {
                    _cachedApiKeys[keyName] = apiKey;
                }
                
                return apiKey;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting API key for {providerName}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets the API key for a specific model
        /// </summary>
        public async Task<string> GetModelApiKeyAsync(string providerName, string modelName)
        {
            if (string.IsNullOrEmpty(providerName))
                return null;
                
            try
            {
                // Ensure initialized
                await EnsureInitializedAsync();
                    
                // First try model-specific key
                string modelKeyName = $"AI_KEY_{providerName.ToUpperInvariant()}_{modelName.Replace("-", "_").ToUpperInvariant()}";
                
                // Try cache first
                if (_cachedApiKeys.TryGetValue(modelKeyName, out string cachedModelKey))
                {
                    return cachedModelKey;
                }
                
                // Try environment variables
                string apiKey = Environment.GetEnvironmentVariable(modelKeyName);
                
                // If not in environment, try API key provider
                if (string.IsNullOrEmpty(apiKey))
                {
                    apiKey = await _apiKeyProvider.GetApiKeyAsync(modelKeyName);
                }
                
                // If still not found, fall back to provider-level key
                if (string.IsNullOrEmpty(apiKey))
                {
                    return await GetApiKeyForProviderAsync(providerName);
                }
                
                // Cache if found
                if (!string.IsNullOrEmpty(apiKey))
                {
                    _cachedApiKeys[modelKeyName] = apiKey;
                }
                
                return apiKey;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting API key for {providerName}/{modelName}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Saves an API key
        /// </summary>
        public async Task<bool> SaveApiKeyAsync(string keyName, string apiKey)
        {
            try
            {
                // Ensure initialized
                await EnsureInitializedAsync();
                    
                if (string.IsNullOrEmpty(keyName) || string.IsNullOrEmpty(apiKey))
                    return false;
                
                // Validate key format
                if (!IsValidApiKeyFormat(apiKey))
                {
                    Debug.WriteLine($"Invalid API key format for {keyName}");
                    return false;
                }
                
                // Save using provider
                bool success = await _apiKeyProvider.SaveApiKeyAsync(keyName, apiKey);
                
                // Update cache if successful
                if (success)
                {
                    _cachedApiKeys[keyName] = apiKey;
                    
                    // Notify listeners that an API key has changed
                    ApiKeyChanged?.Invoke(this, keyName);
                }
                
                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving API key {keyName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Deletes an API key
        /// </summary>
        public async Task<bool> DeleteApiKeyAsync(string keyName)
        {
            try
            {
                // Ensure initialized
                await EnsureInitializedAsync();
                    
                if (string.IsNullOrEmpty(keyName))
                    return false;
                
                // Delete using provider
                bool success = await _apiKeyProvider.DeleteApiKeyAsync(keyName);
                
                // Remove from cache if successful
                if (success && _cachedApiKeys.ContainsKey(keyName))
                {
                    _cachedApiKeys.Remove(keyName);
                }
                
                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting API key {keyName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Validates API key format
        /// </summary>
        public bool IsValidApiKeyFormat(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                return false;
                
            // Check minimum length
            if (apiKey.Length < 8)
                return false;
                
            // Check common API key patterns
            bool isOpenAiFormat = Regex.IsMatch(apiKey, @"^sk-[A-Za-z0-9]{24,}$");  // OpenAI pattern
            bool isGroqFormat = Regex.IsMatch(apiKey, @"^gsk_[A-Za-z0-9]{16,}$");   // Groq pattern
            bool isOpenRouterFormat = Regex.IsMatch(apiKey, @"^sk-or-[A-Za-z0-9-]{20,}$"); // OpenRouter pattern
            bool isGenericFormat = Regex.IsMatch(apiKey, @"^[A-Za-z0-9_\-]{16,}$"); // Generic API key pattern
            
            return isOpenAiFormat || isGroqFormat || isOpenRouterFormat || isGenericFormat;
        }

        /// <summary>
        /// Gets the appropriate environment variable name for a provider
        /// </summary>
        public string GetEnvironmentVariableName(string providerName)
        {
            if (string.IsNullOrEmpty(providerName))
                return string.Empty;
                
            return $"AI_KEY_{providerName.ToUpperInvariant()}";
        }
        
        /// <summary>
        /// Caches common API keys for performance
        /// </summary>
        private async Task CacheCommonKeysAsync()
        {
            string[] commonProviders = { "GROQ", "OPENROUTER", "OPENAI", "ANTHROPIC", "AZURE" };
            
            foreach (string provider in commonProviders)
            {
                string keyName = $"AI_KEY_{provider}";
                
                // Try environment first
                string apiKey = Environment.GetEnvironmentVariable(keyName);
                
                // If not in environment, try provider
                if (string.IsNullOrEmpty(apiKey))
                {
                    apiKey = await _apiKeyProvider.GetApiKeyAsync(keyName);
                }
                
                // Cache if found
                if (!string.IsNullOrEmpty(apiKey))
                {
                    _cachedApiKeys[keyName] = apiKey;
                }
            }
        }

        /// <summary>
        /// Ensures the manager is initialized
        /// </summary>
        private async Task EnsureInitializedAsync()
        {
            if (!_isInitialized)
            {
                await InitializeAsync();
            }
        }

        /// <summary>
        /// Directly gets an API key by key name
        /// </summary>
        public async Task<string> GetApiKeyAsync(string keyName)
        {
            try
            {
                await EnsureInitializedAsync();
                
                if (string.IsNullOrEmpty(keyName))
                    return null;
                    
                // Check cache first
                if (_cachedApiKeys.TryGetValue(keyName, out string cachedKey))
                {
                    return cachedKey;
                }
                
                // Try environment variables
                string envKey = Environment.GetEnvironmentVariable(keyName);
                if (!string.IsNullOrEmpty(envKey))
                {
                    _cachedApiKeys[keyName] = envKey;
                    return envKey;
                }
                
                // Try API key provider
                string apiKey = await _apiKeyProvider.GetApiKeyAsync(keyName);
                
                // Cache if found
                if (!string.IsNullOrEmpty(apiKey))
                {
                    _cachedApiKeys[keyName] = apiKey;
                }
                
                return apiKey;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting API key {keyName}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets the provider-level API key
        /// </summary>
        public async Task<string> GetProviderApiKeyAsync(string providerName)
        {
            return await GetApiKeyForProviderAsync(providerName);
        }
    }
}
