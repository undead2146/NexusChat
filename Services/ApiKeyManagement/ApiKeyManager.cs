using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Services.Interfaces;

namespace NexusChat.Services.ApiKeyManagement
{
    /// <summary>
    /// High-level manager for API keys with business logic and provider/model resolution
    /// </summary>
    public class ApiKeyManager : IApiKeyManager
    {
        private readonly IApiKeyStorageProvider _storageProvider;
        private readonly Dictionary<string, string> _resolvedKeyCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, bool> _providerAvailabilityCache = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DateTime> _cacheTimestamps = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        private readonly SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _cacheLock = new SemaphoreSlim(1, 1);
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(10); // Cache API key availability for 10 minutes
        private bool _isInitialized = false;
        private DateTime _lastBulkCacheRefresh = DateTime.MinValue;
        private readonly TimeSpan _bulkCacheRefreshInterval = TimeSpan.FromMinutes(5); // Refresh all cached data every 5 minutes

        /// <summary>
        /// Creates a new instance of ApiKeyManager
        /// </summary>
        /// <param name="storageProvider">Storage provider for raw key-value operations</param>
        public ApiKeyManager(IApiKeyStorageProvider storageProvider)
        {
            _storageProvider = storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));
        }

        /// <summary>
        /// Event triggered when an API key changes
        /// </summary>
        public event EventHandler<string> ApiKeyChanged;

        /// <summary>
        /// Event fired when an API key is successfully saved
        /// </summary>
        public event EventHandler<string> ApiKeySaved;

        /// <summary>
        /// Initializes the API key manager with pre-caching 
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
                
                Debug.WriteLine("ApiKeyManager: Minimal initialization");
                
                // Just initialize storage provider - no pre-caching
                await _storageProvider.InitializeAsync();
                
                _isInitialized = true;
                Debug.WriteLine("ApiKeyManager: Minimal initialization completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApiKeyManager: Error during minimal initialization: {ex.Message}");
                _isInitialized = true;
            }
            finally
            {
                _initLock.Release();
            }
        }

        /// <summary>
        /// Remove expensive pre-caching methods
        /// </summary>
        private async Task PreCacheEssentialProvidersOnlyAsync()
        {
            // Remove this expensive operation
            Debug.WriteLine("ApiKeyManager: Skipping pre-caching for performance");
        }

        private async Task PreCacheRemainingProvidersAsync()
        {
            // Remove this expensive operation
            Debug.WriteLine("ApiKeyManager: Skipping remaining provider caching for performance");
        }

        private async Task PreCacheAllProviderAvailabilityAsync()
        {
            // Remove this expensive operation
            Debug.WriteLine("ApiKeyManager: Skipping availability pre-caching for performance");
        }

        /// <summary>
        /// Internal API key resolution without caching side effects
        /// </summary>
        private async Task<string> ResolveApiKeyInternalAsync(string providerName, string modelName = null)
        {
            if (string.IsNullOrEmpty(providerName))
                return null;
                
            try
            {
                string resolvedKey = null;
                
                // Try model-specific key first if model name provided
                if (!string.IsNullOrEmpty(modelName))
                {
                    string modelSpecificKeyName = GetModelSpecificKeyName(providerName, modelName);
                    resolvedKey = await _storageProvider.GetStoredValueAsync(modelSpecificKeyName);
                    Debug.WriteLine($"ApiKeyManager: Model-specific key for {providerName}/{modelName}: {(string.IsNullOrEmpty(resolvedKey) ? "NOT FOUND" : "FOUND")}");
                }
                
                // Fall back to provider-level key if model-specific not found
                if (string.IsNullOrEmpty(resolvedKey))
                {
                    string providerKeyName = GetProviderLevelKeyName(providerName);
                    resolvedKey = await _storageProvider.GetStoredValueAsync(providerKeyName);
                    Debug.WriteLine($"ApiKeyManager: Provider-level key for {providerName} (key: {providerKeyName}): {(string.IsNullOrEmpty(resolvedKey) ? "NOT FOUND" : "FOUND")}");
                }
                
                return resolvedKey;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApiKeyManager: Error in ResolveApiKeyInternalAsync for {providerName}/{modelName}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Checks if cache needs refresh and updates if necessary
        /// </summary>
        private async Task RefreshCacheIfNeededAsync()
        {
            var currentTime = DateTime.UtcNow;
            
            // Check if bulk refresh is needed
            if (currentTime - _lastBulkCacheRefresh > _bulkCacheRefreshInterval)
            {
                Debug.WriteLine("ApiKeyManager: Bulk cache refresh needed");
                await PreCacheAllProviderAvailabilityAsync();
                return;
            }
            
            // Check individual provider cache entries
            await _cacheLock.WaitAsync();
            try
            {
                var expiredProviders = _cacheTimestamps
                    .Where(kvp => currentTime - kvp.Value > _cacheExpiry)
                    .Select(kvp => kvp.Key)
                    .ToList();
                
                if (expiredProviders.Any())
                {
                    Debug.WriteLine($"ApiKeyManager: Refreshing cache for {expiredProviders.Count} expired providers");
                    
                    foreach (var provider in expiredProviders)
                    {
                        try
                        {
                            string apiKey = await ResolveApiKeyInternalAsync(provider);
                            bool hasValidKey = !string.IsNullOrEmpty(apiKey) && IsValidApiKeyFormat(apiKey);
                            
                            _providerAvailabilityCache[provider] = hasValidKey;
                            _cacheTimestamps[provider] = currentTime;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"ApiKeyManager: Error refreshing cache for {provider}: {ex.Message}");
                            _providerAvailabilityCache[provider] = false;
                            _cacheTimestamps[provider] = currentTime;
                        }
                    }
                }
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        /// <summary>
        /// Fast synchronous API key availability check using cache
        /// </summary>
        public bool HasActiveApiKeySync(string providerName)
        {
            if (string.IsNullOrEmpty(providerName))
                return false;
                
            try
            {
                // Always check cache first without any async operations
                if (_providerAvailabilityCache.TryGetValue(providerName, out bool cachedResult))
                {
                    if (_cacheTimestamps.TryGetValue(providerName, out DateTime timestamp))
                    {
                        // Use longer cache validity for sync calls to avoid background operations
                        if (DateTime.UtcNow - timestamp <= TimeSpan.FromMinutes(30)) // Increased from 10 minutes
                        {
                            return cachedResult;
                        }
                    }
                }
                
                // For sync calls, if not in cache or expired, do a direct synchronous check
                // without triggering background refresh to avoid blocking
                string apiKey = GetCachedApiKeyDirectly(providerName);
                bool hasValidKey = !string.IsNullOrEmpty(apiKey) && IsValidApiKeyFormat(apiKey);
                
                // Update cache synchronously
                _providerAvailabilityCache[providerName] = hasValidKey;
                _cacheTimestamps[providerName] = DateTime.UtcNow;
                
                return hasValidKey;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApiKeyManager: Error in HasActiveApiKeySync for {providerName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Direct synchronous API key retrieval from cache/environment without async operations
        /// </summary>
        private string GetCachedApiKeyDirectly(string providerName)
        {
            try
            {
                string keyName = GetProviderLevelKeyName(providerName);
                
                // Check resolved cache first
                if (_resolvedKeyCache.TryGetValue(providerName, out string cachedKey))
                {
                    return cachedKey;
                }
                
                // Direct environment lookup without async operations
                string envValue = Environment.GetEnvironmentVariable(keyName);
                if (!string.IsNullOrEmpty(envValue))
                {
                    _resolvedKeyCache[providerName] = envValue;
                    return envValue;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetCachedApiKeyDirectly error for {providerName}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Optimized async method that uses cache when possible
        /// </summary>
        public async Task<bool> HasActiveApiKeyAsync(string providerName)
        {
            if (string.IsNullOrEmpty(providerName))
            {
                Debug.WriteLine("ApiKeyManager: HasActiveApiKeyAsync called with empty provider name");
                return false;
            }
                
            await EnsureInitializedAsync();
            
            try
            {
                Debug.WriteLine($"ApiKeyManager: Checking API key for provider: {providerName}");
                
                if (_providerAvailabilityCache.TryGetValue(providerName, out bool cachedResult))
                {
                    if (_cacheTimestamps.TryGetValue(providerName, out DateTime timestamp))
                    {
                        if (DateTime.UtcNow - timestamp <= _cacheExpiry)
                        {
                            Debug.WriteLine($"ApiKeyManager: Using cached result for {providerName}: {cachedResult}");
                            return cachedResult;
                        }
                    }
                }
                
                string apiKey = await ResolveApiKeyInternalAsync(providerName);
                bool hasValidKey = !string.IsNullOrEmpty(apiKey) && IsValidApiKeyFormat(apiKey);
                
                Debug.WriteLine($"ApiKeyManager: For provider {providerName} - ApiKey exists: {!string.IsNullOrEmpty(apiKey)}, Valid format: {(string.IsNullOrEmpty(apiKey) ? false : IsValidApiKeyFormat(apiKey))}, Final result: {hasValidKey}");
                
                await _cacheLock.WaitAsync();
                try
                {
                    _providerAvailabilityCache[providerName] = hasValidKey;
                    _cacheTimestamps[providerName] = DateTime.UtcNow;
                }
                finally
                {
                    _cacheLock.Release();
                }
                
                return hasValidKey;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApiKeyManager: Error checking active API key for {providerName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Saves an API key and updates availability cache - STEP 9 OPTIMIZATION
        /// </summary>
        public async Task<bool> SaveProviderApiKeyAsync(string providerName, string apiKey)
        {
            if (string.IsNullOrEmpty(providerName) || string.IsNullOrEmpty(apiKey))
                return false;
                
            await EnsureInitializedAsync();
            
            try
            {
                // Validate key format
                if (!IsValidApiKeyFormat(apiKey))
                {
                    Debug.WriteLine($"ApiKeyManager: Invalid API key format for {providerName}");
                    return false;
                }
                
                string keyName = GetProviderLevelKeyName(providerName);
                bool success = await _storageProvider.SaveStoredValueAsync(keyName, apiKey);
                
                if (success)
                {
                    // Clear resolved cache for this provider
                    ClearProviderFromResolvedCache(providerName);
                    
                    // Update availability cache immediately - STEP 9 OPTIMIZATION
                    await _cacheLock.WaitAsync();
                    try
                    {
                        _providerAvailabilityCache[providerName] = true;
                        _cacheTimestamps[providerName] = DateTime.UtcNow;
                        Debug.WriteLine($"ApiKeyManager: Updated availability cache for {providerName} to true");
                    }
                    finally
                    {
                        _cacheLock.Release();
                    }
                    
                    // Notify listeners
                    ApiKeyChanged?.Invoke(this, providerName);
                    Debug.WriteLine($"ApiKeyManager: Saved provider key for {providerName}");
                    
                    // Fire event to notify listeners
                    ApiKeySaved?.Invoke(this, providerName);
                }
                
                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApiKeyManager: Error saving provider API key for {providerName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Deletes an API key and updates availability cache 
        /// </summary>
        public async Task<bool> DeleteProviderApiKeyAsync(string providerName)
        {
            if (string.IsNullOrEmpty(providerName))
                return false;
                
            await EnsureInitializedAsync();
            
            try
            {
                string keyName = GetProviderLevelKeyName(providerName);
                bool success = await _storageProvider.DeleteStoredValueAsync(keyName);
                
                if (success)
                {
                    // Clear resolved cache for this provider
                    ClearProviderFromResolvedCache(providerName);
                    
                    // Update availability cache immediately 
                    await _cacheLock.WaitAsync();
                    try
                    {
                        _providerAvailabilityCache[providerName] = false;
                        _cacheTimestamps[providerName] = DateTime.UtcNow;
                        Debug.WriteLine($"ApiKeyManager: Updated availability cache for {providerName} to false");
                    }
                    finally
                    {
                        _cacheLock.Release();
                    }
                    
                    Debug.WriteLine($"ApiKeyManager: Deleted provider key for {providerName}");
                }
                
                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApiKeyManager: Error deleting provider API key for {providerName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets all actively available providers
        /// </summary>
        public async Task<List<string>> GetActiveProvidersAsync()
        {
            await EnsureInitializedAsync();
            await RefreshCacheIfNeededAsync();
            
            await _cacheLock.WaitAsync();
            try
            {
                var activeProviders = _providerAvailabilityCache
                    .Where(kvp => kvp.Value)
                    .Select(kvp => kvp.Key)
                    .ToList();
                
                Debug.WriteLine($"ApiKeyManager: Found {activeProviders.Count} active providers: {string.Join(", ", activeProviders)}");
                return activeProviders;
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        /// <summary>
        /// Gets all actively available providers synchronously
        /// </summary>
        public List<string> GetActiveProvidersSync()
        {
            try
            {
                var activeProviders = new List<string>();
                
                foreach (var kvp in _providerAvailabilityCache)
                {
                    if (kvp.Value && _cacheTimestamps.TryGetValue(kvp.Key, out DateTime timestamp))
                    {
                        // Only include if cache entry is still valid
                        if (DateTime.UtcNow - timestamp <= _cacheExpiry)
                        {
                            activeProviders.Add(kvp.Key);
                        }
                    }
                }
                
                return activeProviders;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApiKeyManager: Error in GetActiveProvidersSync: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Forces a refresh of the availability cache - STEP 9 UTILITY
        /// </summary>
        public async Task RefreshAvailabilityCacheAsync()
        {
            try
            {
                Debug.WriteLine("ApiKeyManager: Force refreshing availability cache");
                await PreCacheAllProviderAvailabilityAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApiKeyManager: Error in RefreshAvailabilityCacheAsync: {ex.Message}");
            }
        }

        /// <summary>
        /// Clears the availability cache for a specific provider
        /// </summary>
        private void ClearProviderAvailabilityCache(string providerName)
        {
            try
            {
                _providerAvailabilityCache.Remove(providerName);
                _cacheTimestamps.Remove(providerName);
                Debug.WriteLine($"ApiKeyManager: Cleared availability cache for {providerName}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApiKeyManager: Error clearing availability cache for {providerName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Initializes the API key manager
        /// </summary>
        public async Task LegacyInitializeAsync()
        {
            if (_isInitialized)
                return;
                
            await _initLock.WaitAsync();
            
            try
            {
                if (_isInitialized)
                    return;
                
                Debug.WriteLine("ApiKeyManager: Legacy initializing...");
                
                // Initialize the storage provider (this handles Env.Load())
                await _storageProvider.InitializeAsync();
                
                _isInitialized = true;
                Debug.WriteLine("ApiKeyManager: Legacy initialized");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApiKeyManager: Error during legacy initialization: {ex.Message}");
                _isInitialized = true; // Set to true to prevent infinite retry loops
            }
            finally
            {
                _initLock.Release();
            }
        }

        /// <summary>
        /// Resolves an API key for a provider and optional model with fallback logic
        /// </summary>
        public async Task<string> ResolveApiKeyAsync(string providerName, string modelName = null)
        {
            if (string.IsNullOrEmpty(providerName))
                return null;
                
            await EnsureInitializedAsync();
            
            try
            {
                // Create cache key for resolved result
                string cacheKey = string.IsNullOrEmpty(modelName) ? 
                    providerName : $"{providerName}:{modelName}";
                
                // Check resolved cache first
                if (_resolvedKeyCache.TryGetValue(cacheKey, out string cachedResult))
                {
                    return cachedResult;
                }
                
                string resolvedKey = null;
                
                // Try model-specific key first if model name provided
                if (!string.IsNullOrEmpty(modelName))
                {
                    string modelSpecificKeyName = GetModelSpecificKeyName(providerName, modelName);
                    resolvedKey = await _storageProvider.GetStoredValueAsync(modelSpecificKeyName);
                    
                    if (!string.IsNullOrEmpty(resolvedKey))
                    {
                        Debug.WriteLine($"ApiKeyManager: Found model-specific key for {providerName}/{modelName}");
                    }
                }
                
                // Fall back to provider-level key if model-specific not found
                if (string.IsNullOrEmpty(resolvedKey))
                {
                    string providerKeyName = GetProviderLevelKeyName(providerName);
                    resolvedKey = await _storageProvider.GetStoredValueAsync(providerKeyName);
                    
                    if (!string.IsNullOrEmpty(resolvedKey))
                    {
                        Debug.WriteLine($"ApiKeyManager: Found provider-level key for {providerName}");
                    }
                }
                
                // Cache the resolved result (even if null to avoid repeated lookups)
                _resolvedKeyCache[cacheKey] = resolvedKey;
                
                return resolvedKey;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApiKeyManager: Error resolving API key for {providerName}/{modelName}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets an API key for a specific provider (legacy method)
        /// </summary>
        public async Task<string> GetApiKeyAsync(string providerName)
        {
            return await ResolveApiKeyAsync(providerName);
        }

        /// <summary>
        /// Saves a model-specific API key
        /// </summary>
        public async Task<bool> SaveModelSpecificApiKeyAsync(string providerName, string modelName, string apiKey)
        {
            if (string.IsNullOrEmpty(providerName) || string.IsNullOrEmpty(modelName) || string.IsNullOrEmpty(apiKey))
                return false;
                
            await EnsureInitializedAsync();
            
            try
            {
                // Validate key format
                if (!IsValidApiKeyFormat(apiKey))
                {
                    Debug.WriteLine($"ApiKeyManager: Invalid API key format for {providerName}/{modelName}");
                    return false;
                }
                
                string keyName = GetModelSpecificKeyName(providerName, modelName);
                bool success = await _storageProvider.SaveStoredValueAsync(keyName, apiKey);
                
                if (success)
                {
                    // Clear resolved cache for this provider/model combination
                    string cacheKey = $"{providerName}:{modelName}";
                    _resolvedKeyCache.Remove(cacheKey);
                    
                    // Notify listeners
                    ApiKeyChanged?.Invoke(this, providerName);
                    Debug.WriteLine($"ApiKeyManager: Saved model-specific key for {providerName}/{modelName}");
                }
                
                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApiKeyManager: Error saving model-specific API key for {providerName}/{modelName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Legacy method - use HasActiveApiKeyAsync instead
        /// </summary>
        public async Task<bool> HasApiKeyAsync(string providerName)
        {
            return await HasActiveApiKeyAsync(providerName);
        }

        /// <summary>
        /// Gets the API key for a specific model (legacy method)
        /// </summary>
        public async Task<string> GetModelApiKeyAsync(string providerName, string modelName)
        {
            return await ResolveApiKeyAsync(providerName, modelName);
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
            bool isOpenAiFormat = Regex.IsMatch(apiKey, @"^sk-[A-Za-z0-9]{24,}$");
            bool isGroqFormat = Regex.IsMatch(apiKey, @"^gsk_[A-Za-z0-9]{16,}$");
            bool isOpenRouterFormat = Regex.IsMatch(apiKey, @"^sk-or-[A-Za-z0-9-]{20,}$");
            bool isGenericFormat = Regex.IsMatch(apiKey, @"^[A-Za-z0-9_\-]{16,}$");
            
            return isOpenAiFormat || isGroqFormat || isOpenRouterFormat || isGenericFormat;
        }

        /// <summary>
        /// Gets the environment variable name for a provider
        /// </summary>
        public string GetProviderLevelKeyName(string providerName)
        {
            if (string.IsNullOrEmpty(providerName))
                return string.Empty;
                
            return $"AI_KEY_{providerName.ToUpperInvariant()}";
        }
        
        /// <summary>
        /// Gets the environment variable name for a specific model
        /// </summary>
        public string GetModelSpecificKeyName(string providerName, string modelName)
        {
            if (string.IsNullOrEmpty(providerName) || string.IsNullOrEmpty(modelName))
                return string.Empty;
                
            string normalizedModelName = modelName.Replace("-", "_").Replace("/", "_").ToUpperInvariant();
            return $"AI_KEY_{providerName.ToUpperInvariant()}_{normalizedModelName}";
        }

        /// <summary>
        /// Legacy method for backward compatibility
        /// </summary>
        public string GetEnvironmentVariableName(string providerName)
        {
            return GetProviderLevelKeyName(providerName);
        }
        
        /// <summary>
        /// Clears resolved cache entries for a provider
        /// </summary>
        private void ClearProviderFromResolvedCache(string providerName)
        {
            var keysToRemove = new List<string>();
            
            foreach (var key in _resolvedKeyCache.Keys)
            {
                if (key.Equals(providerName, StringComparison.OrdinalIgnoreCase) ||
                    key.StartsWith($"{providerName}:", StringComparison.OrdinalIgnoreCase))
                {
                    keysToRemove.Add(key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                _resolvedKeyCache.Remove(key);
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
        /// Batch provider availability check -
        /// </summary>
        public Dictionary<string, bool> GetProviderAvailabilityBatch(IEnumerable<string> providerNames)
        {
            var results = new Dictionary<string, bool>();
            
            try
            {
                foreach (var provider in providerNames)
                {
                    results[provider] = HasActiveApiKeySync(provider);
                }
                
                return results;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApiKeyManager: Error in GetProviderAvailabilityBatch: {ex.Message}");
                return results;
            }
        }

        /// <summary>
        /// Clears all cached API key data to force fresh validation
        /// </summary>
        public async Task ClearCacheAsync()
        {
            await _cacheLock.WaitAsync();
            try
            {
                Debug.WriteLine("ApiKeyManager: Clearing all cache data");
                
                _resolvedKeyCache.Clear();
                _providerAvailabilityCache.Clear();
                _cacheTimestamps.Clear();
                _lastBulkCacheRefresh = DateTime.MinValue;
                
                Debug.WriteLine("ApiKeyManager: Cache cleared successfully");
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        /// <summary>
        /// Clears cache for a specific provider
        /// </summary>
        public async Task ClearProviderCacheAsync(string providerName)
        {
            if (string.IsNullOrEmpty(providerName)) return;
            
            await _cacheLock.WaitAsync();
            try
            {
                Debug.WriteLine($"ApiKeyManager: Clearing cache for provider: {providerName}");
                
                ClearProviderFromResolvedCache(providerName);
                _providerAvailabilityCache.Remove(providerName);
                _cacheTimestamps.Remove(providerName);
                
                Debug.WriteLine($"ApiKeyManager: Cache cleared for provider: {providerName}");
            }
            finally
            {
                _cacheLock.Release();
            }
        }
    }
}
