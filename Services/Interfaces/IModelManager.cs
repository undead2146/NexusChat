using System.Collections.Generic;
using System.Threading.Tasks;
using NexusChat.Core.Models;
using NexusChat.Services.Interfaces;

namespace NexusChat.Services.Interfaces
{
    /// <summary>
    /// Interface for managing AI models
    /// </summary>
    public interface IModelManager
    {
        /// <summary>
        /// Gets the ID of the default AI model
        /// </summary>
        int DefaultModelId { get; }
        
        /// <summary>
        /// Gets the current AI model being used
        /// </summary>
        AIModel CurrentModel { get; }
        
        /// <summary>
        /// Gets the current AI service
        /// </summary>
        IAIService CurrentService { get; }
        
        /// <summary>
        /// Gets all available AI models
        /// </summary>
        /// <returns>List of available models</returns>
        Task<List<AIModel>> GetAvailableModelsAsync();
        
        /// <summary>
        /// Gets a specific AI model by ID
        /// </summary>
        /// <param name="id">The model ID</param>
        /// <returns>The requested model or null if not found</returns>
        Task<AIModel> GetModelByIdAsync(int id);
        
        /// <summary>
        /// Updates the settings for a model
        /// </summary>
        /// <param name="model">The model with updated settings</param>
        /// <returns>True if successful</returns>
        Task<bool> UpdateModelSettingsAsync(AIModel model);
        
        /// <summary>
        /// Sets the default model
        /// </summary>
        /// <param name="modelId">ID of the model to set as default</param>
        /// <returns>True if successful</returns>
        Task<bool> SetDefaultModelAsync(int modelId);
        
        /// <summary>
        /// Gets the appropriate service for a specified model
        /// </summary>
        /// <param name="modelId">ID of the model</param>
        /// <returns>The AI service for the model</returns>
        Task<IAIService> GetServiceForModelAsync(int modelId);
        
        /// <summary>
        /// Sets the current model and creates the appropriate service
        /// </summary>
        /// <param name="model">The model to set as current</param>
        /// <returns>True if successful</returns>
        Task<bool> SetCurrentModelAsync(AIModel model);
    }
}
