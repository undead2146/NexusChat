using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NexusChat.Core.Models;
using NexusChat.Services.AIProviders.Implementations;
using NexusChat.Services.Interfaces;

namespace NexusChat.Services.AIProviders
{
    /// <summary>
    /// Factory class for creating AI provider services
    /// </summary>
    public class AIProviderFactory : IAIProviderFactory
    {
        private readonly IApiKeyManager _apiKeyManager;
        private bool _hasInitializedProviders = false;
        private Dictionary<string, List<AIModel>> _cachedModels = new Dictionary<string, List<AIModel>>();
        
        /// <summary>
        /// Creates a new instance of AIProviderFactory
        /// </summary>
        /// <param name="apiKeyManager">API key manager for service initialization</param>
        public AIProviderFactory(IApiKeyManager apiKeyManager)
        {
            _apiKeyManager = apiKeyManager ?? throw new ArgumentNullException(nameof(apiKeyManager));
        }
        
        /// <summary>
        /// Gets a default service when no specific model is selected
        /// </summary>
        public IAIProviderService GetDefaultService()
        {
            // Create a dummy service as a safe default
            return new DummyAIService(_apiKeyManager, "DummyGPT");
        }
        
        /// <summary>
        /// Gets a provider service for a specific model
        /// </summary>
        public IAIProviderService GetProviderForModel(string providerName, string modelName)
        {
            if (string.IsNullOrEmpty(providerName) || string.IsNullOrEmpty(modelName))
            {
                Debug.WriteLine("Provider or model name is null - using dummy service");
                return GetDefaultService();
            }
            
            try
            {
                // First check if we have a valid API key for this provider
                bool hasApiKey = _apiKeyManager.HasApiKeyAsync(providerName).GetAwaiter().GetResult();
                if (!hasApiKey)
                {
                    Debug.WriteLine($"No API key for {providerName} - using dummy service");
                    return GetDefaultService();
                }
                
                switch (providerName.ToLowerInvariant())
                {
                    case "groq":
                        // Create Groq service with the specified model
                        return new GroqAIService(_apiKeyManager, modelName);
                        
                    case "openrouter":
                        // Create OpenRouter service with the specified model  
                        return new OpenRouterAIService(_apiKeyManager, modelName);
                        
                    case "dummy":
                        // Return dummy service for testing
                        return new DummyAIService(_apiKeyManager, modelName);
                }
                
                // If provider not supported or model not found, return default
                Debug.WriteLine($"Unknown provider or model: {providerName}/{modelName} - using dummy service");
                return GetDefaultService();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating AI service: {ex.Message}");
                return GetDefaultService();
            }
        }
        
        /// <summary>
        /// Gets all available providers
        /// </summary>
        public List<string> GetAvailableProviders()
        {
            return new List<string> { "Groq", "OpenRouter", "Dummy" };
        }
        
        /// <summary>
        /// Gets available providers that have valid API keys
        /// </summary>
        public async Task<List<string>> GetActiveProvidersAsync()
        {
            var result = new List<string>();
            
            foreach (var provider in GetAvailableProviders())
            {
                if (await _apiKeyManager.HasApiKeyAsync(provider))
                {
                    result.Add(provider);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Gets models available for a specific provider, but only if API key exists
        /// </summary>
        public async Task<List<AIModel>> GetModelsForProviderAsync(string providerName)
        {
            if (string.IsNullOrEmpty(providerName))
            {
                return new List<AIModel>();
            }
            
            try
            {
                // First check if provider has a valid API key
                bool hasApiKey = await _apiKeyManager.HasApiKeyAsync(providerName);
                if (!hasApiKey)
                {
                    Debug.WriteLine($"No API key for provider {providerName}, skipping model loading");
                    return new List<AIModel>();
                }
                
                // Check if we have cached models
                if (_cachedModels.ContainsKey(providerName))
                {
                    Debug.WriteLine($"Using cached models for {providerName}");
                    return _cachedModels[providerName];
                }
                
                Debug.WriteLine($"AIProviderFactory: Getting models for provider: {providerName}");
                
                List<AIModel> models = new List<AIModel>();
                
                switch (providerName.ToLowerInvariant())
                {
                    case "groq":
                        // Use static method from GroqAIService
                        models = GroqAIService.GetAvailableModels().ToList();
                        Debug.WriteLine($"Loaded {models.Count} Groq models");
                        break;
                        
                    case "openrouter":
                        // FIXED: Use OpenRouterAIService instead of GroqAIService
                        models = OpenRouterAIService.GetAvailableModels().ToList();
                        Debug.WriteLine($"Loaded {models.Count} OpenRouter models");
                        break;
                        
                    case "dummy":
                        // Return dummy models
                        models = new List<AIModel> { 
                            new AIModel { 
                                ModelName = "DummyGPT", 
                                ProviderName = "Dummy", 
                                Description = "Test model that doesn't make API calls",
                                MaxTokens = 4096,
                                MaxContextWindow = 8192,
                                IsAvailable = true
                            },
                            new AIModel { 
                                ModelName = "DummyAssistant", 
                                ProviderName = "Dummy", 
                                Description = "Another test model for development",
                                MaxTokens = 4096,
                                MaxContextWindow = 8192,
                                IsAvailable = true
                            }
                        };
                        Debug.WriteLine($"Loaded {models.Count} Dummy models");
                        break;
                        
                    default:
                        Debug.WriteLine($"Unknown provider: {providerName}");
                        return new List<AIModel>();
                }
                
                // Cache the models
                _cachedModels[providerName] = models;
                return models;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting models for provider {providerName}: {ex.Message}");
                return new List<AIModel>();
            }
        }
        
        /// <summary>
        /// Gets models for a provider (sync version with async call inside)
        /// </summary>
        public List<AIModel> GetModelsForProvider(string providerName)
        {
            return GetModelsForProviderAsync(providerName).GetAwaiter().GetResult();
        }
        
        /// <summary>
        /// Gets all available models from providers with API keys
        /// </summary>
        public async Task<List<AIModel>> GetAllModelsAsync()
        {
            var allModels = new List<AIModel>();
            
            try
            {
                // Get active providers that have API keys
                var activeProviders = await GetActiveProvidersAsync();
                Debug.WriteLine($"Found {activeProviders.Count} active providers with API keys");
                
                // Get models only from active providers
                foreach (var provider in activeProviders)
                {
                    var models = await GetModelsForProviderAsync(provider);
                    if (models != null)
                    {
                        allModels.AddRange(models);
                        Debug.WriteLine($"Added {models.Count} models from {provider} provider");
                    }
                }
                
                // Mark initialization complete
                _hasInitializedProviders = true;
                
                Debug.WriteLine($"AIProviderFactory: Got {allModels.Count} models from all providers with valid API keys");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting all models: {ex.Message}");
            }
            
            return allModels;
        }
        
        /// <summary>
        /// Gets all available models (sync version that calls async method inside)
        /// </summary>
        public List<AIModel> GetAllModels()
        {
            return GetAllModelsAsync().GetAwaiter().GetResult();
        }
        
        /// <summary>
        /// Checks if a provider is available
        /// </summary>
        public bool IsProviderAvailable(string providerName)
        {
            return GetAvailableProviders().Contains(providerName, StringComparer.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// Checks if a provider has a valid API key
        /// </summary>
        public async Task<bool> IsProviderActiveAsync(string providerName)
        {
            return await _apiKeyManager.HasApiKeyAsync(providerName);
        }
        
        /// <summary>
        /// Force refresh of cached models
        /// </summary>
        public void ClearModelCache()
        {
            _cachedModels.Clear();
            _hasInitializedProviders = false;
            Debug.WriteLine("AIProviderFactory: Model cache cleared");
        }
    }
}
