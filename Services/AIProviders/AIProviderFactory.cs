using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NexusChat.Core.Models;
using NexusChat.Services.Interfaces;
using NexusChat.Services.AIProviders.Implementations;

namespace NexusChat.Services.AIProviders
{
    /// <summary>
    /// Factory for creating AI provider services and managing model discovery
    /// </summary>
    public class AIProviderFactory : IAIProviderFactory
    {
        private readonly IApiKeyManager _apiKeyManager;
        private readonly IAIModelDiscoveryService _modelDiscoveryService;
        private bool _hasInitializedProviders = false;

        public AIProviderFactory(IApiKeyManager apiKeyManager, IAIModelDiscoveryService modelDiscoveryService)
        {
            _apiKeyManager = apiKeyManager ?? throw new ArgumentNullException(nameof(apiKeyManager));
            _modelDiscoveryService = modelDiscoveryService ?? throw new ArgumentNullException(nameof(modelDiscoveryService));
        }

        /// <summary>
        /// Gets a provider service for a specific model. Returns null if service cannot be created.
        /// </summary>
        public Task<IAIProviderService> GetProviderForModelAsync(string providerName, string modelName)
        {
            if (string.IsNullOrEmpty(providerName) || string.IsNullOrEmpty(modelName))
            {
                Debug.WriteLine("AIProviderFactory: Provider or model name is null or empty. No service will be created.");
                return Task.FromResult<IAIProviderService>(null);
            }
            
            try
            {
                bool hasApiKey = _apiKeyManager.HasActiveApiKeySync(providerName);
                if (!hasApiKey)
                {
                    Debug.WriteLine($"AIProviderFactory: No active API key for {providerName} (cached result). No service will be created.");
                    return Task.FromResult<IAIProviderService>(null);
                }
                
                IAIProviderService service = providerName.ToLowerInvariant() switch
                {
                    "groq" => new GroqAIService(_apiKeyManager, modelName),
                    "openrouter" => new OpenRouterAIService(_apiKeyManager, modelName),
                    _ => null 
                };
                
                if (service == null)
                {
                    Debug.WriteLine($"AIProviderFactory: Unknown or unsupported provider: {providerName}. No service will be created.");
                }
                
                return Task.FromResult(service);
            }
            catch (ArgumentException argEx) // Catch specific exceptions from service constructors
            {
                Debug.WriteLine($"AIProviderFactory: Invalid argument for {providerName}/{modelName}: {argEx.Message}. No service will be created.");
                return Task.FromResult<IAIProviderService>(null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AIProviderFactory: Error creating AI service for {providerName}/{modelName}: {ex.Message}. No service will be created.");
                return Task.FromResult<IAIProviderService>(null);
            }
        }

        /// <summary>
        /// Gets available providers that have valid API keys with cache optimization 
        /// </summary>
        public async Task<List<string>> GetActiveProvidersAsync()
        {
            try
            {
                return await _apiKeyManager.GetActiveProvidersAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting active providers: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Gets available providers synchronously using cache 
        /// </summary>
        public List<string> GetActiveProvidersSync()
        {
            try
            {
                return _apiKeyManager.GetActiveProvidersSync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting active providers synchronously: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Checks if a provider has a valid API key using cache 
        /// </summary>
        public async Task<bool> IsProviderActiveAsync(string providerName)
        {
            try
            {
                return await _apiKeyManager.HasActiveApiKeyAsync(providerName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking if provider {providerName} is active: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if a provider has a valid API key synchronously using cache 
        /// </summary>
        public bool IsProviderActiveSync(string providerName)
        {
            try
            {
                return _apiKeyManager.HasActiveApiKeySync(providerName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking if provider {providerName} is active synchronously: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Discovers all available models from active providers.
        /// </summary>
        public async Task<List<AIModel>> DiscoverAllModelsAsync()
        {
            try
            {
                Debug.WriteLine("AIProviderFactory: Starting model discovery.");
                
                await _apiKeyManager.InitializeAsync();
                
                var activeProviders = _apiKeyManager.GetActiveProvidersSync();
                
                if (activeProviders.Count == 0)
                {
                    Debug.WriteLine("AIProviderFactory: Sync cache for active providers empty, trying async method to populate.");
                    activeProviders = await _apiKeyManager.GetActiveProvidersAsync();
                    Debug.WriteLine($"AIProviderFactory: Async method returned {activeProviders.Count} active providers.");
                }
                
                // If cache is still empty, check known real providers directly.
                if (activeProviders.Count == 0)
                {
                    Debug.WriteLine("AIProviderFactory: No cached active providers found, checking known real providers directly.");
                    var knownProviders = new[] { "Groq", "OpenRouter" }; // Only check real, known providers
                    
                    foreach (var provider in knownProviders)
                    {
                        try
                        {
                            bool hasKey = await _apiKeyManager.HasActiveApiKeyAsync(provider);
                            if (hasKey)
                            {
                                // Ensure no duplicates if GetActiveProvidersAsync populates it later
                                if (!activeProviders.Contains(provider, StringComparer.OrdinalIgnoreCase))
                                {
                                    activeProviders.Add(provider);
                                }
                                Debug.WriteLine($"AIProviderFactory: Found active provider by direct check: {provider}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"AIProviderFactory: Error checking provider {provider} directly: {ex.Message}");
                        }
                    }
                }
                
                // Filter out any "Dummy" provider that might have slipped in, if it's still in the system
                activeProviders = activeProviders.Where(p => !p.Equals("Dummy", StringComparison.OrdinalIgnoreCase)).ToList();

                Debug.WriteLine($"AIProviderFactory: Final list of active providers for model discovery: {string.Join(", ", activeProviders)}");
                
                if (activeProviders.Count == 0)
                {
                    Debug.WriteLine("AIProviderFactory: No active providers found. Returning empty list of models.");
                    return new List<AIModel>(); // Return empty list, not dummy models
                }
                
                var models = await _modelDiscoveryService.DiscoverModelsForProvidersAsync(activeProviders);
                
                _hasInitializedProviders = true;
                Debug.WriteLine($"AIProviderFactory: Successfully discovered {models.Count} models from {activeProviders.Count} active providers.");
                return models;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AIProviderFactory: Error in DiscoverAllModelsAsync: {ex.Message}");
                
                try
                {
                    Debug.WriteLine("AIProviderFactory: Trying fallback general discovery for actual models.");
                    // Ensure this call in your IAIModelDiscoveryService also does not return dummy/fallback models
                    var models = await _modelDiscoveryService.DiscoverAllModelsAsync(); 
                     // Filter again, just in case DiscoverAllModelsAsync might return dummy models
                    models = models.Where(m => !m.ProviderName.Equals("Dummy", StringComparison.OrdinalIgnoreCase)).ToList();
                    _hasInitializedProviders = true;
                    Debug.WriteLine($"AIProviderFactory: Fallback (general discovery) discovered {models.Count} models.");
                    return models;
                }
                catch (Exception fallbackEx)
                {
                    Debug.WriteLine($"AIProviderFactory: Fallback (general discovery) also failed: {fallbackEx.Message}. Returning empty list.");
                    return new List<AIModel>(); // Return empty list on failure
                }
            }
        }

        /// <summary>
        /// Force refresh of cached models and API key availability
        /// </summary>
        public async Task ClearModelCacheAsync()
        {
            try
            {
                _hasInitializedProviders = false;
                _modelDiscoveryService.ClearCache();
                
                await _apiKeyManager.RefreshAvailabilityCacheAsync();
                
                Debug.WriteLine("AIProviderFactory: Model cache and API key availability cache cleared.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error clearing model cache asynchronously: {ex.Message}");
            }
        }

        /// <summary>
        /// Force refresh of cached models (synchronous version)
        /// </summary>
        public void ClearModelCache()
        {
            try
            {
                _hasInitializedProviders = false;
                _modelDiscoveryService.ClearCache();
                Debug.WriteLine("AIProviderFactory: Model cache cleared (API key cache refresh will happen asynchronously).");
                
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _apiKeyManager.RefreshAvailabilityCacheAsync();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Background API key cache refresh error: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error clearing model cache: {ex.Message}");
            }
        }

    }
}
