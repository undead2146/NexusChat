using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DotNetEnv;
using NexusChat.Services.Interfaces;
using Microsoft.Maui.Storage;

namespace NexusChat.Services.ApiKeyManagement
{
    /// <summary>
    /// Provides API keys from environment variables, .env files, and secure storage
    /// </summary>
    public class ApiKeyProvider : IApiKeyProvider
    {
        private readonly Dictionary<string, string> _apiKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private const string ENV_FILE = ".env";
        private const string API_KEY_PREFIX = "AI_KEY_";
        private const string SECURE_STORAGE_PREFIX = "nexuschat_apikey_";
        private readonly SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);
        private bool _initialized = false;
        
        /// <summary>
        /// Initializes the API key provider
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_initialized)
                return;
                
            await _initLock.WaitAsync();
            try
            {
                if (_initialized)
                    return;
                    
                // Load environment variables
                LoadEnvironmentVariables();
                
                // Load keys from secure storage
                await LoadSecureStorageKeysAsync();
                
                _initialized = true;
                Debug.WriteLine($"ApiKeyProvider: Initialized with {_apiKeys.Count} API keys");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApiKeyProvider: Error during initialization: {ex.Message}");
            }
            finally
            {
                _initLock.Release();
            }
        }
        
        /// <summary>
        /// Gets an API key by name
        /// </summary>
        public string GetApiKey(string keyName)
        {
            EnsureInitialized();
            
            if (string.IsNullOrEmpty(keyName))
                return null;
                
            // First check in-memory cache
            if (_apiKeys.TryGetValue(keyName, out string value))
                return value;
                
            // Try API_KEY_ prefixed version if not already
            if (!keyName.StartsWith(API_KEY_PREFIX, StringComparison.OrdinalIgnoreCase))
            {
                string prefixedKey = $"{API_KEY_PREFIX}{keyName.ToUpperInvariant()}";
                if (_apiKeys.TryGetValue(prefixedKey, out string prefixedValue))
                {
                    // Cache it under the requested name too
                    _apiKeys[keyName] = prefixedValue;
                    return prefixedValue;
                }
            }
            
            // Try direct environment lookup as last resort
            string envValue = Environment.GetEnvironmentVariable(keyName) ?? 
                              Environment.GetEnvironmentVariable($"{API_KEY_PREFIX}{keyName.ToUpperInvariant()}");
            
            if (!string.IsNullOrEmpty(envValue))
            {
                // Cache for future lookups
                _apiKeys[keyName] = envValue;
                return envValue;
            }
            
            return null;
        }
        
        /// <summary>
        /// Gets all available API keys
        /// </summary>
        public Dictionary<string, string> GetAllApiKeys() 
        {
            EnsureInitialized();
            return new Dictionary<string, string>(_apiKeys, StringComparer.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// Gets API keys with a specific prefix
        /// </summary>
        public Dictionary<string, string> GetApiKeysWithPrefix(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
                return new Dictionary<string, string>();
                
            EnsureInitialized();
            
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in _apiKeys)
            {
                if (pair.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(pair.Key, pair.Value);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Saves an API key
        /// </summary>
        public async Task<bool> SaveApiKeyAsync(string keyName, string value)
        {
            if (string.IsNullOrEmpty(keyName) || string.IsNullOrEmpty(value))
                return false;
                
            await EnsureInitializedAsync();
            
            try
            {
                // Store in memory cache
                _apiKeys[keyName] = value;
                
                // Store in secure storage
                await SecureStorage.SetAsync(GetSecureKey(keyName), value);
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApiKeyProvider: Error saving API key: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Gets an API key asynchronously
        /// </summary>
        public async Task<string> GetApiKeyAsync(string keyName)
        {
            await EnsureInitializedAsync();
            
            // First try memory cache via synchronous method
            string key = GetApiKey(keyName);
            if (!string.IsNullOrEmpty(key))
                return key;
                
            // Try secure storage as last resort
            try
            {
                string secureKey = await SecureStorage.GetAsync(GetSecureKey(keyName));
                if (!string.IsNullOrEmpty(secureKey))
                {
                    // Cache it for future use
                    _apiKeys[keyName] = secureKey;
                    return secureKey;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApiKeyProvider: Error accessing secure storage: {ex.Message}");
            }
            
            return null;
        }
        
        /// <summary>
        /// Deletes an API key
        /// </summary>
        public async Task<bool> DeleteApiKeyAsync(string keyName)
        {
            if (string.IsNullOrEmpty(keyName))
                return false;
                
            await EnsureInitializedAsync();
            
            try
            {
                // Remove from memory cache
                _apiKeys.Remove(keyName);
                
                // Remove from secure storage
                SecureStorage.Remove(GetSecureKey(keyName));
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApiKeyProvider: Error deleting API key: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Loads environment variables from .env files
        /// </summary>
        private void LoadEnvironmentVariables()
        {
            try
            {
                // First try loading .env file
                LoadEnvFromFile(ENV_FILE);
                
                // Try project directory and parent directories
                string baseDir = AppContext.BaseDirectory;
                for (int i = 0; i < 5; i++) // Look up to 5 directories up
                {
                    string envPath = Path.Combine(baseDir, ENV_FILE);
                    if (LoadEnvFromFile(envPath))
                        break;
                        
                    baseDir = Path.Combine(baseDir, "..");
                }
                
                // Now extract AI_KEY_ environment variables
                var envVars = Environment.GetEnvironmentVariables();
                foreach (string key in envVars.Keys)
                {
                    if (key != null && key.StartsWith(API_KEY_PREFIX, StringComparison.OrdinalIgnoreCase))
                    {
                        string value = envVars[key]?.ToString();
                        if (!string.IsNullOrEmpty(value))
                        {
                            _apiKeys[key] = value;
                            
                            // Also store without prefix for easier lookup
                            string unprefixedKey = key.Substring(API_KEY_PREFIX.Length).ToLowerInvariant();
                            _apiKeys[unprefixedKey] = value;
                        }
                    }
                }
                
                Debug.WriteLine($"ApiKeyProvider: Loaded {_apiKeys.Count} API keys from environment variables");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApiKeyProvider: Error loading environment variables: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Loads a .env file
        /// </summary>
        private bool LoadEnvFromFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    DotNetEnv.Env.Load(path);
                    Debug.WriteLine($"ApiKeyProvider: Loaded .env file from {path}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApiKeyProvider: Error loading .env file {path}: {ex.Message}");
            }
            
            return false;
        }
        
        /// <summary>
        /// Loads keys from secure storage
        /// </summary>
        private async Task LoadSecureStorageKeysAsync()
        {
            try
            {
                // Note: There's no direct way to enumerate all keys in SecureStorage
                // This would require maintaining a registry of stored keys elsewhere
                // For now, we just rely on the cache being populated on-demand
                Debug.WriteLine("ApiKeyProvider: Note - SecureStorage keys are loaded on-demand");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApiKeyProvider: Error accessing secure storage: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Ensures the provider is initialized
        /// </summary>
        private void EnsureInitialized()
        {
            if (!_initialized)
            {
                InitializeAsync().Wait();
            }
        }
        
        /// <summary>
        /// Ensures the provider is initialized asynchronously
        /// </summary>
        private async Task EnsureInitializedAsync()
        {
            if (!_initialized)
            {
                await InitializeAsync();
            }
        }
        
        /// <summary>
        /// Gets the secure storage key for a given key name
        /// </summary>
        private string GetSecureKey(string keyName)
        {
            return $"{SECURE_STORAGE_PREFIX}{keyName.ToLowerInvariant()}";
        }
    }
}
