using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Core.Models;
using NexusChat.Data.Interfaces;
using NexusChat.Services.Interfaces;
using DotNetEnv;

namespace NexusChat.Services.AIManagement
{
    /// <summary>
    /// Service for managing AI models and their configurations
    /// </summary>
    public class AIModelManager : IAIModelManager
    {
        private readonly IAIModelRepository _modelRepository;
        private readonly IApiKeyManager _apiKeyManager;
        private readonly IAIProviderFactory _providerFactory;
        private readonly SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);
        private bool _initialized = false;
        private int _initializeAttempts = 0;
        private const int MAX_INIT_ATTEMPTS = 3;
        
        public event EventHandler<AIModel> CurrentModelChanged;
        
        /// <summary>
        /// Gets the current selected model
        /// </summary>
        public AIModel CurrentModel { get; private set; }
        
        /// <summary>
        /// Creates a new instance of AIModelManager
        /// </summary>
        public AIModelManager(
            IAIModelRepository modelRepository,
            IApiKeyManager apiKeyManager,
            IAIProviderFactory providerFactory)
        {
            _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
            _apiKeyManager = apiKeyManager ?? throw new ArgumentNullException(nameof(apiKeyManager));
            _providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
        }
        
        /// <summary>
        /// Initializes the service with improved error handling and retry logic
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
                
                _initializeAttempts++;
                Debug.WriteLine($"AIModelManager: Initializing... (attempt {_initializeAttempts})");
                
                // Wait for API key manager initialization
                await _apiKeyManager.InitializeAsync();
                
                // Ensure we have some models in the database
                var models = await _modelRepository.GetAllAsync();
                Debug.WriteLine($"AIModelManager: Found {models.Count} models in database");
                
                if (models.Count == 0 || _initializeAttempts == 1)
                {
                    Debug.WriteLine("AIModelManager: No models found or first attempt - loading from environment");
                    await LoadModelsFromEnvironmentAsync();
                    
                    // Also load from provider factories
                    await LoadModelsFromProvidersAsync();
                    
                    // Reload models after loading from environment
                    models = await _modelRepository.GetAllAsync();
                    Debug.WriteLine($"AIModelManager: Now have {models.Count} models after loading");
                    
                    // Check providers for debugging
                    await LogModelBreakdownAsync();
                }
                
                // Get the current model
                var currentModel = await _modelRepository.GetCurrentModelAsync();
                if (currentModel == null)
                {
                    // No current model set, try to set a default
                    var defaultModels = await _modelRepository.GetDefaultModelsAsync();
                    Debug.WriteLine($"AIModelManager: Found {defaultModels.Count} default models");
                    
                    if (defaultModels.Count > 0)
                    {
                        currentModel = defaultModels[0];
                        bool success = await _modelRepository.SetCurrentModelAsync(currentModel);
                        Debug.WriteLine($"AIModelManager: Set default model as current: {currentModel.ProviderName}/{currentModel.ModelName}, " +
                                       $"Success: {success}");
                    }
                    else if (models.Count > 0)
                    {
                        // No default model, try to set the first one
                        currentModel = models[0];
                        bool success = await _modelRepository.SetCurrentModelAsync(currentModel);
                        Debug.WriteLine($"AIModelManager: Set first model as current: {currentModel.ProviderName}/{currentModel.ModelName}, " +
                                       $"Success: {success}");
                    }
                }
                
                CurrentModel = currentModel;
                Debug.WriteLine($"AIModelManager: Current model set to: {(currentModel != null ? $"{currentModel.ProviderName}/{currentModel.ModelName}" : "none")}");
                
                _initialized = true;
                Debug.WriteLine("AIModelManager: Initialization complete");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing AIModelManager: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Try to initialize again if we haven't exceeded max attempts
                if (_initializeAttempts < MAX_INIT_ATTEMPTS)
                {
                    _initialized = false;
                }
                else
                {
                    // Set initialized to true but with basic functionality
                    Debug.WriteLine("AIModelManager: Max initialization attempts reached, proceeding with limited functionality");
                    _initialized = true;
                }
            }
            finally
            {
                _initLock.Release();
            }
        }
        
        /// <summary>
        /// Gets all available models
        /// </summary>
        public async Task<List<AIModel>> GetAllModelsAsync()
        {
            await EnsureInitializedAsync();
            
            try
            {
                var models = await _modelRepository.GetAllAsync();
                Debug.WriteLine($"AIModelManager: Retrieved {models.Count} models");
                
                // If no models, try to load from providers
                if (models.Count == 0)
                {
                    Debug.WriteLine("AIModelManager: No models found, attempting to load from providers");
                    await LoadModelsFromProvidersAsync();
                    models = await _modelRepository.GetAllAsync();
                    Debug.WriteLine($"AIModelManager: After loading from providers: {models.Count} models");
                }
                
                return models;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting all models: {ex.Message}");
                return new List<AIModel>();
            }
        }
        
        /// <summary>
        /// Logs the breakdown of models by provider for debugging
        /// </summary>
        private async Task LogModelBreakdownAsync()
        {
            try
            {
                var allModels = await _modelRepository.GetAllAsync();
                var groqModels = allModels.Count(m => m.ProviderName.Equals("Groq", StringComparison.OrdinalIgnoreCase));
                var openRouterModels = allModels.Count(m => m.ProviderName.Equals("OpenRouter", StringComparison.OrdinalIgnoreCase));
                var dummyModels = allModels.Count(m => m.ProviderName.Equals("Dummy", StringComparison.OrdinalIgnoreCase));
                
                Debug.WriteLine($"Model counts - Groq: {groqModels}, OpenRouter: {openRouterModels}, Dummy: {dummyModels}");
                
                // Log a few models for validation
                int index = 0;
                foreach (var model in allModels.Take(10))
                {
                    Debug.WriteLine($"Loaded model: {model.ProviderName}/{model.ModelName}");
                    index++;
                }
                
                if (allModels.Count > 10)
                {
                    Debug.WriteLine($"... and {allModels.Count - 10} more models");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in LogModelBreakdownAsync: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Gets models for a specific provider
        /// </summary>
        public async Task<List<AIModel>> GetProviderModelsAsync(string providerName)
        {
            if (string.IsNullOrEmpty(providerName))
                return new List<AIModel>();
                
            await EnsureInitializedAsync();
            
            try
            {
                var models = await _modelRepository.GetByProviderAsync(providerName);
                Debug.WriteLine($"AIModelManager: Retrieved {models.Count} models for provider {providerName}");
                return models;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting models for provider {providerName}: {ex.Message}");
                return new List<AIModel>();
            }
        }
        
        /// <summary>
        /// Gets favorite models
        /// </summary>
        public async Task<List<AIModel>> GetFavoriteModelsAsync()
        {
            await EnsureInitializedAsync();
            
            try
            {
                var models = await _modelRepository.GetFavoriteModelsAsync();
                Debug.WriteLine($"AIModelManager: Retrieved {models.Count} favorite models");
                return models;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting favorite models: {ex.Message}");
                return new List<AIModel>();
            }
        }
        
        /// <summary>
        /// Sets current model with improved error handling
        /// </summary>
        public async Task<bool> SetCurrentModelAsync(AIModel model)
        {
            if (model == null)
                return false;
                
            await EnsureInitializedAsync();
            
            try
            {
                Debug.WriteLine($"AIModelManager: Setting current model to {model.ProviderName}/{model.ModelName}");
                
                // Try to find the model in the database first
                var dbModel = await _modelRepository.GetModelByNameAsync(model.ProviderName, model.ModelName);
                
                AIModel modelToUpdate;
                if (dbModel != null)
                {
                    // Use the database version to ensure we have the correct ID
                    modelToUpdate = dbModel;
                }
                else
                {
                    // Model not in DB yet, add it first
                    Debug.WriteLine($"Model {model.ProviderName}/{model.ModelName} not in database, adding it first");
                    model.Id = await _modelRepository.AddAsync(model);
                    modelToUpdate = model;
                }
                
                bool success = await _modelRepository.SetCurrentModelAsync(modelToUpdate);
                if (success)
                {
                    CurrentModel = modelToUpdate;
                    CurrentModelChanged?.Invoke(this, modelToUpdate);
                    Debug.WriteLine($"AIModelManager: Successfully set current model");
                    
                    // Record usage whenever we set a model as current
                    await _modelRepository.RecordUsageAsync(modelToUpdate.ProviderName, modelToUpdate.ModelName);
                    
                    return true;
                }
                else
                {
                    Debug.WriteLine($"AIModelManager: Failed to set current model");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting current model: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }
        
        /// <summary>
        /// Sets model as provider default
        /// </summary>
        public async Task<bool> SetDefaultModelAsync(string providerName, string modelName)
        {
            if (string.IsNullOrEmpty(providerName) || string.IsNullOrEmpty(modelName))
                return false;
                
            await EnsureInitializedAsync();
            
            try
            {
                Debug.WriteLine($"AIModelManager: Setting {providerName}/{modelName} as default");
                return await _modelRepository.SetAsDefaultAsync(providerName, modelName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting default model: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Sets favorite status with improved error handling
        /// </summary>
        public async Task<bool> SetFavoriteStatusAsync(string providerName, string modelName, bool isFavorite)
        {
            if (string.IsNullOrEmpty(providerName) || string.IsNullOrEmpty(modelName))
                return false;
                
            await EnsureInitializedAsync();
            
            try
            {
                Debug.WriteLine($"AIModelManager: Setting favorite status for {providerName}/{modelName} to {isFavorite}");
                
                // Try to find the model in the database first
                var dbModel = await _modelRepository.GetModelByNameAsync(providerName, modelName);
                
                // If model doesn't exist in database, add it first
                if (dbModel == null)
                {
                    Debug.WriteLine($"Model {providerName}/{modelName} not found in database, creating it first");
                    
                    // Check if the provider factory knows about this model
                    bool isKnownModel = false;
                    if (_providerFactory != null && _providerFactory.IsProviderAvailable(providerName))
                    {
                        var providerModels = _providerFactory.GetModelsForProvider(providerName);
                        var factoryModel = providerModels.FirstOrDefault(m => 
                            m.ModelName.Equals(modelName, StringComparison.OrdinalIgnoreCase));
                            
                        if (factoryModel != null)
                        {
                            isKnownModel = true;
                            factoryModel.IsFavorite = isFavorite;
                            await _modelRepository.AddAsync(factoryModel);
                            return true;
                        }
                    }
                    
                    if (!isKnownModel)
                    {
                        // Create a minimal model entry
                        var newModel = new AIModel
                        {
                            ProviderName = providerName,
                            ModelName = modelName,
                            DisplayName = modelName,
                            Description = $"{providerName} model: {modelName}",
                            IsFavorite = isFavorite,
                            IsAvailable = true,
                            MaxTokens = 4096,
                            MaxContextWindow = 8192,
                            SupportsStreaming = true
                        };
                        
                        await _modelRepository.AddAsync(newModel);
                        
                        // Use direct SQL update for safety
                        return await _modelRepository.SetFavoriteStatusAsync(providerName, modelName, isFavorite);
                    }
                }
                else
                {
                    // Use the direct method which is most reliable
                    bool success = await _modelRepository.SetFavoriteStatusAsync(providerName, modelName, isFavorite);
                    
                    if (success)
                    {
                        // Also update CurrentModel if it's the affected model
                        if (CurrentModel != null && 
                            CurrentModel.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase) &&
                            CurrentModel.ModelName.Equals(modelName, StringComparison.OrdinalIgnoreCase))
                        {
                            CurrentModel.IsFavorite = isFavorite;
                        }
                        
                        Debug.WriteLine($"AIModelManager: Successfully set favorite status");
                    }
                    else
                    {
                        Debug.WriteLine($"AIModelManager: Failed to set favorite status");
                    }
                    
                    return success;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting favorite status: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
            
            return false;
        }
        
        /// <summary>
        /// Records model usage
        /// </summary>
        public async Task<bool> RecordModelUsageAsync(string providerName, string modelName)
        {
            if (string.IsNullOrEmpty(providerName) || string.IsNullOrEmpty(modelName))
                return false;
                
            await EnsureInitializedAsync();
            
            try
            {
                Debug.WriteLine($"AIModelManager: Recording usage for {providerName}/{modelName}");
                return await _modelRepository.RecordUsageAsync(providerName, modelName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error recording model usage: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Ensures the manager is initialized
        /// </summary>
        private void EnsureInitialized()
        {
            if (!_initialized)
            {
                InitializeAsync().Wait();
            }
        }
        
        /// <summary>
        /// Ensures the manager is initialized asynchronously
        /// </summary>
        private async Task EnsureInitializedAsync()
        {
            if (!_initialized)
            {
                await InitializeAsync();
            }
        }
        
        /// <summary>
        /// Loads models from the environment variables
        /// </summary>
        private async Task LoadModelsFromEnvironmentAsync()
        {
            try
            {
                Debug.WriteLine("AIModelManager: Loading models from environment...");
                
                // First load models from the database
                var existingModels = await _modelRepository.GetAllAsync();
                var existingModelKeys = existingModels
                    .Select(m => $"{m.ProviderName.ToLowerInvariant()}:{m.ModelName.ToLowerInvariant()}")
                    .ToHashSet();
                
                Debug.WriteLine($"AIModelManager: Found {existingModels.Count} existing models in database");
                
                // Make sure DotNetEnv is loaded
                Env.Load();
                Debug.WriteLine("AIModelManager: Loaded .env file if available");
                
                // Load all models from both services
                await LoadGroqModelsAsync(existingModelKeys);
                await LoadOpenRouterModelsAsync(existingModelKeys);
                await LoadDummyModelsAsync(existingModelKeys);
                
                // Re-check counts after loading
                var updatedModels = await _modelRepository.GetAllAsync();
                
                // Count models by provider for debugging
                var groqCount = updatedModels.Count(m => m.ProviderName.Equals("Groq", StringComparison.OrdinalIgnoreCase));
                var openRouterCount = updatedModels.Count(m => m.ProviderName.Equals("OpenRouter", StringComparison.OrdinalIgnoreCase));
                var dummyCount = updatedModels.Count(m => m.ProviderName.Equals("Dummy", StringComparison.OrdinalIgnoreCase));
                
                Debug.WriteLine($"AIModelManager: After loading - Total={updatedModels.Count}, Groq={groqCount}, OpenRouter={openRouterCount}, Dummy={dummyCount}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading models from environment: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// Loads models from provider factories with significant performance optimizations
        /// </summary>
        private async Task LoadModelsFromProvidersAsync()
        {
            try
            {
                Debug.WriteLine("Loading models from AI providers...");
                
                if (_providerFactory == null)
                {
                    Debug.WriteLine("Provider factory is null, cannot load models from providers");
                    return;
                }
                
                // Get only providers that have valid API keys
                var activeProviders = await _providerFactory.GetActiveProvidersAsync();
                
                if (activeProviders.Count == 0)
                {
                    Debug.WriteLine("No providers with API keys available");
                    return;
                }
                
                Debug.WriteLine($"Found {activeProviders.Count} active providers with API keys");
                
                // Get models only for active providers
                foreach (var providerName in activeProviders)
                {
                    try
                    {
                        Debug.WriteLine($"Loading models for provider: {providerName}");
                        var models = await _providerFactory.GetModelsForProviderAsync(providerName);
                        
                        if (models != null && models.Count > 0)
                        {
                            Debug.WriteLine($"Found {models.Count} models for provider {providerName}");
                            
                            // Process in batches for better performance
                            for (int i = 0; i < models.Count; i += 10)
                            {
                                var batch = models.Skip(i).Take(10);
                                
                                foreach (var model in batch)
                                {
                                    try
                                    {
                                        var existingModel = await _modelRepository.GetModelByNameAsync(
                                            model.ProviderName, model.ModelName);
                                            
                                        if (existingModel == null)
                                        {
                                            int id = await _modelRepository.AddAsync(model);
                                            Debug.WriteLine($"Added model {model.ProviderName}/{model.ModelName} with ID {id}");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"Error adding model {model.ProviderName}/{model.ModelName}: {ex.Message}");
                                    }
                                }
                                
                                // Small delay between batches to not block UI thread
                                await Task.Delay(10);
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"No models found for provider {providerName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error loading models for provider {providerName}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in LoadModelsFromProvidersAsync: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// Loads OpenRouter models from environment variables
        /// </summary>
        private async Task LoadOpenRouterModelsAsync(HashSet<string> existingModelKeys)
        {
            try
            {
                Debug.WriteLine("AIModelManager: Loading OpenRouter models from environment...");
                
                // Get all environment variables with the OpenRouter model prefix
                var modelVars = GetEnvironmentVariableKeys("AI_MODEL_OPENROUTER_");
                Debug.WriteLine($"AIModelManager: Found {modelVars.Count} OpenRouter model environment variables");
                
                // Create a counter to track added models
                int addedModelCount = 0;
                
                foreach (var modelVar in modelVars)
                {
                    try
                    {
                        // Extract model name and ID from environment variable
                        string modelEnvSuffix = modelVar.Substring("AI_MODEL_OPENROUTER_".Length);
                        string modelId = Environment.GetEnvironmentVariable(modelVar);
                        
                        if (string.IsNullOrEmpty(modelId))
                        {
                            Debug.WriteLine($"AIModelManager: Empty model ID for {modelVar}, skipping");
                            continue;
                        }
                        
                        // Use the model ID as is (OpenRouter models often contain slashes)
                        string normalizedModelName = modelId.ToLowerInvariant();
                        
                        // Check if we already have this model
                        string modelKey = $"openrouter:{normalizedModelName}";
                        if (existingModelKeys.Contains(modelKey))
                        {
                            Debug.WriteLine($"AIModelManager: Model {normalizedModelName} already exists, skipping");
                            continue;
                        }
                        
                        Debug.WriteLine($"AIModelManager: Adding OpenRouter model: {normalizedModelName}");
                        
                        // Get model description if available
                        string descEnvVar = $"AI_MODEL_DESC_OPENROUTER_{modelEnvSuffix}";
                        string description = Environment.GetEnvironmentVariable(descEnvVar) ?? 
                                            $"OpenRouter model: {normalizedModelName}";
                        
                        // Get model capabilities
                        int maxTokens = 4096;
                        int contextWindow = 16384;
                        bool supportsStreaming = true;
                        bool supportsVision = false;
                        bool supportsCodeCompletion = false;
                        
                        // Parse capabilities from environment if available
                        if (int.TryParse(Environment.GetEnvironmentVariable($"AI_MODEL_CAP_OPENROUTER_{modelEnvSuffix}_TOKENS"), out int tokens))
                            maxTokens = tokens;
                            
                        if (int.TryParse(Environment.GetEnvironmentVariable($"AI_MODEL_CAP_OPENROUTER_{modelEnvSuffix}_CONTEXT"), out int ctx))
                            contextWindow = ctx;
                            
                        if (bool.TryParse(Environment.GetEnvironmentVariable($"AI_MODEL_CAP_OPENROUTER_{modelEnvSuffix}_STREAMING"), out bool streaming))
                            supportsStreaming = streaming;
                            
                        if (bool.TryParse(Environment.GetEnvironmentVariable($"AI_MODEL_CAP_OPENROUTER_{modelEnvSuffix}_VISION"), out bool vision))
                            supportsVision = vision;
                            
                        if (bool.TryParse(Environment.GetEnvironmentVariable($"AI_MODEL_CAP_OPENROUTER_{modelEnvSuffix}_CODE"), out bool code))
                            supportsCodeCompletion = code;
                        
                        // Create and save the model
                        var model = new AIModel
                        {
                            ModelName = normalizedModelName,
                            ProviderName = "OpenRouter",
                            Description = description,
                            MaxTokens = maxTokens,
                            MaxContextWindow = contextWindow,
                            SupportsStreaming = supportsStreaming,
                            SupportsVision = supportsVision,
                            SupportsCodeCompletion = supportsCodeCompletion,
                            IsAvailable = true,
                            ApiKeyVariable = $"AI_KEY_OPENROUTER_{modelEnvSuffix}",
                            DisplayName = normalizedModelName.Split('/').LastOrDefault() ?? normalizedModelName,
                            IsFavorite = false,
                            IsSelected = false,
                            IsDefault = false,
                            UsageCount = 0,
                            LastUsed = null
                        };
                        
                        // Add to database
                        var id = await _modelRepository.AddAsync(model);
                        Debug.WriteLine($"AIModelManager: Added OpenRouter model {normalizedModelName} with ID {id}");
                        
                        existingModelKeys.Add(modelKey);
                        addedModelCount++;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error adding OpenRouter model {modelVar}: {ex.Message}");
                    }
                }
                
                Debug.WriteLine($"AIModelManager: Added {addedModelCount} OpenRouter models from environment variables");
                
                // Add core OpenRouter models explicitly if needed
                string[] coreModels = new[]
                {
                    "anthropic/claude-3-opus",
                    "anthropic/claude-3-sonnet",
                    "google/gemini-pro",
                    "mistral-small-3-1-24b"
                };
                
                int addedCoreModelCount = 0;
                
                foreach (var modelName in coreModels)
                {
                    string modelKey = $"openrouter:{modelName.ToLowerInvariant()}";
                    if (!existingModelKeys.Contains(modelKey))
                    {
                        try
                        {
                            var model = new AIModel
                            {
                                ModelName = modelName,
                                ProviderName = "OpenRouter",
                                Description = $"OpenRouter model: {modelName}",
                                MaxTokens = 4096,
                                MaxContextWindow = 16384,
                                SupportsStreaming = true,
                                IsAvailable = true,
                                ApiKeyVariable = "AI_KEY_OPENROUTER",
                                DisplayName = modelName.Split('/').LastOrDefault() ?? modelName,
                                IsFavorite = false,
                                IsSelected = false,
                                IsDefault = false,
                                UsageCount = 0
                            };
                            
                            var id = await _modelRepository.AddAsync(model);
                            Debug.WriteLine($"AIModelManager: Added core OpenRouter model {modelName} with ID {id}");
                            
                            existingModelKeys.Add(modelKey);
                            addedCoreModelCount++;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error adding core OpenRouter model {modelName}: {ex.Message}");
                        }
                    }
                }
                
                Debug.WriteLine($"AIModelManager: Added {addedCoreModelCount} core OpenRouter models");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in LoadOpenRouterModelsAsync: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// Loads Groq models from environment variables
        /// </summary>
        private async Task LoadGroqModelsAsync(HashSet<string> existingModelKeys)
        {
            try
            {
                Debug.WriteLine("AIModelManager: Loading Groq models from environment...");
                
                // Get all environment variables with the Groq model prefix
                var modelVars = GetEnvironmentVariableKeys("AI_MODEL_GROQ_");
                Debug.WriteLine($"AIModelManager: Found {modelVars.Count} Groq model environment variables");
                
                // Create a counter to track added models  
                int addedModelCount = 0;
                
                foreach (var modelVar in modelVars)
                {
                    try
                    {
                        // Extract model name and ID from environment variable
                        string modelEnvSuffix = modelVar.Substring("AI_MODEL_GROQ_".Length);
                        string modelId = Environment.GetEnvironmentVariable(modelVar);
                        
                        if (string.IsNullOrEmpty(modelId))
                        {
                            Debug.WriteLine($"AIModelManager: Empty model ID for {modelVar}, skipping");
                            continue;
                        }
                        
                        // Convert underscore format to dash format for display
                        string normalizedModelName = modelId.Replace('_', '-').ToLowerInvariant();
                        
                        // Check if we already have this model
                        string modelKey = $"groq:{normalizedModelName}";
                        if (existingModelKeys.Contains(modelKey))
                        {
                            Debug.WriteLine($"AIModelManager: Model {normalizedModelName} already exists, skipping");
                            continue;
                        }
                        
                        Debug.WriteLine($"AIModelManager: Adding Groq model: {normalizedModelName}");
                        
                        // Get model description if available
                        string descEnvVar = $"AI_MODEL_DESC_GROQ_{modelEnvSuffix}";
                        string description = Environment.GetEnvironmentVariable(descEnvVar) ?? 
                                            $"Groq model: {normalizedModelName}";
                        
                        // Get model capabilities
                        int maxTokens = 4096;
                        int contextWindow = 8192;
                        bool supportsStreaming = true;
                        bool supportsVision = false;
                        bool supportsCodeCompletion = false;
                        
                        // Parse capabilities from environment if available
                        if (int.TryParse(Environment.GetEnvironmentVariable($"AI_MODEL_CAP_GROQ_{modelEnvSuffix}_TOKENS"), out int tokens))
                            maxTokens = tokens;
                            
                        if (int.TryParse(Environment.GetEnvironmentVariable($"AI_MODEL_CAP_GROQ_{modelEnvSuffix}_CONTEXT"), out int ctx))
                            contextWindow = ctx;
                            
                        if (bool.TryParse(Environment.GetEnvironmentVariable($"AI_MODEL_CAP_GROQ_{modelEnvSuffix}_STREAMING"), out bool streaming))
                            supportsStreaming = streaming;
                            
                        if (bool.TryParse(Environment.GetEnvironmentVariable($"AI_MODEL_CAP_GROQ_{modelEnvSuffix}_VISION"), out bool vision))
                            supportsVision = vision;
                            
                        if (bool.TryParse(Environment.GetEnvironmentVariable($"AI_MODEL_CAP_GROQ_{modelEnvSuffix}_CODE"), out bool code))
                            supportsCodeCompletion = code;
                        
                        // Create and save the model
                        var model = new AIModel
                        {
                            ModelName = normalizedModelName,
                            ProviderName = "Groq",
                            Description = description,
                            MaxTokens = maxTokens,
                            MaxContextWindow = contextWindow,
                            SupportsStreaming = supportsStreaming,
                            SupportsVision = supportsVision,
                            SupportsCodeCompletion = supportsCodeCompletion,
                            IsAvailable = true,
                            ApiKeyVariable = $"AI_KEY_GROQ_{modelEnvSuffix}",
                            DisplayName = normalizedModelName,
                            IsFavorite = false,
                            IsSelected = false,
                            IsDefault = false,
                            UsageCount = 0,
                            LastUsed = null
                        };
                        
                        // Add to database
                        var id = await _modelRepository.AddAsync(model);
                        Debug.WriteLine($"AIModelManager: Added Groq model {normalizedModelName} with ID {id}");
                        
                        existingModelKeys.Add(modelKey);
                        addedModelCount++;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error adding Groq model {modelVar}: {ex.Message}");
                    }
                }
                
                Debug.WriteLine($"AIModelManager: Added {addedModelCount} Groq models from environment variables");
                
                // Add core models explicitly if needed
                string[] coreModels = new[]
                {
                    "llama3-70b-8192",
                    "llama3-8b-8192",
                    "mixtral-8x7b-32768",
                    "gemma-7b-it",
                    "llama-3-3-70b-versatile",
                    "llama-3-1-8b-instant"
                };
                
                int addedCoreModelCount = 0;
                
                foreach (var modelName in coreModels)
                {
                    string modelKey = $"groq:{modelName.ToLowerInvariant()}";
                    if (!existingModelKeys.Contains(modelKey))
                    {
                        try
                        {
                            var model = new AIModel
                            {
                                ModelName = modelName,
                                ProviderName = "Groq",
                                Description = $"Groq model: {modelName}",
                                MaxTokens = 4096,
                                MaxContextWindow = 8192,
                                SupportsStreaming = true,
                                IsAvailable = true,
                                ApiKeyVariable = "AI_KEY_GROQ",
                                DisplayName = modelName,
                                IsFavorite = false,
                                IsSelected = false,
                                IsDefault = false,
                                UsageCount = 0
                            };
                            
                            var id = await _modelRepository.AddAsync(model);
                            Debug.WriteLine($"AIModelManager: Added core Groq model {modelName} with ID {id}");
                            
                            existingModelKeys.Add(modelKey);
                            addedCoreModelCount++;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error adding core Groq model {modelName}: {ex.Message}");
                        }
                    }
                }
                
                Debug.WriteLine($"AIModelManager: Added {addedCoreModelCount} core Groq models");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in LoadGroqModelsAsync: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Gets environment variable keys with a specific prefix
        /// </summary>
        private List<string> GetEnvironmentVariableKeys(string prefix)
        {
            var result = new List<string>();
            
            try
            {
                foreach (string key in Environment.GetEnvironmentVariables().Keys)
                {
                    if (key != null && key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add(key);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting environment variable keys: {ex.Message}");
            }
            
            return result;
        }
        
        /// <summary>
        /// Loads dummy models for testing
        /// </summary>
        private async Task LoadDummyModelsAsync(HashSet<string> existingModelKeys)
        {
            try
            {
                Debug.WriteLine("AIModelManager: Loading dummy models for testing...");
                
                // Add a test model for development
                string modelKey = "dummy:dummygpt";
                if (!existingModelKeys.Contains(modelKey))
                {
                    try
                    {
                        var model = new AIModel
                        {
                            ModelName = "DummyGPT",
                            ProviderName = "Dummy",
                            Description = "A simulated model for testing without API calls",
                            MaxTokens = 4096,
                            MaxContextWindow = 8192,
                            SupportsStreaming = true,
                            IsAvailable = true,
                            ApiKeyVariable = "AI_KEY_DUMMY",
                            DisplayName = "Test Model (No API)",
                            IsFavorite = false,
                            IsSelected = false,
                            IsDefault = false,
                            UsageCount = 0
                        };
                        
                        var id = await _modelRepository.AddAsync(model);
                        Debug.WriteLine($"AIModelManager: Added dummy test model with ID {id}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error adding dummy model: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in LoadDummyModelsAsync: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads models from all providers for refresh
        /// </summary>
        public async Task<bool> LoadModelsFromAllProvidersAsync()
        {
            try
            {
                Debug.WriteLine("AIModelManager: Loading models from all providers...");
                await LoadModelsFromProvidersAsync();
                Debug.WriteLine("AIModelManager: Completed loading models from providers");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading models from providers: {ex.Message}");
                return false;
            }
        }
    }
}
