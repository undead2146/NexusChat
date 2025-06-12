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
        private readonly IAIModelDiscoveryService _modelDiscoveryService;
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
            IAIModelDiscoveryService modelDiscoveryService)
        {
            _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
            _apiKeyManager = apiKeyManager ?? throw new ArgumentNullException(nameof(apiKeyManager));
            _modelDiscoveryService = modelDiscoveryService ?? throw new ArgumentNullException(nameof(modelDiscoveryService));
        }
        
        /// <summary>
        /// Initialize the service 
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
                Debug.WriteLine($"AIModelManager: Initializing without discovery... (attempt {_initializeAttempts})");

                await _apiKeyManager.InitializeAsync();
                
                // Check existing models without triggering discovery
                var models = await GetExistingModelsOnlyAsync();
                Debug.WriteLine($"AIModelManager: Found {models.Count} existing models in database");
                
                // Get current model without triggering discovery
                var currentModel = await _modelRepository.GetCurrentModelAsync();
                if (currentModel == null && models.Count > 0)
                {
                    currentModel = models.FirstOrDefault(m => m.IsDefault) ?? models.First();
                    await _modelRepository.SetCurrentModelAsync(currentModel);
                    Debug.WriteLine($"AIModelManager: Set existing model as current: {currentModel.ProviderName}/{currentModel.ModelName}");
                }
                
                CurrentModel = currentModel;
                _initialized = true;
                Debug.WriteLine("AIModelManager: Fast initialization complete without discovery");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing AIModelManager: {ex.Message}");
                _initialized = true; // Set to true to prevent infinite retry loops
            }
            finally
            {
                _initLock.Release();
            }
        }

        /// <summary>
        /// Gets existing models from database only, no discovery 
        /// </summary>
        private async Task<List<AIModel>> GetExistingModelsOnlyAsync()
        {
            try
            {
                return await _modelRepository.GetAllAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetExistingModelsOnlyAsync: Error: {ex.Message}");
                return new List<AIModel>();
            }
        }

        /// <summary>
        /// Gets all available models filtered by providers with valid API keys
        /// </summary>
        public async Task<List<AIModel>> GetAllModelsAsync()
        {
            await EnsureInitializedAsync();
            
            try
            {
                // Get all models from database
                var allModels = await _modelRepository.GetAllAsync();
                Debug.WriteLine($"AIModelManager: Retrieved {allModels.Count} total models from database");
                
                // Filter models by providers that have valid API keys
                var filteredModels = await FilterModelsByAvailableProviders(allModels);
                
                Debug.WriteLine($"AIModelManager: Returning {filteredModels.Count} models after API key filtering");
                return filteredModels;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting all models: {ex.Message}");
                return new List<AIModel>();
            }
        }

        /// <summary>
        /// Filters models to only include those from providers with valid API keys
        /// </summary>
        private async Task<List<AIModel>> FilterModelsByAvailableProviders(List<AIModel> allModels)
        {
            if (allModels == null || allModels.Count == 0)
                return new List<AIModel>();

            try
            {
                // Get providers that have valid API keys
                var availableProviders = await GetProvidersWithApiKeys();
                
                if (availableProviders.Count == 0)
                {
                    Debug.WriteLine("AIModelManager: No providers with API keys found");
                    return new List<AIModel>();
                }

                // Filter models by available providers
                var filteredModels = allModels
                    .Where(model => availableProviders.Any(provider => 
                        string.Equals(model.ProviderName, provider, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                Debug.WriteLine($"AIModelManager: Filtered {allModels.Count} models down to {filteredModels.Count} for providers: {string.Join(", ", availableProviders)}");
                
                return filteredModels;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error filtering models by available providers: {ex.Message}");
                return allModels; // Return all models if filtering fails
            }
        }

        /// <summary>
        /// Gets list of providers that have valid API keys configured
        /// </summary>
        private async Task<List<string>> GetProvidersWithApiKeys()
        {
            var availableProviders = new List<string>();
            var knownProviders = new[] { "Groq", "OpenRouter", "Anthropic", "OpenAI", "Azure" };
            
            Debug.WriteLine("AIModelManager: Checking API keys for all known providers...");
            
            foreach (var provider in knownProviders)
            {
                try
                {
                    bool hasKey = await _apiKeyManager.HasActiveApiKeyAsync(provider);
                    string apiKey = await _apiKeyManager.GetApiKeyAsync(provider);
                    
                    Debug.WriteLine($"AIModelManager: Provider {provider} - HasKey: {hasKey}, KeyLength: {(string.IsNullOrEmpty(apiKey) ? 0 : apiKey.Length)}");
                    
                    if (hasKey)
                    {
                        availableProviders.Add(provider);
                        Debug.WriteLine($"AIModelManager: Added {provider} to available providers");
                    }
                    else
                    {
                        Debug.WriteLine($"AIModelManager: Provider {provider} has no valid API key");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error checking API key for {provider}: {ex.Message}");
                }
            }
            
            Debug.WriteLine($"AIModelManager: Final available providers: {string.Join(", ", availableProviders)} (Total: {availableProviders.Count})");
            return availableProviders;
        }

        /// <summary>
        /// Gets all models including those without API keys (for administrative purposes)
        /// </summary>
        public async Task<List<AIModel>> GetAllModelsUnfilteredAsync()
        {
            await EnsureInitializedAsync();
            
            try
            {
                var models = await _modelRepository.GetAllAsync();
                Debug.WriteLine($"AIModelManager: Retrieved {models.Count} unfiltered models from database");
                return models;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting unfiltered models: {ex.Message}");
                return new List<AIModel>();
            }
        }

        /// <summary>
        /// Discovers and loads models ONLY when explicitly called - STEP 12 FIX
        /// </summary>
        public async Task<bool> DiscoverAndLoadModelsAsync()
        {
            try
            {
                Debug.WriteLine("AIModelManager: Explicitly discovering models (not during init)");
                
                // Use the discovery service to get all models from all sources
                var discoveredModels = await _modelDiscoveryService.DiscoverAllModelsAsync();
                Debug.WriteLine($"AIModelManager: Discovered {discoveredModels.Count} models explicitly");
                
                if (discoveredModels.Count == 0)
                {
                    Debug.WriteLine("AIModelManager: No models discovered");
                    return false;
                }
                
                // Get existing models to avoid duplicates
                var existingModels = await _modelRepository.GetAllAsync();
                var existingModelKeys = existingModels
                    .Select(m => $"{m.ProviderName.ToLowerInvariant()}:{m.ModelName.ToLowerInvariant()}")
                    .ToHashSet();
                
                // Filter out models that already exist
                var newModels = discoveredModels
                    .Where(model => !existingModelKeys.Contains(
                        $"{model.ProviderName.ToLowerInvariant()}:{model.ModelName.ToLowerInvariant()}"))
                    .ToList();
                    
                Debug.WriteLine($"AIModelManager: Found {newModels.Count} truly new models to add");
                
                // Add new models in batches
                int addedCount = 0;
                const int batchSize = 20;
                
                for (int i = 0; i < newModels.Count; i += batchSize)
                {
                    var batch = newModels.Skip(i).Take(batchSize);
                    
                    foreach (var model in batch)
                    {
                        try
                        {
                            int id = await _modelRepository.AddAsync(model);
                            addedCount++;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error adding model {model.ProviderName}/{model.ModelName}: {ex.Message}");
                        }
                    }
                    
                    // Small delay between batches to prevent overwhelming database
                    if (i + batchSize < newModels.Count)
                    {
                        await Task.Delay(10);
                    }
                }
                
                Debug.WriteLine($"AIModelManager: Added {addedCount} new models from explicit discovery");
                return addedCount > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in explicit discovery and loading: {ex.Message}");
                return false;
            }
        }
        
        
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
        /// Sets favorite status with improved error handling and fixed SQLite queries
        /// </summary>
        public async Task<bool> SetFavoriteStatusAsync(string providerName, string modelName, bool isFavorite)
        {
            if (string.IsNullOrEmpty(providerName) || string.IsNullOrEmpty(modelName))
                return false;
                
            await EnsureInitializedAsync();
            
            try
            {
                Debug.WriteLine($"AIModelManager: Setting favorite status for {providerName}/{modelName} to {isFavorite}");
                
                // Use case-insensitive lookup to avoid SQLite query errors
                var dbModel = await FindModelByNameAsync(providerName, modelName);
                
                // If model doesn't exist in database, add it first
                if (dbModel == null)
                {
                    Debug.WriteLine($"Model {providerName}/{modelName} not found in database, discovering it first");
                    
                    // Try to discover the model via the discovery service
                    var discoveredModels = await _modelDiscoveryService.DiscoverProviderModelsAsync(providerName);
                    var discoveredModel = discoveredModels.FirstOrDefault(m => 
                        string.Equals(m.ModelName, modelName, StringComparison.OrdinalIgnoreCase));
                    
                    if (discoveredModel != null)
                    {
                        discoveredModel.IsFavorite = isFavorite;
                        await _modelRepository.AddAsync(discoveredModel);
                        Debug.WriteLine($"Successfully updated favorite status to {isFavorite}");
                        return true;
                    }
                    else
                    {
                        // Create a minimal model entry if we can't discover it
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
                            string.Equals(CurrentModel.ProviderName, providerName, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(CurrentModel.ModelName, modelName, StringComparison.OrdinalIgnoreCase))
                        {
                            CurrentModel.IsFavorite = isFavorite;
                        }
                        
                        Debug.WriteLine($"AIModelManager: Successfully updated favorite status to {isFavorite}");
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
        }

        /// <summary>
        /// Helper method to find model by name using case-insensitive comparison
        /// Avoids SQLite "no such function: equals" error by using in-memory filtering
        /// </summary>
        private async Task<AIModel> FindModelByNameAsync(string providerName, string modelName)
        {
            try
            {
                // Get all models and filter in memory to avoid SQLite function compatibility issues
                var allModels = await _modelRepository.GetAllAsync();
                return allModels.FirstOrDefault(m => 
                    string.Equals(m.ProviderName, providerName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(m.ModelName, modelName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error finding model by name: {ex.Message}");
                return null;
            }
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
        [Obsolete("This method performs sync-over-async operations which can lead to deadlocks. Use EnsureInitializedAsync instead.")]
        private void EnsureInitialized()
        {
            Debug.WriteLine("WARNING: Synchronous EnsureInitialized called. This can cause deadlocks and should be avoided.");
            
            // Check if already initialized to avoid potential deadlocks
            if (!_initialized)
            {
                // We won't call InitializeAsync().Wait() here anymore as it can deadlock
                // Just log a warning and return without initialization
                Debug.WriteLine("AIModelManager: Skipping initialization in synchronous context to prevent deadlocks.");
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
        /// Discovers and loads models for a specific provider
        /// </summary>
        public async Task<bool> DiscoverAndLoadProviderModelsAsync(string providerName)
        {
            if (string.IsNullOrEmpty(providerName))
                return false;
                
            try
            {
                Debug.WriteLine($"AIModelManager: Discovering models for provider {providerName}");
                
                // Use the discovery service to get models for this provider
                var discoveredModels = await _modelDiscoveryService.DiscoverProviderModelsAsync(providerName);
                Debug.WriteLine($"AIModelManager: Discovered {discoveredModels.Count} models for provider {providerName}");
                
                if (discoveredModels.Count == 0)
                {
                    Debug.WriteLine($"AIModelManager: No models discovered for {providerName}");
                    return false;
                }
                
                // Get existing models to avoid duplicates
                var existingModels = await _modelRepository.GetByProviderAsync(providerName);
                var existingModelNames = existingModels
                    .Select(m => m.ModelName.ToLowerInvariant())
                    .ToHashSet();
                
                // Add all discovered models that don't already exist
                int addedCount = 0;
                foreach (var model in discoveredModels)
                {
                    if (!existingModelNames.Contains(model.ModelName.ToLowerInvariant()))
                    {
                        try
                        {
                            int id = await _modelRepository.AddAsync(model);
                            Debug.WriteLine($"Added model {model.ProviderName}/{model.ModelName} with ID {id}");
                            addedCount++;
                            existingModelNames.Add(model.ModelName.ToLowerInvariant());
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error adding model {model.ProviderName}/{model.ModelName}: {ex.Message}");
                        }
                    }
                }
                
                Debug.WriteLine($"AIModelManager: Added {addedCount} new models for provider {providerName}");
                return addedCount > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error discovering models for provider {providerName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Processes a list of discovered models 
        /// </summary>
        public async Task<bool> ProcessDiscoveredModelsAsync(List<AIModel> discoveredModels)
        {
            if (discoveredModels == null || discoveredModels.Count == 0)
            {
                Debug.WriteLine("AIModelManager: No discovered models provided to process");
                return false;
            }

            try
            {
                Debug.WriteLine($"AIModelManager: Processing {discoveredModels.Count} discovered models with enhanced deduplication");
                
                // Pre-filter discovered models to remove duplicates BEFORE database operations
                var uniqueDiscoveredModels = await Task.Run(() =>
                {
                    return discoveredModels
                        .Where(m => !string.IsNullOrWhiteSpace(m.ProviderName) && !string.IsNullOrWhiteSpace(m.ModelName))
                        .GroupBy(m => new { 
                            Provider = m.ProviderName?.Trim().ToLowerInvariant(), 
                            Model = m.ModelName?.Trim().ToLowerInvariant() 
                        })
                        .Select(g => g.First())
                        .ToList();
                });
                
                if (uniqueDiscoveredModels.Count != discoveredModels.Count)
                {
                    Debug.WriteLine($"AIModelManager: Removed {discoveredModels.Count - uniqueDiscoveredModels.Count} duplicates from discovery");
                }
                
                // Clean up database duplicates first
                await CleanupDatabaseDuplicates();
                
                // Get existing models with optimized query
                var existingModels = await _modelRepository.GetAllAsync();
                var existingModelKeys = new HashSet<string>(
                    existingModels.Select(m => $"{m.ProviderName?.Trim().ToLowerInvariant()}:{m.ModelName?.Trim().ToLowerInvariant()}"),
                    StringComparer.Ordinal);
                
                // Filter out models that already exist
                var newModels = uniqueDiscoveredModels
                    .Where(model => !existingModelKeys.Contains(
                        $"{model.ProviderName?.Trim().ToLowerInvariant()}:{model.ModelName?.Trim().ToLowerInvariant()}"))
                    .ToList();
                    
                Debug.WriteLine($"AIModelManager: Found {newModels.Count} genuinely new models to add");
                
                if (newModels.Count == 0)
                {
                    Debug.WriteLine("AIModelManager: No new models to add - skipping database operations");
                    return false;
                }
                
                // Add new models in optimized batches
                int addedCount = 0;
                const int batchSize = 10;
                
                for (int i = 0; i < newModels.Count; i += batchSize)
                {
                    var batch = newModels.Skip(i).Take(batchSize);
                    
                    var addTasks = batch.Select(async model =>
                    {
                        try
                        {
                            int id = await _modelRepository.AddAsync(model);
                            Debug.WriteLine($"AIModelManager: Added model {model.ProviderName}/{model.ModelName} with ID {id}");
                            return 1;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"AIModelManager: Error adding model {model.ProviderName}/{model.ModelName}: {ex.Message}");
                            return 0;
                        }
                    });
                    
                    var results = await Task.WhenAll(addTasks);
                    addedCount += results.Sum();
                    
                    if (i + batchSize < newModels.Count)
                    {
                        await Task.Delay(5);
                    }
                }
                
                Debug.WriteLine($"AIModelManager: Successfully processed and added {addedCount} new models");
                return addedCount > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AIModelManager: Error in ProcessDiscoveredModelsAsync: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Cleans up duplicate entries in the database
        /// </summary>
        private async Task CleanupDatabaseDuplicates()
        {
            try
            {
                Debug.WriteLine("AIModelManager: Starting database duplicate cleanup");
                
                var allModels = await _modelRepository.GetAllAsync();
                var duplicateGroups = allModels
                    .GroupBy(m => new { 
                        Provider = m.ProviderName?.Trim().ToLowerInvariant(), 
                        Model = m.ModelName?.Trim().ToLowerInvariant() 
                    })
                    .Where(g => g.Count() > 1)
                    .ToList();
                
                if (duplicateGroups.Count == 0)
                {
                    Debug.WriteLine("AIModelManager: No duplicates found in database");
                    return;
                }
                
                Debug.WriteLine($"AIModelManager: Found {duplicateGroups.Count} duplicate groups");
                
                foreach (var group in duplicateGroups)
                {
                    var modelsToKeep = group.OrderByDescending(m => m.LastUsed ?? DateTime.MinValue)
                                          .ThenByDescending(m => m.UsageCount)
                                          .ThenByDescending(m => m.IsFavorite)
                                          .First();
                    
                    var modelsToDelete = group.Where(m => m.Id != modelsToKeep.Id).ToList();
                    
                    foreach (var modelToDelete in modelsToDelete)
                    {
                        try
                        {
                            
                            await _modelRepository.DeleteAsync(modelToDelete, CancellationToken.None);  
                            Debug.WriteLine($"AIModelManager: Deleted duplicate {modelToDelete.ProviderName}/{modelToDelete.ModelName} (ID: {modelToDelete.Id})");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"AIModelManager: Error deleting duplicate model {modelToDelete.Id}: {ex.Message}");
                        }
                    }
                }
                
                Debug.WriteLine("AIModelManager: Database duplicate cleanup completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AIModelManager: Error during database cleanup: {ex.Message}");
            }
        }
    }
}
