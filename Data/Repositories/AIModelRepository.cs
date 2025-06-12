using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Core.Models;
using NexusChat.Data.Context;
using NexusChat.Data.Interfaces;
using SQLite;

namespace NexusChat.Data.Repositories
{
    /// <summary>
    /// Repository for AI models
    /// </summary>
    public class AIModelRepository : BaseRepository<AIModel>, IAIModelRepository
    {
        /// <summary>
        /// Creates a new instance of AIModelRepository
        /// </summary>
        /// <param name="dbService">Database service</param>
        public AIModelRepository(DatabaseService dbService) : base(dbService)
        {
            // No need to call schema migration here as it's handled by DatabaseService
        }

        /// <summary>
        /// Gets a model by its provider and name
        /// </summary>
        public async Task<AIModel> GetModelByNameAsync(string providerName, string modelName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(providerName) || string.IsNullOrEmpty(modelName))
                return null;

            return await ExecuteDbOperationAsync<AIModel>(async (db, ct) =>
            {
                Debug.WriteLine($"AIModelRepository: Getting model {providerName}/{modelName}");
                
                // Use parameter binding with LOWER for case-insensitive comparison
                var sql = @"
                    SELECT * FROM AIModels 
                    WHERE LOWER(ProviderName) = LOWER(?) 
                    AND LOWER(ModelName) = LOWER(?) 
                    LIMIT 1";
                
                var models = await db.QueryAsync<AIModel>(sql, providerName, modelName);
                var model = models.FirstOrDefault();
                
                Debug.WriteLine($"AIModelRepository: Found model: {(model != null ? "Yes" : "No")}");
                return model;
            }, $"GetModelByName for {providerName}/{modelName}", cancellationToken);
        }
        
        /// <summary>
        /// Gets all models for a specific provider
        /// </summary>
        public async Task<List<AIModel>> GetByProviderAsync(string providerName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(providerName))
                return new List<AIModel>();

            return await ExecuteDbOperationAsync<List<AIModel>>(async (db, ct) =>
            {
                Debug.WriteLine($"AIModelRepository: Getting models for provider {providerName}");
                
                // Use LIKE with case-insensitive comparison instead of equals function
                var sql = @"
                    SELECT * FROM AIModels 
                    WHERE LOWER(ProviderName) = LOWER(?)
                    ORDER BY DisplayName, ModelName";
                
                var models = await db.QueryAsync<AIModel>(sql, providerName);
                
                Debug.WriteLine($"AIModelRepository: Found {models.Count} models for provider {providerName}");
                return models;
            }, $"GetByProvider for {providerName}", cancellationToken, new List<AIModel>());
        }
        
        /// <summary>
        /// Gets all favorite models
        /// </summary>
        public async Task<List<AIModel>> GetFavoriteModelsAsync(CancellationToken cancellationToken = default)
        {
            return await ExecuteDbOperationAsync(
                async (db, ct) => await db.Table<AIModel>()
                    .Where(m => m.IsFavorite)
                    .OrderBy(m => m.ProviderName)
                    .ThenBy(m => m.ModelName)
                    .ToListAsync(),
                "GetFavoriteModels",
                cancellationToken,
                new List<AIModel>());
        }
        
        /// <summary>
        /// Gets the currently selected model
        /// </summary>
        public async Task<AIModel> GetCurrentModelAsync(CancellationToken cancellationToken = default)
        {
            return await ExecuteDbOperationAsync(
                async (db, ct) => await db.Table<AIModel>()
                    .Where(m => m.IsSelected)
                    .FirstOrDefaultAsync(),
                "GetCurrentModel",
                cancellationToken);
        }
        
        /// <summary>
        /// Gets all default models
        /// </summary>
        public async Task<List<AIModel>> GetDefaultModelsAsync(CancellationToken cancellationToken = default)
        {
            return await ExecuteDbOperationAsync(
                async (db, ct) => await db.Table<AIModel>()
                    .Where(m => m.IsDefault)
                    .OrderBy(m => m.ProviderName)
                    .ToListAsync(),
                "GetDefaultModels",
                cancellationToken,
                new List<AIModel>());
        }
        
        /// <summary>
        /// Sets a model as the current selected model
        /// </summary>
        public async Task<bool> SetCurrentModelAsync(AIModel model, CancellationToken cancellationToken = default)
        {
            if (model == null) return false;
            
            return await ExecuteDbOperationAsync(
                async (db, ct) => {
                    // First clear selection on all models
                    await db.ExecuteAsync("UPDATE AIModels SET IsSelected = 0");
                    
                    // Then set this model as selected
                    int result;
                    
                    if (model.Id > 0)
                    {
                        // Update by ID if we have it
                        result = await db.ExecuteAsync(
                            "UPDATE AIModels SET IsSelected = 1 WHERE Id = ?", 
                            model.Id);
                    }
                    else
                    {
                        // Update by provider and model name as fallback
                        result = await db.ExecuteAsync(
                            "UPDATE AIModels SET IsSelected = 1 WHERE ProviderName = ? AND ModelName = ?", 
                            model.ProviderName, model.ModelName);
                    }
                    
                    // Update usage statistics
                    if (result > 0)
                    {
                        DateTime now = DateTime.Now;
                        string dateStr = now.ToString("o");
                        
                        if (model.Id > 0)
                        {
                            await db.ExecuteAsync(
                                "UPDATE AIModels SET UsageCount = COALESCE(UsageCount, 0) + 1, LastUsed = ? WHERE Id = ?",
                                dateStr, model.Id);
                        }
                        else 
                        {
                            await db.ExecuteAsync(
                                "UPDATE AIModels SET UsageCount = COALESCE(UsageCount, 0) + 1, LastUsed = ? WHERE ProviderName = ? AND ModelName = ?",
                                dateStr, model.ProviderName, model.ModelName);
                        }
                    }
                    
                    return result > 0;
                },
                "SetCurrentModel",
                cancellationToken,
                false);
        }
        
        /// <summary>
        /// Sets a model as default for its provider
        /// </summary>
        public async Task<bool> SetAsDefaultAsync(string providerName, string modelName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(providerName) || string.IsNullOrEmpty(modelName)) return false;
            
            return await ExecuteDbOperationAsync(
                async (db, ct) => {
                    // Get the model first
                    var model = await db.Table<AIModel>()
                        .Where(m => m.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase) 
                                 && m.ModelName.Equals(modelName, StringComparison.OrdinalIgnoreCase))
                        .FirstOrDefaultAsync();
                        
                    if (model == null) return false;
                    
                    // Clear default flag for this provider
                    await db.ExecuteAsync(
                        "UPDATE AIModels SET IsDefault = 0 WHERE ProviderName = ?", 
                        providerName);
                    
                    // Set this model as default
                    int result = await db.ExecuteAsync(
                        "UPDATE AIModels SET IsDefault = 1 WHERE Id = ?", 
                        model.Id);
                    
                    return result > 0;
                },
                "SetAsDefault",
                cancellationToken,
                false);
        }
        
        /// <summary>
        /// Sets a model's favorite status
        /// </summary>
        public async Task<bool> SetFavoriteStatusAsync(string providerName, string modelName, bool isFavorite, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(providerName) || string.IsNullOrEmpty(modelName)) return false;
            
            return await ExecuteDbOperationAsync(
                async (db, ct) => {
                    // Try direct update first
                    int result = await db.ExecuteAsync(
                        "UPDATE AIModels SET IsFavorite = ? WHERE ProviderName = ? AND ModelName = ?",
                        isFavorite ? 1 : 0, 
                        providerName,
                        modelName);
                        
                    return result > 0;
                },
                "SetFavoriteStatus", 
                cancellationToken,
                false);
        }
        
        /// <summary>
        /// Records usage of a model
        /// </summary>
        public async Task<bool> RecordUsageAsync(string providerName, string modelName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(providerName) || string.IsNullOrEmpty(modelName)) return false;
            
            return await ExecuteDbOperationAsync(
                async (db, ct) => {
                    int result = await db.ExecuteAsync(
                        "UPDATE AIModels SET UsageCount = COALESCE(UsageCount, 0) + 1, LastUsed = ? " +
                        "WHERE ProviderName = ? AND ModelName = ?",
                        DateTime.Now.ToString("o"),
                        providerName,
                        modelName);
                        
                    return result > 0;
                },
                "RecordUsage",
                cancellationToken,
                false);
        }
        
        /// <summary>
        /// Gets available models (where IsAvailable = true)
        /// </summary>
        public async Task<List<AIModel>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
        {
            return await GetModelsByAvailabilityAsync(true, cancellationToken);
        }
        
        /// <summary>
        /// Gets models by availability status
        /// </summary>
        public async Task<List<AIModel>> GetModelsByAvailabilityAsync(bool isAvailable, CancellationToken cancellationToken = default)
        {
            // FindAsync only takes the predicate; cancellationToken is not supported in BaseRepository
            return await FindAsync(m => m.IsAvailable == isAvailable);
        }
        
        /// <summary>
        /// Gets models with API key configuration in the database
        /// Note: This doesn't check if those API keys are currently valid at runtime
        /// </summary>
        public async Task<List<AIModel>> GetModelsWithApiKeyConfigAsync(CancellationToken cancellationToken = default)
        {
            // FindAsync only takes the predicate; cancellationToken is not supported in BaseRepository
            return await FindAsync(m => !string.IsNullOrEmpty(m.ApiKeyVariable));
        }
        
        /// <summary>
        /// Gets active models (models that are available and have API keys configured)
        /// </summary>
        public async Task<List<AIModel>> GetActiveModelsAsync(CancellationToken cancellationToken = default)
        {
            return await ExecuteDbOperationAsync(
                async (db, ct) => await db.Table<AIModel>()
                    .Where(m => m.IsAvailable && !string.IsNullOrEmpty(m.ApiKeyVariable))
                    .OrderByDescending(m => m.IsSelected)
                    .ThenByDescending(m => m.IsFavorite)
                    .ThenByDescending(m => m.IsDefault)
                    .ThenByDescending(m => m.UsageCount)
                    .ThenBy(m => m.ProviderName)
                    .ThenBy(m => m.ModelName)
                    .ToListAsync(),
                "GetActiveModels",
                cancellationToken,
                new List<AIModel>());
        }
        
        /// <summary>
        /// Implementation of the abstract search method
        /// </summary>
        public override async Task<List<AIModel>> SearchAsync(string searchText, int limit = 50)
        {
            if (string.IsNullOrEmpty(searchText)) 
                return await GetAllAsync();
                
            searchText = searchText.ToLowerInvariant();
            
            return await ExecuteDbOperationAsync(
                async (db, ct) => {
                    // Use SQL LIKE for text search
                    return await db.QueryAsync<AIModel>(
                        @"SELECT * FROM AIModels 
                          WHERE lower(ModelName) LIKE ? OR 
                                lower(ProviderName) LIKE ? OR 
                                lower(Description) LIKE ? OR
                                lower(DisplayName) LIKE ?
                          ORDER BY IsSelected DESC, IsFavorite DESC, UsageCount DESC
                          LIMIT ?",
                        $"%{searchText}%", $"%{searchText}%", $"%{searchText}%", $"%{searchText}%", limit);
                },
                "Search",
                CancellationToken.None,
                new List<AIModel>());
        }
        
        /// <summary>
        ///  GetAllAsync 
        /// </summary>
        public override async Task<List<AIModel>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await ExecuteDbOperationAsync(
                async (db, ct) => {
                    // Simplified query for better performance
                    var query = @"
                        SELECT * FROM AIModels 
                        ORDER BY IsSelected DESC, IsFavorite DESC, IsDefault DESC, 
                                 UsageCount DESC, ProviderName, ModelName
                        LIMIT 50";
                    
                    var models = await db.QueryAsync<AIModel>(query);
                    Debug.WriteLine($"AIModelRepository: Retrieved {models.Count} models from database (performance optimized)");
                    return models;
                },
                "GetAll",
                cancellationToken,
                new List<AIModel>());
        }

        /// <summary>
        /// Adds a model with duplicate prevention 
        /// </summary>
        public override async Task<int> AddAsync(AIModel model, CancellationToken cancellationToken = default)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            return await ExecuteDbOperationAsync(
                async (db, ct) => {
                    //Check for existing model before adding
                    var existing = await db.Table<AIModel>()
                        .Where(m => m.ProviderName.ToLower() == model.ProviderName.ToLower() &&
                                   m.ModelName.ToLower() == model.ModelName.ToLower())
                        .FirstOrDefaultAsync();
                    
                    if (existing != null)
                    {
                        Debug.WriteLine($"AIModelRepository: Model {model.ProviderName}/{model.ModelName} already exists with ID {existing.Id}");
                        return existing.Id; // Return existing ID instead of creating duplicate
                    }
                    
                    // Set timestamps
                    model.CreatedAt = DateTime.UtcNow;
                    model.UpdatedAt = DateTime.UtcNow;
                    
                    int result = await db.InsertAsync(model);
                    Debug.WriteLine($"AIModelRepository: Added new model {model.ProviderName}/{model.ModelName} with ID {model.Id}");
                    return model.Id;
                },
                "AddWithDuplicateCheck",
                cancellationToken,
                -1);
        }

        /// <summary>
        /// Bulk cleanup method to remove database duplicates 
        /// </summary>
        public async Task<int> CleanupDuplicatesAsync()
        {
            return await ExecuteDbOperationAsync(
                async (db, ct) => {
                    Debug.WriteLine("AIModelRepository: Starting duplicate cleanup");
                    
                    // Find duplicates
                    var duplicatesQuery = @"
                        SELECT Id FROM AIModels 
                        WHERE Id NOT IN (
                            SELECT MIN(Id) 
                            FROM AIModels 
                            GROUP BY LOWER(ProviderName), LOWER(ModelName)
                        )";
                    
                    var duplicateIds = await db.QueryAsync<int>(duplicatesQuery);
                    
                    if (duplicateIds.Count == 0)
                    {
                        Debug.WriteLine("AIModelRepository: No duplicates found");
                        return 0;
                    }
                    
                    // Delete duplicates
                    var deletedCount = 0;
                    foreach (var id in duplicateIds)
                    {
                        await db.ExecuteAsync("DELETE FROM AIModels WHERE Id = ?", id);
                        deletedCount++;
                    }
                    
                    Debug.WriteLine($"AIModelRepository: Removed {deletedCount} duplicate models");
                    return deletedCount;
                },
                "CleanupDuplicates",
                CancellationToken.None,
                0);
        }
    }
}

