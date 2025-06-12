using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Core.Models;
using NexusChat.Services.Interfaces;
using NexusChat.Services.AIProviders.Implementations; // Ensure this is present

namespace NexusChat.Services.AIManagement
{
    /// <summary>
    /// Service for discovering AI models from various providers and sources
    /// </summary>
    public class AIModelDiscoveryService : IAIModelDiscoveryService
    {
        private readonly IApiKeyManager _apiKeyManager;
        private readonly Dictionary<string, List<AIModel>> _modelCache = new Dictionary<string, List<AIModel>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DateTime> _cacheTimestamps = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(10); // Cache model lists for 10 minutes
        private readonly SemaphoreSlim _discoveryLock = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _cacheLock = new SemaphoreSlim(1, 1);

        public AIModelDiscoveryService(IApiKeyManager apiKeyManager)
        {
            _apiKeyManager = apiKeyManager ?? throw new ArgumentNullException(nameof(apiKeyManager));
        }

        /// <summary>
        /// Discovers all models from all available providers
        /// </summary>
        public async Task<List<AIModel>> DiscoverAllModelsAsync()
        {
            await _discoveryLock.WaitAsync();
            
            try
            {
                Debug.WriteLine("AIModelDiscoveryService: Starting model discovery from all providers.");
                
                // Use IApiKeyManager to get a list of providers that might have keys or are known.
                // This could be a predefined list or dynamically determined.
                // For now, let's assume a known list of providers we support direct discovery for.
                var providersToQuery = new List<string> { "Groq", "OpenRouter", "Anthropic" };
                // Alternatively, you could use _apiKeyManager.GetActiveProvidersAsync() if it's reliable
                // for finding all providers you want to query, not just those with currently set keys.

                Debug.WriteLine($"AIModelDiscoveryService: Querying providers: {string.Join(", ", providersToQuery)}");
                
                var discoveryTasks = providersToQuery.Select(async provider =>
                {
                    try
                    {
                        // Pass _apiKeyManager to DiscoverProviderModelsAsync
                        var models = await DiscoverProviderModelsAsync(provider); 
                        Debug.WriteLine($"AIModelDiscoveryService: Discovered {models.Count} models from {provider}");
                        return new { Provider = provider, Models = models, Success = true };
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"AIModelDiscoveryService: Error discovering models for {provider}: {ex.Message}");
                        return new { Provider = provider, Models = new List<AIModel>(), Success = false };
                    }
                }).ToList();
                
                var results = await Task.WhenAll(discoveryTasks);
                
                var allModels = new List<AIModel>();
                foreach (var result in results)
                {
                    if (result.Success && result.Models.Any())
                    {
                        allModels.AddRange(result.Models);
                    }
                }
                
                if (!allModels.Any())
                {
                    Debug.WriteLine("AIModelDiscoveryService: No models discovered from any providers.");
                }
                
                Debug.WriteLine($"AIModelDiscoveryService: Total discovered models: {allModels.Count}");
                return allModels.DistinctBy(m => new { m.ProviderName, m.ModelName }).ToList(); // Ensure uniqueness
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AIModelDiscoveryService: Error in DiscoverAllModelsAsync: {ex.Message}");
                return new List<AIModel>();
            }
            finally
            {
                _discoveryLock.Release();
            }
        }

        /// <summary>
        /// Discovers models for specific providers
        /// </summary>
        public async Task<List<AIModel>> DiscoverModelsForProvidersAsync(List<string> providerNames)
        {
            if (providerNames == null || !providerNames.Any())
                return new List<AIModel>();

            // No need for _discoveryLock here if DiscoverProviderModelsAsync handles its own caching and thread safety for external calls.
            // However, if multiple calls to DiscoverModelsForProvidersAsync can happen concurrently for the *same* providers,
            // the cache within DiscoverProviderModelsAsync will handle it.
            
            try
            {
                Debug.WriteLine($"AIModelDiscoveryService: Discovering models for {providerNames.Count} specific providers: {string.Join(", ", providerNames)}");
                
                var discoveryTasks = providerNames.Select(async provider =>
                {
                    try
                    {
                        var models = await DiscoverProviderModelsAsync(provider); // Pass _apiKeyManager
                        return new { Provider = provider, Models = models, Success = true };
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"AIModelDiscoveryService: Error discovering models for {provider}: {ex.Message}");
                        return new { Provider = provider, Models = new List<AIModel>(), Success = false };
                    }
                }).ToList();
                
                var results = await Task.WhenAll(discoveryTasks);
                
                var allModels = new List<AIModel>();
                foreach (var result in results)
                {
                    if (result.Success && result.Models.Any())
                    {
                        allModels.AddRange(result.Models);
                    }
                }
                
                Debug.WriteLine($"AIModelDiscoveryService: Total discovered models from specified providers: {allModels.Count}");
                return allModels.DistinctBy(m => new { m.ProviderName, m.ModelName }).ToList(); // Ensure uniqueness
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AIModelDiscoveryService: Error in DiscoverModelsForProvidersAsync: {ex.Message}");
                return new List<AIModel>();
            }
        }

        /// <summary>
        /// Discovers models for a specific provider with caching.
        /// It now calls static methods on provider-specific services.
        /// </summary>
        public async Task<List<AIModel>> DiscoverProviderModelsAsync(string providerName)
        {
            if (string.IsNullOrEmpty(providerName))
                return new List<AIModel>();

            string cacheKey = providerName.ToLowerInvariant();
            
            await _cacheLock.WaitAsync();
            try
            {
                if (_modelCache.TryGetValue(cacheKey, out var cachedModels) &&
                    _cacheTimestamps.TryGetValue(cacheKey, out var timestamp) &&
                    DateTime.UtcNow - timestamp < _cacheExpiry)
                {
                    Debug.WriteLine($"AIModelDiscoveryService: Returning cached models for {providerName} ({cachedModels?.Count ?? 0} models).");
                    return cachedModels ?? new List<AIModel>();
                }
            }
            finally
            {
                _cacheLock.Release();
            }
            
            List<AIModel> models = new List<AIModel>();
            try
            {
                Debug.WriteLine($"AIModelDiscoveryService: No cache or cache expired for {providerName}. Discovering anew.");
                // The _apiKeyManager is passed to the static discovery methods.
                models = providerName.ToLowerInvariant() switch
                {
                    "groq" => await GroqAIService.DiscoverModelsAsync(_apiKeyManager),
                    "openrouter" => await OpenRouterAIService.DiscoverModelsAsync(_apiKeyManager),
                    _ => new List<AIModel>() // Unknown provider
                };
                
                if (!models.Any() && providerName.ToLowerInvariant() != "dummy") // Avoid caching empty for known if error, unless it's truly no models
                {
                     Debug.WriteLine($"AIModelDiscoveryService: No models returned from {providerName}'s discovery method.");
                }

                await _cacheLock.WaitAsync();
                try
                {
                    _modelCache[cacheKey] = models;
                    _cacheTimestamps[cacheKey] = DateTime.UtcNow;
                    Debug.WriteLine($"AIModelDiscoveryService: Cached {models.Count} models for {providerName}.");
                }
                finally
                {
                    _cacheLock.Release();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AIModelDiscoveryService: Error calling static discovery for {providerName}: {ex.Message}");
                return new List<AIModel>(); // Return empty on error
            }
            
            return models;
        }

        /// <summary>
        /// Clears the discovery cache
        /// </summary>
        public void ClearCache()
        {
            _cacheLock.Wait();
            try
            {
                _modelCache.Clear();
                _cacheTimestamps.Clear();
                Debug.WriteLine("AIModelDiscoveryService: Cache cleared");
            }
            finally
            {
                _cacheLock.Release();
            }
        }
    }
}
