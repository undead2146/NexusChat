using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using NexusChat.Core.Models;
using NexusChat.Data.Repositories;
using NexusChat.Services.Interfaces;

namespace NexusChat.Services.AIManagement
{
    /// <summary>
    /// Unified service for loading model configurations from environment variables and database
    /// </summary>
    public class ModelLoaderService : IModelLoaderService, IStartupInitializer
    {
        private readonly IEnvironmentService _environmentService;
        private readonly IModelConfigurationRepository _repository;
        private readonly IMemoryCache _cache;
        private readonly SemaphoreSlim _loadLock = new SemaphoreSlim(1, 1);
        private bool _initialized = false;
        
        // Cache keys
        private const string ENV_MODELS_CACHE_KEY = "env_models";
        private const string ALL_MODELS_CACHE_KEY = "all_models";
        private const string MODEL_PREFIX_CACHE_KEY = "model_prefix_";
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(15);

        // Environment variable patterns - compiled for performance
        private static readonly Regex ModelKeyPattern = new Regex(@"AI_MODEL_([A-Z0-9_]+)_([A-Z0-9_]+)", RegexOptions.Compiled);
        private static readonly Regex ModelCapabilityPattern = new Regex(@"AI_MODEL_CAP_([A-Z0-9_]+)_([A-Z0-9_]+)_([A-Z0-9_]+)", RegexOptions.Compiled);
        private static readonly Regex ModelDescriptionPattern = new Regex(@"AI_MODEL_DESC_([A-Z0-9_]+)_([A-Z0-9_]+)", RegexOptions.Compiled);

        /// <summary>
        /// Creates a new instance of the model loader service
        /// </summary>
        public ModelLoaderService(
            IEnvironmentService environmentService,
            IModelConfigurationRepository repository,
            IMemoryCache cache)
        {
            _environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        /// <summary>
        /// Initializes the service by loading models from environment to database
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_initialized)
                return;

            await _loadLock.WaitAsync();
            try
            {
                if (_initialized) return;

                // Ensure environment variables are loaded
                await _environmentService.InitializeAsync();

                // Parse models from environment
                var envConfigs = ParseModelConfigurationsFromEnvironment();
                
                // Cache for future use
                _cache.Set(ENV_MODELS_CACHE_KEY, envConfigs, _cacheExpiry);

                // Import to database (if any found)
                if (envConfigs.Any())
                {
                    int count = await _repository.ImportFromEnvironmentAsync(envConfigs);
                    Debug.WriteLine($"Imported {count} model configurations from environment");
                }
                else
                {
                    Debug.WriteLine("No model configurations found in environment variables.");
                }

                _initialized = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing model loader: {ex.Message}");
            }
            finally
            {
                _loadLock.Release();
            }
        }

        /// <summary>
        /// Gets all available model configurations
        /// </summary>
        public async Task<List<ModelConfiguration>> LoadModelsAsync()
        {
            await EnsureInitializedAsync();
            
            // Try to get from cache first
            if (_cache.TryGetValue(ALL_MODELS_CACHE_KEY, out List<ModelConfiguration> cachedModels))
            {
                return cachedModels;
            }
            
            // Load from repository
            var models = await _repository.GetAllAsync();
            
            // Cache the results
            _cache.Set(ALL_MODELS_CACHE_KEY, models, _cacheExpiry);
            
            return models;
        }

        /// <summary>
        /// Loads provider configurations from database
        /// </summary>
        public async Task<List<ProviderConfiguration>> LoadProvidersAsync()
        {
            await EnsureInitializedAsync();
            
            // Get all models and group them by provider
            var models = await LoadModelsAsync();
            var providerGroups = models.GroupBy(m => m.ProviderName);
            
            // Create provider configurations
            var providers = new List<ProviderConfiguration>();
            foreach (var group in providerGroups)
            {
                // Create a provider configuration for each provider
                var provider = new ProviderConfiguration
                {
                    ProviderId = group.Key,
                    Name = group.Key,
                    IsEnabled = true
                };
                
                // Add provider-specific settings if available
                var firstModel = group.FirstOrDefault();
                if (firstModel != null)
                {
                    provider.ApiKeyName = $"AI_KEY_{group.Key.ToUpperInvariant()}";
                    provider.ApiEndpoint = firstModel.ApiEndpoint;
                }
                
                providers.Add(provider);
            }
            
            return providers;
        }
        
        /// <summary>
        /// Saves a model configuration
        /// </summary>
        public async Task SaveModelConfigurationAsync(ModelConfiguration config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
                
            await EnsureInitializedAsync();
            
            // Update or insert the configuration
            if (config.Id > 0)
            {
                await _repository.UpdateAsync(config);
            }
            else
            {
                await _repository.AddAsync(config);
            }
            
            // Clear caches
            _cache.Remove(ALL_MODELS_CACHE_KEY);
            _cache.Remove($"{MODEL_PREFIX_CACHE_KEY}{config.ProviderName}");
        }
        
        /// <summary>
        /// Parses model configurations from environment variables using efficient batch processing
        /// </summary>
        private List<ModelConfiguration> ParseModelConfigurationsFromEnvironment()
        {
            // Get all environment variables in one call for efficiency
            var allVars = _environmentService.GetAllVariables();
            var result = new List<ModelConfiguration>();
            
            // Process known providers in an organized way
            ProcessProviderModels("GROQ", allVars, result);
            ProcessProviderModels("OPENROUTER", allVars, result);
            ProcessProviderModels("AZURE", allVars, result);
            
            // Look for any other providers that might be defined
            var otherProviders = FindOtherProviders(allVars, new[] { "GROQ", "OPENROUTER", "AZURE" });
            foreach (var provider in otherProviders)
            {
                ProcessProviderModels(provider, allVars, result);
            }
            
            return result;
        }
        
        /// <summary>
        /// Finds additional providers defined in environment variables
        /// </summary>
        private IEnumerable<string> FindOtherProviders(Dictionary<string, string> allVars, string[] knownProviders)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            // Look for AI_MODEL_X_ patterns
            foreach (var key in allVars.Keys)
            {
                if (key.StartsWith("AI_MODEL_", StringComparison.OrdinalIgnoreCase))
                {
                    var match = ModelKeyPattern.Match(key);
                    if (match.Success && match.Groups.Count >= 2)
                    {
                        string provider = match.Groups[1].Value;
                        if (!knownProviders.Contains(provider, StringComparer.OrdinalIgnoreCase))
                        {
                            result.Add(provider);
                        }
                    }
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Processes models for a specific provider from environment variables
        /// </summary>
        private void ProcessProviderModels(string providerName, Dictionary<string, string> allVars, List<ModelConfiguration> models)
        {
            string prefix = $"AI_MODEL_{providerName}_";
            var modelVars = allVars.Where(kv => kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                                  .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
            
            // Group by model key (the part after AI_MODEL_PROVIDER_)
            var modelGroups = modelVars.Keys
                .Select(k => new { 
                    Key = k, 
                    Value = modelVars[k], 
                    ModelKey = k.Substring(prefix.Length) 
                })
                .GroupBy(x => x.ModelKey)
                .ToList();
            
            // Process each model
            foreach (var group in modelGroups)
            {
                // Skip capability and description keys - they'll be handled separately
                if (group.Key.StartsWith("CAP_", StringComparison.OrdinalIgnoreCase) || 
                    group.Key.StartsWith("DESC_", StringComparison.OrdinalIgnoreCase))
                    continue;
                    
                string modelKey = group.Key;
                string modelId = group.First().Value;
                
                // Look for related environment variables
                string descKey = $"AI_MODEL_DESC_{providerName}_{modelKey}";
                string capTokensKey = $"AI_MODEL_CAP_{providerName}_{modelKey}_TOKENS";
                string capContextKey = $"AI_MODEL_CAP_{providerName}_{modelKey}_CONTEXT";
                string capStreamingKey = $"AI_MODEL_CAP_{providerName}_{modelKey}_STREAMING";
                string capTempKey = $"AI_MODEL_CAP_{providerName}_{modelKey}_TEMP";
                string capCodeKey = $"AI_MODEL_CAP_{providerName}_{modelKey}_CODE";
                string capVisionKey = $"AI_MODEL_CAP_{providerName}_{modelKey}_VISION";
                
                // Create model with values from environment
                var model = new ModelConfiguration
                {
                    ProviderName = NormalizeProviderName(providerName),
                    ModelIdentifier = modelId,
                    Description = GetValueOrDefault(allVars, descKey, $"{modelKey} model"),
                    ApiKeyEnvironmentVariable = $"AI_KEY_{providerName}_{modelKey}",
                    IsEnabled = true,
                    Source = ConfigurationSource.Environment,
                    Capabilities = new ModelCapabilities
                    {
                        MaxTokens = TryParseInt(GetValueOrDefault(allVars, capTokensKey), 4096),
                        MaxContextWindow = TryParseInt(GetValueOrDefault(allVars, capContextKey), 8192),
                        SupportsStreaming = TryParseBool(GetValueOrDefault(allVars, capStreamingKey), true),
                        DefaultTemperature = TryParseFloat(GetValueOrDefault(allVars, capTempKey), 0.7f),
                        SupportsCodeCompletion = TryParseBool(GetValueOrDefault(allVars, capCodeKey), false),
                        SupportsVision = TryParseBool(GetValueOrDefault(allVars, capVisionKey), false)
                    }
                };
                
                models.Add(model);
            }
        }
        
        /// <summary>
        /// Gets a value from a dictionary with a default fallback
        /// </summary>
        private string GetValueOrDefault(Dictionary<string, string> dict, string key, string defaultValue = null)
        {
            return dict.TryGetValue(key, out string value) ? value : defaultValue;
        }
        
        /// <summary>
        /// Tries to parse an integer from a string
        /// </summary>
        private int TryParseInt(string value, int defaultValue)
        {
            if (int.TryParse(value, out int result))
                return result;
            return defaultValue;
        }
        
        /// <summary>
        /// Tries to parse a float from a string
        /// </summary>
        private float TryParseFloat(string value, float defaultValue)
        {
            if (float.TryParse(value, out float result))
                return result;
            return defaultValue;
        }
        
        /// <summary>
        /// Tries to parse a boolean from a string
        /// </summary>
        private bool TryParseBool(string value, bool defaultValue)
        {
            if (bool.TryParse(value, out bool result))
                return result;
            return defaultValue;
        }

        /// <summary>
        /// Normalizes provider names for consistency
        /// </summary>
        public string NormalizeProviderName(string providerName)
        {
            if (string.IsNullOrEmpty(providerName))
                return "Unknown";

            providerName = providerName.Trim();

            // Normalize common provider name variations
            switch (providerName.ToLowerInvariant())
            {
                case "groq": case "groqi": case "groqapi": 
                    return "Groq";
                case "openrouter": case "or": case "openr":
                    return "OpenRouter";
                case "openai": case "oai":
                    return "OpenAI";
                case "anthropic": case "claude":
                    return "Anthropic";
                case "google": case "gemini":
                    return "Google";
                case "dummy": case "test": case "testing":
                    return "Dummy";
                default:
                    return char.ToUpper(providerName[0]) + providerName.Substring(1).ToLowerInvariant();
            }
        }

        /// <summary>
        /// Ensures the service is initialized
        /// </summary>
        private async Task EnsureInitializedAsync()
        {
            if (!_initialized)
            {
                await InitializeAsync();
            }
        }

        /// <summary>
        /// Gets all available model configurations with optional refresh
        /// </summary>
        public async Task<List<ModelConfiguration>> LoadModelConfigurationsAsync(bool forceRefresh = false)
        {
            if (forceRefresh)
            {
                _cache.Remove(ALL_MODELS_CACHE_KEY);
            }
            
            // Use existing method
            return await LoadModelsAsync();
        }

        /// <summary>
        /// Gets a specific model configuration by provider and model name
        /// </summary>
        public async Task<ModelConfiguration> GetModelConfigurationAsync(string providerName, string modelName)
        {
            if (string.IsNullOrEmpty(providerName) || string.IsNullOrEmpty(modelName))
                return null;
                
            await EnsureInitializedAsync();
            
            // Create cache key for this specific query
            string cacheKey = $"{MODEL_PREFIX_CACHE_KEY}{providerName.ToLowerInvariant()}_{modelName.ToLowerInvariant()}";
            
            // Try to get from cache
            if (_cache.TryGetValue(cacheKey, out ModelConfiguration cachedConfig))
            {
                return cachedConfig;
            }
            
            // Get all models and search
            var allModels = await LoadModelsAsync();
            var model = allModels.FirstOrDefault(m => 
                m.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase) && 
                m.ModelIdentifier.Equals(modelName, StringComparison.OrdinalIgnoreCase));
            
            // Cache result, even if null
            _cache.Set(cacheKey, model, _cacheExpiry);
            
            return model;
        }

        /// <summary>
        /// Extracts models from environment variables without saving to database
        /// </summary>
        public async Task<List<AIModel>> ExtractModelsFromEnvironmentAsync()
        {
            await EnsureInitializedAsync();
            
            // Use existing method to parse configurations
            var configs = ParseModelConfigurationsFromEnvironment();
            
            // Convert ModelConfiguration to AIModel
            var models = new List<AIModel>();
            foreach (var config in configs)
            {
                models.Add(new AIModel
                {
                    ModelName = config.ModelIdentifier,
                    ProviderName = config.ProviderName,
                    Description = config.Description,
                    MaxTokens = config.Capabilities?.MaxTokens ?? 4096,
                    MaxContextWindow = config.Capabilities?.MaxContextWindow ?? 8192,
                    SupportsStreaming = config.Capabilities?.SupportsStreaming ?? true,
                    DefaultTemperature = config.Capabilities?.DefaultTemperature ?? 0.7f
                });
            }
            
            return models;
        }

        /// <summary>
        /// Creates default configurations when none are available
        /// </summary>
        public async Task<List<ModelConfiguration>> CreateFallbackConfigurationsAsync()
        {
            var fallbacks = new List<ModelConfiguration>
            {
                // Dummy model for testing
                new ModelConfiguration
                {
                    ProviderName = "Dummy",
                    ModelIdentifier = "dummy-model",
                    Description = "Fallback model for testing purposes",
                    IsEnabled = true,
                    Source = ConfigurationSource.UserDefined,
                    Capabilities = new ModelCapabilities
                    {
                        MaxTokens = 4096,
                        MaxContextWindow = 8192,
                        SupportsStreaming = true,
                        DefaultTemperature = 0.7f
                    }
                },
                // Default Groq model
                new ModelConfiguration
                {
                    ProviderName = "Groq",
                    ModelIdentifier = "llama3-70b-8192",
                    Description = "Llama 3 (70B) via Groq",
                    IsEnabled = true,
                    Source = ConfigurationSource.UserDefined,
                    ApiKeyEnvironmentVariable = "AI_KEY_GROQ",
                    IsDefault = true,
                    Capabilities = new ModelCapabilities
                    {
                        MaxTokens = 4096,
                        MaxContextWindow = 8192,
                        SupportsStreaming = true,
                        DefaultTemperature = 0.7f
                    }
                }
            };
            
            // Save to repository
            foreach (var fallback in fallbacks)
            {
                await SaveModelConfigurationAsync(fallback);
            }
            
            return fallbacks;
        }

        /// <summary>
        /// Clears all cached configurations
        /// </summary>
        public void ClearCache()
        {
            _cache.Remove(ALL_MODELS_CACHE_KEY);
            _cache.Remove(ENV_MODELS_CACHE_KEY);
            
            // Clear any provider-specific caches
            var prefixes = new[] { MODEL_PREFIX_CACHE_KEY };
            foreach (var prefix in prefixes)
            {
                // We can't enumerate all cache keys, so remove known patterns
                foreach (var provider in new[] { "groq", "openrouter", "azure", "dummy" })
                {
                    _cache.Remove($"{prefix}{provider}");
                }
            }
            
            Debug.WriteLine("Cleared model configuration caches");
        }
    }
}
