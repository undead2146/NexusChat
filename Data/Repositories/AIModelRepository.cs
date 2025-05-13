using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        private readonly DatabaseService _dbService;
        
        /// <summary>
        /// Creates a new instance of AIModelRepository
        /// </summary>
        /// <param name="dbService">Database service</param>
        public AIModelRepository(DatabaseService dbService) : base(dbService)
        {
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
            // Don't call EnsureColumnsExist in constructor - it could cause issues
            // We'll ensure it's called before every DB operation instead
        }
        
        /// <summary>
        /// Ensures required columns exist in the AIModels table
        /// </summary>
        private async Task EnsureColumnsExist()
        {
            try
            {
                Debug.WriteLine("AIModelRepository: Ensuring required columns exist");
                
                // Check if database is ready
                if (_dbService == null)
                {
                    Debug.WriteLine("Error: Database service is null!");
                    return;
                }
                
                // Get connection safely
                var connection = await GetConnectionAsync();
                if (connection == null)
                {
                    Debug.WriteLine("Error: Could not get database connection!");
                    return;
                }
                
                // Check if AIModels table exists
                bool tableExists = false;
                try
                {
                    // Try getting table info as a way to check if table exists
                    var tableInfo = await connection.GetTableInfoAsync("AIModels");
                    tableExists = tableInfo != null && tableInfo.Count > 0;
                    Debug.WriteLine($"AIModel table exists: {tableExists}, has {tableInfo.Count} columns");
                } 
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error checking if AIModels table exists: {ex.Message}");
                    // Create the table if we couldn't get info
                    await connection.CreateTableAsync<AIModel>();
                    tableExists = true;
                    Debug.WriteLine("Created AIModels table");
                }
                
                // If table doesn't exist, create it
                if (!tableExists)
                {
                    await connection.CreateTableAsync<AIModel>();
                    Debug.WriteLine("Created AIModels table");
                    return; // New table will have all columns
                }
                
                // Get existing columns
                var columnNames = new List<string>();
                try
                {
                    var tableInfo = await connection.GetTableInfoAsync("AIModels");
                    columnNames = tableInfo.Select(c => c.Name).ToList();
                    Debug.WriteLine($"Existing columns: {string.Join(", ", columnNames)}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error getting table info: {ex.Message}");
                    return; // Can't proceed without column info
                }
                
                // Create a list to store statements
                var alterStatements = new List<string>();
                
                // Check and add missing columns
                if (!columnNames.Any(c => c.Equals("IsFavorite", StringComparison.OrdinalIgnoreCase)))
                    alterStatements.Add("ALTER TABLE AIModels ADD COLUMN IsFavorite BOOLEAN DEFAULT 0");
                
                if (!columnNames.Any(c => c.Equals("IsSelected", StringComparison.OrdinalIgnoreCase)))
                    alterStatements.Add("ALTER TABLE AIModels ADD COLUMN IsSelected BOOLEAN DEFAULT 0");
                
                if (!columnNames.Any(c => c.Equals("IsDefault", StringComparison.OrdinalIgnoreCase)))
                    alterStatements.Add("ALTER TABLE AIModels ADD COLUMN IsDefault BOOLEAN DEFAULT 0");
                
                if (!columnNames.Any(c => c.Equals("UsageCount", StringComparison.OrdinalIgnoreCase)))
                    alterStatements.Add("ALTER TABLE AIModels ADD COLUMN UsageCount INTEGER DEFAULT 0");
                
                if (!columnNames.Any(c => c.Equals("LastUsed", StringComparison.OrdinalIgnoreCase)))
                    alterStatements.Add("ALTER TABLE AIModels ADD COLUMN LastUsed TEXT");
                
                if (!columnNames.Any(c => c.Equals("DisplayStatus", StringComparison.OrdinalIgnoreCase)))
                    alterStatements.Add("ALTER TABLE AIModels ADD COLUMN DisplayStatus TEXT DEFAULT 'normal'");
                
                // Execute all alter statements
                foreach (var statement in alterStatements)
                {
                    try
                    {
                        Debug.WriteLine($"Executing: {statement}");
                        await connection.ExecuteAsync(statement);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error executing {statement}: {ex.Message}");
                        // Continue with other statements even if one fails
                    }
                }
                
                Debug.WriteLine("AIModelRepository: Column check completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error ensuring columns exist: {ex.Message}");
                Debug.WriteLine(ex.StackTrace);
            }
        }
        
        /// <summary>
        /// Gets a database connection asynchronously with error checking
        /// </summary>
        private async Task<SQLiteAsyncConnection> GetConnectionAsync()
        {
            try
            {
                if (_dbService == null)
                {
                    Debug.WriteLine("Database service is null in GetConnectionAsync");
                    return null;
                }
                
                var connection = await _dbService.GetConnectionAsync();
                if (connection == null)
                {
                    Debug.WriteLine("Connection is null from database service");
                }
                
                return connection;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting database connection: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets a model by its provider and name
        /// </summary>
        public async Task<AIModel> GetModelByNameAsync(string providerName, string modelName)
        {
            try
            {
                // Ensure columns exist before every operation
                await EnsureColumnsExist();
                
                var connection = await GetConnectionAsync();
                if (connection == null) return null;
                
                Debug.WriteLine($"Looking for model: {providerName}/{modelName}");
                
                return await connection.Table<AIModel>()
                    .Where(m => m.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase) 
                             && m.ModelName.Equals(modelName, StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting model by name: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Gets all models for a specific provider
        /// </summary>
        public async Task<List<AIModel>> GetByProviderAsync(string providerName)
        {
            try
            {
                // Ensure columns exist before every operation
                await EnsureColumnsExist();
                
                var connection = await GetConnectionAsync();
                if (connection == null) return new List<AIModel>();
                
                Debug.WriteLine($"Getting models for provider: {providerName}");
                
                return await connection.Table<AIModel>()
                    .Where(m => m.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(m => m.IsDefault)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting models by provider: {ex.Message}");
                return new List<AIModel>();
            }
        }
        
        /// <summary>
        /// Gets all favorite models
        /// </summary>
        public async Task<List<AIModel>> GetFavoriteModelsAsync()
        {
            try
            {
                // Ensure columns exist before every operation
                await EnsureColumnsExist();
                
                var connection = await GetConnectionAsync();
                if (connection == null) return new List<AIModel>();
                
                Debug.WriteLine("Getting favorite models");
                
                return await connection.Table<AIModel>()
                    .Where(m => m.IsFavorite)
                    .OrderBy(m => m.ProviderName)
                    .ThenBy(m => m.ModelName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting favorite models: {ex.Message}");
                return new List<AIModel>();
            }
        }
        
        /// <summary>
        /// Gets the currently selected model
        /// </summary>
        public async Task<AIModel> GetCurrentModelAsync()
        {
            try
            {
                // Ensure columns exist before every operation
                await EnsureColumnsExist();
                
                var connection = await GetConnectionAsync();
                if (connection == null) return null;
                
                Debug.WriteLine("Getting current model");
                
                return await connection.Table<AIModel>()
                    .Where(m => m.IsSelected)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting current model: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Gets all default models
        /// </summary>
        public async Task<List<AIModel>> GetDefaultModelsAsync()
        {
            try
            {
                // Ensure columns exist before every operation
                await EnsureColumnsExist();
                
                var connection = await GetConnectionAsync();
                if (connection == null) return new List<AIModel>();
                
                Debug.WriteLine("Getting default models");
                
                return await connection.Table<AIModel>()
                    .Where(m => m.IsDefault)
                    .OrderBy(m => m.ProviderName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting default models: {ex.Message}");
                return new List<AIModel>();
            }
        }
        
        /// <summary>
        /// Sets a model as the current selected model
        /// </summary>
        public async Task<bool> SetCurrentModelAsync(AIModel model)
        {
            if (model == null) return false;
            
            try
            {
                // Ensure columns exist before every operation
                await EnsureColumnsExist();
                
                var connection = await GetConnectionAsync();
                if (connection == null) return false;
                
                Debug.WriteLine($"Setting current model: {model.ProviderName}/{model.ModelName} (ID: {model.Id})");
                
                // First clear selection on all models with safe SQL
                await connection.ExecuteAsync("UPDATE AIModels SET IsSelected = 0");
                Debug.WriteLine("Cleared IsSelected flag on all models");
                
                // Then set this model as selected
                model.IsSelected = true;
                
                // Use a safer update approach checking for the model's id and provider/name
                int result = 0;
                if (model.Id > 0)
                {
                    // Update by ID if we have it
                    result = await connection.ExecuteAsync(
                        "UPDATE AIModels SET IsSelected = 1 WHERE Id = ?", 
                        model.Id);
                }
                else
                {
                    // Update by provider and model name as fallback
                    result = await connection.ExecuteAsync(
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
                        await connection.ExecuteAsync(
                            "UPDATE AIModels SET UsageCount = COALESCE(UsageCount, 0) + 1, LastUsed = ? WHERE Id = ?",
                            dateStr, model.Id);
                    }
                    else 
                    {
                        await connection.ExecuteAsync(
                            "UPDATE AIModels SET UsageCount = COALESCE(UsageCount, 0) + 1, LastUsed = ? WHERE ProviderName = ? AND ModelName = ?",
                            dateStr, model.ProviderName, model.ModelName);
                    }
                }
                
                Debug.WriteLine($"Updated model selection, rows affected: {result}");
                
                return result > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting current model: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }
        
        /// <summary>
        /// Sets a model as default for its provider
        /// </summary>
        public async Task<bool> SetAsDefaultAsync(string providerName, string modelName)
        {
            if (string.IsNullOrEmpty(providerName) || string.IsNullOrEmpty(modelName)) return false;
            
            try
            {
                // Ensure columns exist before every operation
                await EnsureColumnsExist();
                
                var connection = await GetConnectionAsync();
                if (connection == null) return false;
                
                Debug.WriteLine($"Setting default model: {providerName}/{modelName}");
                
                // Get the model first
                var model = await GetModelByNameAsync(providerName, modelName);
                if (model == null) 
                {
                    Debug.WriteLine($"Model {providerName}/{modelName} not found");
                    return false;
                }
                
                // Clear default flag for this provider
                await connection.ExecuteAsync(
                    "UPDATE AIModels SET IsDefault = 0 WHERE ProviderName = ?", 
                    providerName);
                
                // Set this model as default
                int result = await connection.ExecuteAsync(
                    "UPDATE AIModels SET IsDefault = 1 WHERE Id = ?", 
                    model.Id);
                
                Debug.WriteLine($"Updated model defaults, rows affected: {result}");
                
                return result > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting default model: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Sets a model's favorite status with improved error handling
        /// </summary>
        public async Task<bool> SetFavoriteStatusAsync(string providerName, string modelName, bool isFavorite)
        {
            try
            {
                // Ensure parameters are valid
                if (string.IsNullOrEmpty(providerName) || string.IsNullOrEmpty(modelName)) 
                {
                    Debug.WriteLine("Cannot set favorite: provider or model name is empty");
                    return false;
                }
                
                // Ensure columns exist before every operation
                await EnsureColumnsExist();
                
                var connection = await GetConnectionAsync();
                if (connection == null) return false;
                
                Debug.WriteLine($"Setting favorite status for {providerName}/{modelName} to {isFavorite}");
                
                // First try to find the model
                var model = await GetModelByNameAsync(providerName, modelName);
                if (model == null)
                {
                    Debug.WriteLine($"Model {providerName}/{modelName} not found - attempting direct update");
                    // Try a direct update if model not found (may happen if model is in DB but object not loaded)
                    int directResult = await connection.ExecuteAsync(
                        "UPDATE AIModels SET IsFavorite = ? WHERE ProviderName = ? AND ModelName = ?",
                        isFavorite ? 1 : 0, 
                        providerName,
                        modelName);
                    
                    Debug.WriteLine($"Direct update result: {directResult} rows affected");
                    return directResult > 0;
                }
                
                // Update favorite status in the model
                model.IsFavorite = isFavorite;
                
                // Try to update using the model object first
                try
                {
                    await connection.UpdateAsync(model);
                    Debug.WriteLine("Updated model using UpdateAsync");
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error updating model with UpdateAsync: {ex.Message}, trying direct SQL");
                    
                    // Fallback to direct SQL if model update fails
                    int result = await connection.ExecuteAsync(
                        "UPDATE AIModels SET IsFavorite = ? WHERE Id = ?",
                        isFavorite ? 1 : 0, 
                        model.Id);
                    
                    Debug.WriteLine($"Updated model favorite status with direct SQL, rows affected: {result}");
                    return result > 0;
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
        /// Records usage of a model
        /// </summary>
        public async Task<bool> RecordUsageAsync(string providerName, string modelName)
        {
            if (string.IsNullOrEmpty(providerName) || string.IsNullOrEmpty(modelName)) return false;
            
            try
            {
                // Ensure columns exist before every operation
                await EnsureColumnsExist();
                
                var connection = await GetConnectionAsync();
                if (connection == null) return false;
                
                Debug.WriteLine($"Recording usage for {providerName}/{modelName}");
                
                // Get the model first
                var model = await GetModelByNameAsync(providerName, modelName);
                if (model == null)
                {
                    Debug.WriteLine($"Model {providerName}/{modelName} not found - attempting direct update");
                    // Try a direct update if model not found
                    int directResult = await connection.ExecuteAsync(
                        "UPDATE AIModels SET UsageCount = COALESCE(UsageCount, 0) + 1, LastUsed = ? " +
                        "WHERE ProviderName = ? AND ModelName = ?",
                        DateTime.Now.ToString("o"),
                        providerName,
                        modelName);
                        
                    return directResult > 0;
                }
                
                // Update usage statistics directly with SQL
                int result = await connection.ExecuteAsync(
                    "UPDATE AIModels SET UsageCount = COALESCE(UsageCount, 0) + 1, LastUsed = ? WHERE Id = ?",
                    DateTime.Now.ToString("o"),
                    model.Id);
                
                return result > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error recording model usage: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Gets all active models (with valid API keys)
        /// </summary>
        public async Task<List<AIModel>> GetActiveModelsAsync()
        {
            try
            {
                // Ensure columns exist before every operation
                await EnsureColumnsExist();
                
                // Since we can't check API keys directly in the repository,
                // we'll return the models and let the service filter them
                var allModels = await GetAllAsync();
                return allModels;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting active models: {ex.Message}");
                return new List<AIModel>();
            }
        }
    }
}

