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
    /// Low-level storage provider for API keys from various sources
    /// </summary>
    public class ApiKeyStorageProvider : IApiKeyStorageProvider
    {
        private readonly Dictionary<string, string> _storageCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private const string ENV_FILE = ".env";
        private const string SECURE_STORAGE_PREFIX = "nexuschat_apikey_";
        private readonly SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);
        private bool _initialized = false;
        
        /// <summary>
        /// Initializes the storage provider - single point for environment loading
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
                    
                Debug.WriteLine("ApiKeyStorageProvider: Initializing...");
                
                // Single point for environment variable loading
                LoadEnvironmentVariables();
                
                // Load secure storage
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await LoadSecureStorageKeysAsync();
                        Debug.WriteLine("ApiKeyStorageProvider: Secure storage loaded in background");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"ApiKeyStorageProvider: Background secure storage error: {ex.Message}");
                    }
                });
                
                _initialized = true;
                Debug.WriteLine($"ApiKeyStorageProvider: Initialized with {_storageCache.Count} cached values");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApiKeyStorageProvider: Error during initialization: {ex.Message}");
                _initialized = true; // Prevent retry loops
            }
            finally
            {
                _initLock.Release();
            }
        }
        
        /// <summary>
        /// Gets a stored value by exact key name
        /// </summary>
        public async Task<string> GetStoredValueAsync(string exactKeyName)
        {
            if (string.IsNullOrEmpty(exactKeyName))
                return null;
                
            await EnsureInitializedAsync();
            
            // Check memory cache first
            if (_storageCache.TryGetValue(exactKeyName, out string cachedValue))
                return cachedValue;
                
            // Try direct environment lookup
            string envValue = Environment.GetEnvironmentVariable(exactKeyName);
            if (!string.IsNullOrEmpty(envValue))
            {
                _storageCache[exactKeyName] = envValue;
                return envValue;
            }
            
            // Try secure storage as last resort
            try
            {
                string secureValue = await SecureStorage.GetAsync(GetSecureStorageKey(exactKeyName));
                if (!string.IsNullOrEmpty(secureValue))
                {
                    _storageCache[exactKeyName] = secureValue;
                    return secureValue;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApiKeyStorageProvider: Error accessing secure storage for {exactKeyName}: {ex.Message}");
            }
            
            return null;
        }
        
        /// <summary>
        /// Gets all stored values with a specific prefix
        /// </summary>
        public async Task<Dictionary<string, string>> GetAllStoredValuesWithPrefixAsync(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
                return new Dictionary<string, string>();
                
            await EnsureInitializedAsync();
            
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            // Get from cache
            foreach (var pair in _storageCache)
            {
                if (pair.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    result[pair.Key] = pair.Value;
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Saves a value to storage
        /// </summary>
        public async Task<bool> SaveStoredValueAsync(string exactKeyName, string value)
        {
            if (string.IsNullOrEmpty(exactKeyName) || string.IsNullOrEmpty(value))
                return false;
                
            await EnsureInitializedAsync();
            
            try
            {
                // Store in memory cache
                _storageCache[exactKeyName] = value;
                
                // Store in secure storage
                await SecureStorage.SetAsync(GetSecureStorageKey(exactKeyName), value);
                
                Debug.WriteLine($"ApiKeyStorageProvider: Saved value for {exactKeyName}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApiKeyStorageProvider: Error saving value for {exactKeyName}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Deletes a value from storage
        /// </summary>
        public async Task<bool> DeleteStoredValueAsync(string exactKeyName)
        {
            if (string.IsNullOrEmpty(exactKeyName))
                return false;
                
            await EnsureInitializedAsync();
            
            try
            {
                // Remove from memory cache
                _storageCache.Remove(exactKeyName);
                
                // Remove from secure storage
                SecureStorage.Remove(GetSecureStorageKey(exactKeyName));
                
                Debug.WriteLine($"ApiKeyStorageProvider: Deleted value for {exactKeyName}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApiKeyStorageProvider: Error deleting value for {exactKeyName}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        ///  environment loading
        /// </summary>
        private void LoadEnvironmentVariables()
        {
            try
            {
                Debug.WriteLine("ApiKeyStorageProvider: Loading environment variables ");
                
                // Try to load .env file from current directory first (most common case)
                if (File.Exists(ENV_FILE))
                {
                    DotNetEnv.Env.Load(ENV_FILE);
                    Debug.WriteLine("ApiKeyStorageProvider: Loaded .env file");
                }
                else
                {
                    // If not found, try to load from the application directory
                    string appDir = AppContext.BaseDirectory;
                    string envFilePath = Path.Combine(appDir, ENV_FILE);
                    if (File.Exists(envFilePath))
                    {
                        DotNetEnv.Env.Load(envFilePath);
                        Debug.WriteLine($"ApiKeyStorageProvider: Loaded .env file from {envFilePath}");
                    }
                    else
                    {
                        Debug.WriteLine("ApiKeyStorageProvider: No .env file found");
                    }
                }
                // Cache all environment variables that look like API keys
                var envVars = Environment.GetEnvironmentVariables();
                foreach (string key in envVars.Keys)
                {
                    if (key != null && (key.StartsWith("AI_KEY_", StringComparison.OrdinalIgnoreCase) ||
                                       key.Contains("API_KEY", StringComparison.OrdinalIgnoreCase) ||
                                       key.Contains("SECRET", StringComparison.OrdinalIgnoreCase)))
                    {
                        string value = envVars[key]?.ToString();
                        if (!string.IsNullOrEmpty(value))
                        {
                            _storageCache[key] = value;
                        }
                    }
                }
                
                Debug.WriteLine($"ApiKeyStorageProvider: Optimized loading found {_storageCache.Count} relevant variables");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApiKeyStorageProvider: Error loading environment variables: {ex.Message}");
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
                    Debug.WriteLine($"ApiKeyStorageProvider: Loaded .env file from {path}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApiKeyStorageProvider: Error loading .env file {path}: {ex.Message}");
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
                // Note: SecureStorage doesn't support enumeration
                // Keys are loaded on-demand when requested
                Debug.WriteLine("ApiKeyStorageProvider: SecureStorage keys will be loaded on-demand");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApiKeyStorageProvider: Error preparing secure storage access: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Ensures the provider is initialized
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
        private string GetSecureStorageKey(string keyName)
        {
            return $"{SECURE_STORAGE_PREFIX}{keyName.ToLowerInvariant()}";
        }
    }
}
