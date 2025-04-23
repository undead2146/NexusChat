using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using NexusChat.Core.Models;
using NexusChat.Data.Interfaces;
using NexusChat.Services.Interfaces;

namespace NexusChat.Services.AIManagement
{
    /// <summary>
    /// Service for managing AI models and their selection
    /// </summary>
    public class ModelManager : IModelManager, IStartupInitializer
    {
        private readonly IAIServiceFactory _serviceFactory;
        private readonly IAIModelRepository _modelRepository;
        private readonly IModelLoaderService _modelLoaderService;
        private AIModel _currentModel;
        private IAIService _currentService;
        private int _defaultModelId;
        private const string DEFAULT_MODEL_PREFERENCE_KEY = "default_model_id";

        /// <summary>
        /// Gets the current model being used
        /// </summary>
        public AIModel CurrentModel => _currentModel;

        /// <summary>
        /// Gets the current service being used
        /// </summary>
        public IAIService CurrentService => _currentService;

        /// <summary>
        /// Gets or sets the default model ID
        /// </summary>
        public int DefaultModelId
        {
            get => _defaultModelId;
            set
            {
                _defaultModelId = value;
                SaveDefaultModelIdAsync(value).FireAndForget();
            }
        }

        /// <summary>
        /// Creates a new instance of the model manager
        /// </summary>
        public ModelManager(
            IAIModelRepository modelRepository,
            IAIServiceFactory serviceFactory,
            IModelLoaderService modelLoaderService)
        {
            _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
            _serviceFactory = serviceFactory ?? throw new ArgumentNullException(nameof(serviceFactory));
            _modelLoaderService = modelLoaderService ?? throw new ArgumentNullException(nameof(modelLoaderService));
        }

        /// <summary>
        /// Initializes the model manager
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                // Ensure the model loader service is initialized
                await _modelLoaderService.InitializeAsync();
                
                // Check if we have models in the database
                var existingModels = await GetAvailableModelsAsync();
                if (existingModels.Count == 0)
                {
                    // No models found, try to import from environment variables
                    Debug.WriteLine("No models found in database. Importing from environment variables...");
                    var envModels = await _modelLoaderService.ExtractModelsFromEnvironmentAsync();
                    
                    if (envModels.Count > 0)
                    {
                        // Import models to database
                        Debug.WriteLine($"Found {envModels.Count} models in environment variables. Adding to database...");
                        foreach (var model in envModels)
                        {
                            // Set default favorite state to false for imported models
                            model.IsFavourite = false;
                            await _modelRepository.AddModelAsync(model);
                        }
                        
                        // Refresh the model list after import
                        existingModels = await GetAvailableModelsAsync();
                        Debug.WriteLine($"Successfully imported {existingModels.Count} models from environment variables.");
                    }
                    else
                    {
                        Debug.WriteLine("No models found in environment variables. Creating fallback models...");
                        // Try to create fallback models
                        var fallbackConfigs = await _modelLoaderService.CreateFallbackConfigurationsAsync();
                        
                        // Convert fallback configurations to AIModels and add to database
                        foreach (var config in fallbackConfigs)
                        {
                            var fallbackModel = new AIModel
                            {
                                ModelName = config.ModelIdentifier,
                                ProviderName = config.ProviderName,
                                Description = config.Description,
                                IsAvailable = config.IsEnabled,
                                MaxTokens = config.Capabilities?.MaxTokens ?? 4096,
                                MaxContextWindow = config.Capabilities?.MaxContextWindow ?? 8192,
                                SupportsStreaming = config.Capabilities?.SupportsStreaming ?? true,
                                DefaultTemperature = config.Capabilities?.DefaultTemperature ?? 0.7f,
                                IsDefault = config.IsDefault,
                                IsFavourite = false // Initialize favorite status as false for new fallback models
                            };
                            
                            await _modelRepository.AddModelAsync(fallbackModel);
                        }
                        
                        // Refresh the model list after adding fallbacks
                        existingModels = await GetAvailableModelsAsync();
                    }
                }
                
                // Load the default model ID from preferences
                await LoadDefaultModelIdAsync();
                
                // If we have a default model ID, try to load that model
                if (_defaultModelId > 0)
                {
                    var model = await GetModelByIdAsync(_defaultModelId);
                    if (model != null)
                    {
                        await SetCurrentModelAsync(model);
                        return;
                    }
                }
                        
                // If no default model was found or loaded, get the default from database
                var defaultModel = await GetDefaultModelAsync();
                if (defaultModel != null)
                {
                    _defaultModelId = defaultModel.Id;
                    await SetCurrentModelAsync(defaultModel);
                    return;
                }
                
                // If no database default, try to get any model
                if (existingModels.Count > 0)
                {
                    // Log a warning but use the first available model
                    Debug.WriteLine("No default model found, using the first available model");
                    await SetCurrentModelAsync(existingModels[0]);
                    return;
                }
                
                // If all else fails
                Debug.WriteLine("WARNING: No models were found or created. Using a minimal dummy model for basic functionality");
                _currentModel = new AIModel
                {
                    Id = -1,
                    ModelName = "minimal-dummy",
                    ProviderName = "Dummy",
                    Description = "Minimal dummy model for fallback",
                    IsAvailable = true,
                    MaxTokens = 2048,
                    MaxContextWindow = 4096,
                    SupportsStreaming = true,
                    DefaultTemperature = 0.7f,
                    IsFavourite = false // Initialize favorite status as false
                };
                
                _currentService = _serviceFactory.CreateService("minimal-dummy");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing model manager: {ex.Message}");
                Debug.WriteLine(ex.StackTrace);
            }
        }

        /// <summary>
        /// Gets all available models
        /// </summary>
        public async Task<List<AIModel>> GetAvailableModelsAsync()
        {
            try
            {
                return await _modelRepository.GetAllModelsAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting available models: {ex.Message}");
                return new List<AIModel>();
            }
        }

        /// <summary>
        /// Gets a model by its ID
        /// </summary>
        public async Task<AIModel> GetModelByIdAsync(int id)
        {
            if (id <= 0)
                return null;

            try
            {
                return await _modelRepository.GetModelByIdAsync(id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting model by ID {id}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Updates model settings
        /// </summary>
        public async Task<bool> UpdateModelSettingsAsync(AIModel model)
        {
            if (model == null)
                return false;

            try
            {
                bool result = await _modelRepository.UpdateModelAsync(model);
                
                // If this is the current model, reload it
                if (_currentModel != null && _currentModel.Id == model.Id)
                {
                    _currentModel = model;
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating model settings: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sets the default model
        /// </summary>
        public async Task<bool> SetDefaultModelAsync(int modelId)
        {
            if (modelId <= 0)
                return false;

            try
            {
                // First check if the model exists
                var model = await GetModelByIdAsync(modelId);
                if (model == null)
                    return false;

                // Update in memory and preferences
                DefaultModelId = modelId;
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting default model: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets an AI service for a specific model
        /// </summary>
        public async Task<IAIService> GetServiceForModelAsync(int modelId)
        {
            try
            {
                // Get the model
                var model = await GetModelByIdAsync(modelId);
                if (model == null)
                    return null;
                
                // Create and return the service
                return _serviceFactory.CreateService(model.ModelName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating service for model {modelId}: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Sets the current model and creates the appropriate service
        /// </summary>
        public async Task<bool> SetCurrentModelAsync(AIModel model)
        {
            if (model == null)
                return false;
                
            try
            {
                _currentModel = model;
                _currentService = _serviceFactory.CreateService(model.ModelName);
                
                // If this model wasn't loaded from database (e.g., through API),
                // make sure it exists in the database for future use
                var existingModel = await GetModelByIdAsync(model.Id);
                if (existingModel == null)
                {
                    await _modelRepository.AddModelAsync(model);
                }
                
                return _currentService != null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting current model: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the default model from the database
        /// </summary>
        private async Task<AIModel> GetDefaultModelAsync()
        {
            try
            {
                // Get default configuration 
                var config = await _modelRepository.GetDefaultConfigurationAsync();
                if (config != null)
                {
                    // Get model by name/provider instead of using configuration directly
                    return await _modelRepository.GetModelByNameAsync(config.ProviderName, config.ModelIdentifier);
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting default model: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Loads the default model ID from preferences
        /// </summary>
        private async Task LoadDefaultModelIdAsync()
        {
            try
            {
                string storedId = await SecureStorage.GetAsync(DEFAULT_MODEL_PREFERENCE_KEY);
                if (!string.IsNullOrEmpty(storedId) && int.TryParse(storedId, out int id))
                {
                    _defaultModelId = id;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading default model ID: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves the default model ID to preferences
        /// </summary>
        private async Task SaveDefaultModelIdAsync(int modelId)
        {
            try
            {
                await SecureStorage.SetAsync(DEFAULT_MODEL_PREFERENCE_KEY, modelId.ToString());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving default model ID: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Extension method for fire-and-forget tasks
    /// </summary>
    internal static class TaskExtensions
    {
        public static void FireAndForget(this Task task)
        {
            // Simple fire-and-forget with error handling
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    var ex = t.Exception.InnerException ?? t.Exception;
                    Debug.WriteLine($"Fire and forget task error: {ex.Message}");
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
