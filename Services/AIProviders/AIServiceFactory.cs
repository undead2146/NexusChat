using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using NexusChat.Core.Models;
using NexusChat.Data.Repositories;
using NexusChat.Services.AIProviders.Implementations;
using NexusChat.Services.Interfaces;

namespace NexusChat.Services.AIProviders
{
    /// <summary>
    /// Factory for creating AI services dynamically based on database configuration
    /// </summary>
    public class AIServiceFactory : IAIServiceFactory, IStartupInitializer
    {
        private readonly IApiKeyManager _apiKeyManager;
        private readonly IModelConfigurationRepository _repository;
        private readonly IMemoryCache _cache;
        
        // Lock for thread safety
        private readonly SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);
        private bool _initialized = false;
        
        // Cache keys and expiration times
        private const string CONFIG_CACHE_KEY = "model_config_";
        private const string SERVICE_CACHE_KEY = "service_";
        private const string PROVIDER_CACHE_KEY = "provider_";
        private readonly TimeSpan _configCacheTime = TimeSpan.FromMinutes(15);
        private readonly TimeSpan _serviceCacheTime = TimeSpan.FromMinutes(30);
        
        // Registry of service creators keyed by provider name (case-insensitive)
        private readonly Dictionary<string, Func<string, ModelCapabilities, IAIService>> _serviceCreators;
        
        // Registry of service types for reflection-based instantiation
        private readonly Dictionary<string, Type> _serviceTypes;
        
        // Dictionary of discovered types to avoid repeated reflection searches
        private readonly Dictionary<string, Type> _discoveredTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        
        // Provider name
        public string ProviderName => "DynamicAIFactory";
        
        /// <summary>
        /// Creates a new instance of the AI service factory
        /// </summary>
        public AIServiceFactory(
            IApiKeyManager apiKeyManager,
            IModelConfigurationRepository repository,
            IMemoryCache cache)
        {
            _apiKeyManager = apiKeyManager ?? throw new ArgumentNullException(nameof(apiKeyManager));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            
            // Initialize dictionaries with case-insensitive comparer
            _serviceCreators = new Dictionary<string, Func<string, ModelCapabilities, IAIService>>(
                StringComparer.OrdinalIgnoreCase);
            _serviceTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            
            // Register core service creators
            RegisterDefaultProviders();
        }
        
        /// <summary>
        /// Initializes the factory by preloading configurations
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
                    
                // Preload configurations for faster startup
                var configs = await _repository.GetAllAsync();
                
                // Cache each configuration for faster lookups
                foreach (var config in configs.Where(c => c.IsEnabled))
                {
                    string configCacheKey = $"{CONFIG_CACHE_KEY}{config.ModelIdentifier}";
                    _cache.Set(configCacheKey, config, _configCacheTime);
                }
                
                Debug.WriteLine($"Initialized AI Service Factory with {configs.Count} model configurations");
                _initialized = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing AI Service Factory: {ex.Message}");
                // Continue without initialization - will fall back to on-demand loading
            }
            finally
            {
                _initLock.Release();
            }
        }
        
        /// <summary>
        /// Registers the default providers
        /// </summary>
        private void RegisterDefaultProviders()
        {
            // Register function-based service creators
            RegisterServiceCreator("groq", CreateGroqService);
            RegisterServiceCreator("openrouter", CreateOpenRouterService);
            RegisterServiceCreator("dummy", CreateDummyService);
            //RegisterServiceCreator("azure", CreateAzureService);
            
            // Register types for reflection-based creation
            _serviceTypes["groq"] = typeof(GroqAIService);
            _serviceTypes["openrouter"] = typeof(OpenRouterAIService);
            _serviceTypes["dummy"] = typeof(DummyAIService);
            
            Debug.WriteLine("Registered default service providers");
        }
        
        /// <summary>
        /// Registers a service creator function for a specific provider
        /// </summary>
        public void RegisterServiceCreator(string providerName, Func<string, ModelCapabilities, IAIService> creator)
        {
            if (string.IsNullOrEmpty(providerName))
                throw new ArgumentException("Provider name cannot be empty", nameof(providerName));
            if (creator == null)
                throw new ArgumentNullException(nameof(creator));
                
            string normalizedName = NormalizeProviderName(providerName);
            _serviceCreators[normalizedName] = creator;
            
            // Clear any cached services for this provider
            ClearProviderCache(normalizedName);
            
            Debug.WriteLine($"Registered service creator for provider: {normalizedName}");
        }
        
        /// <summary>
        /// Creates an AI service for the specified model name
        /// </summary>
        public IAIService CreateService(string modelName)
        {
            if (string.IsNullOrEmpty(modelName))
                throw new ArgumentException("Model name cannot be empty", nameof(modelName));
                
            // Try to get from cache first
            var cacheKey = $"{SERVICE_CACHE_KEY}{modelName}";
            if (_cache.TryGetValue(cacheKey, out IAIService cachedService))
            {
                Debug.WriteLine($"Retrieved service from cache for model: {modelName}");
                return cachedService;
            }
            
            try
            {
                // Ensure initialization
                EnsureInitializedAsync().Wait();
                
                // Get model configuration from database through repository
                var config = GetModelConfigurationAsync(modelName).Result;
                if (config == null)
                {
                    Debug.WriteLine($"No configuration found for model: {modelName}");
                    throw new InvalidOperationException($"No model configuration found for {modelName}");
                }
                
                // Create service based on configuration
                IAIService service = CreateServiceFromConfiguration(config);
                
                // Cache the service
                _cache.Set(cacheKey, service, _serviceCacheTime);
                
                return service;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating service for model {modelName}: {ex.Message}");
                return CreateFallbackService(modelName);
            }
        }
        
        /// <summary>
        /// Gets a model configuration by name
        /// </summary>
        private async Task<ModelConfiguration> GetModelConfigurationAsync(string modelName)
        {
            // Try cache first
            string configCacheKey = $"{CONFIG_CACHE_KEY}{modelName}";
            if (_cache.TryGetValue(configCacheKey, out ModelConfiguration cachedConfig))
                return cachedConfig;
                
            // Load from repository
            ModelConfiguration config;
            
            // Try to find by exact model identifier first
            config = await _repository.GetByModelIdentifierAsync(modelName);
            
            // If not found, try with provider/model parsing
            if (config == null && modelName.Contains('/'))
            {
                var parts = modelName.Split('/', 2);
                if (parts.Length == 2)
                {
                    config = await _repository.GetByProviderAndModelAsync(parts[0], parts[1]);
                }
            }
            
            // If still not found, try a search
            if (config == null)
            {
                var allConfigs = await _repository.GetAllAsync();
                config = allConfigs.FirstOrDefault(c => 
                    c.ModelIdentifier.Equals(modelName, StringComparison.OrdinalIgnoreCase) ||
                    c.ModelId.Equals(modelName, StringComparison.OrdinalIgnoreCase));
            }
            
            if (config != null)
            {
                // Cache the configuration
                _cache.Set(configCacheKey, config, _configCacheTime);
            }
            
            return config;
        }
        
        /// <summary>
        /// Creates a service from a model configuration with efficiency improvements
        /// </summary>
        private IAIService CreateServiceFromConfiguration(ModelConfiguration config)
        {
            string providerNameLower = NormalizeProviderName(config.ProviderName);
            
            // Log service creation attempt
            Debug.WriteLine($"Creating service for provider: {providerNameLower}, model: {config.ModelIdentifier}");
            var sw = Stopwatch.StartNew();
            
            try
            {
                // Try function-based creation first (most efficient path)
                if (_serviceCreators.TryGetValue(providerNameLower, out var creator))
                {
                    var service = creator(config.ModelIdentifier, config.Capabilities);
                    
                    sw.Stop();
                    Debug.WriteLine($"Created service using function creator in {sw.ElapsedMilliseconds}ms");
                    
                    return service;
                }
                
                // Try reflection-based creation next with cached type
                if (_serviceTypes.TryGetValue(providerNameLower, out var serviceType))
                {
                    var service = CreateServiceByReflection(serviceType, config);
                    
                    sw.Stop();
                    Debug.WriteLine($"Created service using reflection in {sw.ElapsedMilliseconds}ms");
                    
                    return service;
                }
                
                // Check if we've already tried to discover this type
                if (_discoveredTypes.TryGetValue(providerNameLower, out var discoveredType))
                {
                    if (discoveredType != null)
                    {
                        var service = CreateServiceByReflection(discoveredType, config);
                        
                        sw.Stop();
                        Debug.WriteLine($"Created service using previously discovered type in {sw.ElapsedMilliseconds}ms");
                        
                        return service;
                    }
                    
                    // We've already attempted discovery and found nothing
                    throw new InvalidOperationException($"No service creator registered for provider {config.ProviderName}");
                }
                
                // Try dynamic discovery as last resort (expensive)
                var newlyDiscoveredType = DiscoverServiceType(providerNameLower);
                _discoveredTypes[providerNameLower] = newlyDiscoveredType; // Cache result even if null
                
                if (newlyDiscoveredType != null)
                {
                    var service = CreateServiceByReflection(newlyDiscoveredType, config);
                    
                    sw.Stop();
                    Debug.WriteLine($"Created service using newly discovered type in {sw.ElapsedMilliseconds}ms");
                    
                    // Register for future use
                    _serviceTypes[providerNameLower] = newlyDiscoveredType;
                    
                    return service;
                }
                
                throw new InvalidOperationException($"No service creator registered for provider {config.ProviderName}");
            }
            catch (Exception ex)
            {
                sw.Stop();
                Debug.WriteLine($"Failed to create service in {sw.ElapsedMilliseconds}ms: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Creates a service using reflection
        /// </summary>
        private IAIService CreateServiceByReflection(Type serviceType, ModelConfiguration config)
        {
            try
            {
                // Try to find constructor(apiKeyManager, modelName)
                var constructor = serviceType.GetConstructor(new[] { typeof(IApiKeyManager), typeof(string) });
                if (constructor != null)
                {
                    return (IAIService)constructor.Invoke(new object[] { _apiKeyManager, config.ModelIdentifier });
                }
                
                // Try to find constructor(apiKeyManager, modelName, capabilities)
                constructor = serviceType.GetConstructor(new[] { typeof(IApiKeyManager), typeof(string), typeof(ModelCapabilities) });
                if (constructor != null)
                {
                    return (IAIService)constructor.Invoke(new object[] { _apiKeyManager, config.ModelIdentifier, config.Capabilities });
                }
                
                // Try to find constructor(apiKey, modelName)
                string apiKey = GetApiKeyForConfig(config);
                constructor = serviceType.GetConstructor(new[] { typeof(string), typeof(string) });
                if (constructor != null && !string.IsNullOrEmpty(apiKey))
                {
                    return (IAIService)constructor.Invoke(new[] { apiKey, config.ModelIdentifier });
                }
                
                // Fall back to parameterless constructor if available
                constructor = serviceType.GetConstructor(Type.EmptyTypes);
                if (constructor != null)
                {
                    var service = (IAIService)constructor.Invoke(Array.Empty<object>());
                    
                    // Try to set properties
                    var modelNameProperty = serviceType.GetProperty("ModelName");
                    if (modelNameProperty?.CanWrite == true)
                    {
                        modelNameProperty.SetValue(service, config.ModelIdentifier);
                    }
                    
                    return service;
                }
                
                throw new InvalidOperationException($"No suitable constructor found for {serviceType.Name}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating service by reflection: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Attempts to discover a service type dynamically for a provider (optimized)
        /// </summary>
        private Type DiscoverServiceType(string providerName)
        {
            try
            {
                // Try to find a type that matches the naming pattern
                var formattedName = char.ToUpperInvariant(providerName[0]) + providerName.Substring(1) + "AIService";
                
                // Search in current assembly first (most likely location)
                var assembly = Assembly.GetExecutingAssembly();
                var matchingType = FindTypeInAssembly(assembly, providerName, formattedName);
                if (matchingType != null)
                    return matchingType;
                
                // Only search in referenced assemblies if needed (expensive)
                var currentDomain = AppDomain.CurrentDomain;
                foreach (var loadedAssembly in currentDomain.GetAssemblies())
                {
                    if (loadedAssembly == assembly)
                        continue; // Skip already searched assembly
                        
                    try
                    {
                        var type = FindTypeInAssembly(loadedAssembly, providerName, formattedName);
                        if (type != null)
                            return type;
                    }
                    catch
                    {
                        // Skip assemblies that can't be searched
                        continue;
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error discovering service type: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Finds a type in an assembly (helper method for DiscoverServiceType)
        /// </summary>
        private Type FindTypeInAssembly(Assembly assembly, string providerName, string formattedName)
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch
            {
                // Some assemblies may throw exceptions when getting types
                return null;
            }
            
            // First look for exact match
            var exactMatch = types.FirstOrDefault(t => 
                typeof(IAIService).IsAssignableFrom(t) &&
                t.Name.Equals(formattedName, StringComparison.OrdinalIgnoreCase));
                
            if (exactMatch != null)
                return exactMatch;
                
            // Then try partial match
            return types.FirstOrDefault(t => 
                typeof(IAIService).IsAssignableFrom(t) && 
                t.Name.Contains(providerName, StringComparison.OrdinalIgnoreCase));
        }
        
        /// <summary>
        /// Normalizes a provider name for consistent lookup
        /// </summary>
        private string NormalizeProviderName(string providerName)
        {
            if (string.IsNullOrEmpty(providerName))
                return "dummy";
                
            return providerName.ToLowerInvariant();
        }
        
        /// <summary>
        /// Creates a fallback dummy service when other creation methods fail
        /// </summary>
        private IAIService CreateFallbackService(string modelName)
        {
            Debug.WriteLine($"Creating fallback service for model: {modelName}");
            return new DummyAIService(modelName);
        }
        
        /// <summary>
        /// Gets the API key for a model configuration
        /// </summary>
        private string GetApiKeyForConfig(ModelConfiguration config)
        {
            // Try model-specific key first
            string apiKey = null;
            
            if (!string.IsNullOrEmpty(config.ApiKeyEnvironmentVariable))
            {
                apiKey = _apiKeyManager.GetApiKey(config.ApiKeyEnvironmentVariable);
            }
            
            // Fall back to provider-level key
            if (string.IsNullOrEmpty(apiKey))
            {
                apiKey = _apiKeyManager.GetApiKey(config.ProviderName);
            }
            
            return apiKey;
        }
        
        /// <summary>
        /// Ensures the factory is initialized
        /// </summary>
        private async Task EnsureInitializedAsync()
        {
            if (!_initialized)
            {
                await InitializeAsync();
            }
        }
        
        /// <summary>
        /// Clears cached services for a provider with better key tracking
        /// </summary>
        private void ClearProviderCache(string providerName)
        {
            if (string.IsNullOrEmpty(providerName))
                return;
                
            // Get all cache entries
            var allModels = GetSupportedModels().ToList();
            
            // Clear any services for this provider
            foreach (var modelName in allModels)
            {
                try
                {
                    string cacheKey = $"{SERVICE_CACHE_KEY}{modelName}";
                    
                    // Check if this model belongs to the provider before removing
                    if (_cache.TryGetValue(cacheKey, out IAIService service) && 
                        service.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase))
                    {
                        _cache.Remove(cacheKey);
                    }
                }
                catch
                {
                    // Ignore errors when clearing cache
                }
            }
            
            // Also clear the provider cache key
            string providerCacheKey = $"{PROVIDER_CACHE_KEY}{providerName}";
            _cache.Remove(providerCacheKey);
        }
        
        /// <summary>
        /// Checks if this factory supports a provider
        /// </summary>
        public bool SupportsProvider(string providerName)
        {
            if (string.IsNullOrEmpty(providerName))
                return false;
                
            string normalizedName = NormalizeProviderName(providerName);
            return _serviceCreators.ContainsKey(normalizedName) || _serviceTypes.ContainsKey(normalizedName);
        }
        
        /// <summary>
        /// Gets capabilities for a model
        /// </summary>
        public ModelCapabilities GetCapabilities(string modelName)
        {
            try
            {
                var config = GetModelConfigurationAsync(modelName).Result;
                return config?.Capabilities ?? new ModelCapabilities();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting capabilities: {ex.Message}");
                return new ModelCapabilities();
            }
        }
        
        /// <summary>
        /// Gets the list of supported models
        /// </summary>
        public IEnumerable<string> GetSupportedModels()
        {
            try
            {
                return _repository.GetAllAsync().Result
                    .Where(c => c.IsEnabled)
                    .Select(c => c.ModelIdentifier);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting supported models: {ex.Message}");
                return Enumerable.Empty<string>();
            }
        }
        
        /// <summary>
        /// Gets provider metadata
        /// </summary>
        public IDictionary<string, string> GetProviderMetadata()
        {
            try
            {
                return _repository.GetAllAsync().Result
                    .GroupBy(c => NormalizeProviderName(c.ProviderName))
                    .ToDictionary(
                        g => g.Key, 
                        g => $"Provider with {g.Count()} models", 
                        StringComparer.OrdinalIgnoreCase
                    );
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting provider metadata: {ex.Message}");
                return new Dictionary<string, string>();
            }
        }
        
        /// <summary>
        /// Gets a list of all supported providers
        /// </summary>
        public IEnumerable<string> GetSupportedProviders()
        {
            // Combine providers from both registration methods
            var providers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var provider in _serviceCreators.Keys)
            {
                providers.Add(provider);
            }
            
            foreach (var provider in _serviceTypes.Keys)
            {
                providers.Add(provider);
            }
            
            // Add providers from discovered types
            foreach (var provider in _discoveredTypes.Keys)
            {
                providers.Add(provider);
            }
            
            return providers;
        }
        
        /// <summary>
        /// Creates a service asynchronously from provider and configuration
        /// </summary>
        public async Task<IAIService> CreateServiceAsync(AIProvider provider, ModelConfiguration config)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            
            // Try to get from cache first
            string cacheKey = $"{SERVICE_CACHE_KEY}{config.ModelIdentifier}_{provider.Name}";
            if (_cache.TryGetValue(cacheKey, out IAIService cachedService))
            {
                Debug.WriteLine($"Retrieved service from cache for model: {config.ModelIdentifier} with provider {provider.Name}");
                return cachedService;
            }
            
            try
            {
                // Ensure initialization is complete
                await EnsureInitializedAsync();
                
                // Create service using provider and configuration
                IAIService service = CreateServiceFromProviderConfig(provider, config);
                
                // Cache the service
                _cache.Set(cacheKey, service, _serviceCacheTime);
                
                return service;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating service for {provider.Name}/{config.ModelIdentifier}: {ex.Message}");
                return CreateFallbackService(config.ModelIdentifier);
            }
        }
        
        /// <summary>
        /// Creates a service using provider and model configuration
        /// </summary>
        private IAIService CreateServiceFromProviderConfig(AIProvider provider, ModelConfiguration config)
        {
            string normalizedProvider = NormalizeProviderName(provider.Name);
            var sw = Stopwatch.StartNew();
            
            try
            {
                // Try function-based creation first (fastest)
                if (_serviceCreators.TryGetValue(normalizedProvider, out var creator))
                {
                    var service = creator(config.ModelIdentifier, config.Capabilities);
                    sw.Stop();
                    Debug.WriteLine($"Created service using function creator in {sw.ElapsedMilliseconds}ms");
                    return service;
                }
                
                // Fall back to standard configuration-based creation
                return CreateServiceFromConfiguration(config);
            }
            catch (Exception ex)
            {
                sw.Stop();
                Debug.WriteLine($"Failed to create service in {sw.ElapsedMilliseconds}ms: {ex.Message}");
                throw;
            }
        }
        
        #region Service Creation Methods
        
        private IAIService CreateGroqService(string modelName, ModelCapabilities capabilities)
        {
            return new GroqAIService(_apiKeyManager, modelName);
        }
        
        private IAIService CreateOpenRouterService(string modelName, ModelCapabilities capabilities)
        {
            return new OpenRouterAIService(_apiKeyManager, modelName);
        }
        
        private IAIService CreateDummyService(string modelName, ModelCapabilities capabilities)
        {
            return new DummyAIService(modelName);
        }
        /*
        private IAIService CreateAzureService(string modelName, ModelCapabilities capabilities)
        {
            return new AzureAIService(_apiKeyManager, modelName);
        }
        */
        #endregion
    }
}
