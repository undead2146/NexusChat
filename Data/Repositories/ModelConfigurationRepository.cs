using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Core.Models;
using NexusChat.Data.Context;
using SQLite;

namespace NexusChat.Data.Repositories
{
    public class ModelConfigurationRepository : IModelConfigurationRepository
    {
        private readonly DatabaseService _database;
        private readonly SemaphoreSlim _dbLock = new SemaphoreSlim(1, 1);
        
        public ModelConfigurationRepository(DatabaseService database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }
        
        public async Task<List<ModelConfiguration>> GetAllAsync()
        {
            await EnsureInitializedAsync();
            
            // Use direct SQLite connection
            var connection = _database.Connection;
            var result = connection.Table<ModelConfiguration>().ToList();
            
            // Convert to async result
            return await Task.FromResult(result);
        }
        
        public async Task<ModelConfiguration> GetByIdAsync(int id)
        {
            await EnsureInitializedAsync();
            return await _database.Database.FindAsync<ModelConfiguration>(id);
        }
        
        public async Task<ModelConfiguration> GetByProviderAndModelAsync(string providerName, string modelName)
        {
            await EnsureInitializedAsync();
            
            // Use sync connection and convert result
            var connection = _database.Connection;
            var result = connection.Table<ModelConfiguration>()
                .Where(m => m.ProviderName == providerName && m.ModelIdentifier == modelName)
                .FirstOrDefault();
                
            return await Task.FromResult(result);
        }
        
        public async Task<ModelConfiguration> GetDefaultAsync()
        {
            await EnsureInitializedAsync();
            
            // Use sync connection and convert result
            var connection = _database.Connection;
            var result = connection.Table<ModelConfiguration>()
                .Where(m => m.IsDefault)
                .FirstOrDefault();
                
            return await Task.FromResult(result);
        }
        
        public async Task<bool> SetDefaultAsync(int modelId)
        {
            await EnsureInitializedAsync();
            
            var connection = _database.Connection;
            
            // Clear existing defaults
            connection.Execute("UPDATE ModelConfigurations SET IsDefault = 0");
            
            // Set new default
            int rows = connection.Execute("UPDATE ModelConfigurations SET IsDefault = 1 WHERE Id = ?", modelId);
            
            return await Task.FromResult(rows > 0);
        }
        
        public async Task<int> AddAsync(ModelConfiguration model)
        {
            await EnsureInitializedAsync();
            return await _database.Database.InsertAsync(model);
        }
        
        public async Task<bool> UpdateAsync(ModelConfiguration model)
        {
            if (model == null)
                return false;
                
            await EnsureInitializedAsync();
            
            await _dbLock.WaitAsync();
            try
            {
                // Update the modification date
                model.ModifiedDate = DateTime.UtcNow;
                
                int result = await _database.Database.UpdateAsync(model);
                return result > 0;
            }
            finally
            {
                _dbLock.Release();
            }
        }
        
        public async Task<bool> DeleteAsync(int id)
        {
            await EnsureInitializedAsync();
            
            // Use direct connection
            var connection = _database.Connection;
            int rows = connection.Delete<ModelConfiguration>(id);
            
            return await Task.FromResult(rows > 0);
        }
        
        public async Task<int> ImportFromEnvironmentAsync(List<ModelConfiguration> configs)
        {
            if (configs == null || !configs.Any())
                return 0;
                
            await EnsureInitializedAsync();
            
            int imported = 0;
            
            // Use the lock to ensure thread safety
            await _dbLock.WaitAsync();
            try
            {
                // Use a transaction for better performance and atomicity
                await _database.Database.RunInTransactionAsync(tran =>
                {
                    foreach (var config in configs)
                    {
                        if (string.IsNullOrEmpty(config.ProviderName) || string.IsNullOrEmpty(config.ModelIdentifier))
                            continue;
                            
                        // Check if configuration already exists
                        var existing = _database.Connection.Table<ModelConfiguration>()
                            .Where(m => m.ProviderName == config.ProviderName && m.ModelIdentifier == config.ModelIdentifier)
                            .FirstOrDefault();
                            
                        if (existing != null)
                        {
                            // Update existing configuration
                            existing.Description = config.Description ?? existing.Description;
                            existing.ApiKeyEnvironmentVariable = config.ApiKeyEnvironmentVariable ?? existing.ApiKeyEnvironmentVariable;
                            
                            if (config.Capabilities != null)
                            {
                                existing.Capabilities = config.Capabilities;
                            }
                            
                            _database.Connection.Update(existing);
                        }
                        else
                        {
                            // Set creation date
                            config.CreatedDate = DateTime.UtcNow;
                            config.ModifiedDate = DateTime.UtcNow;
                            
                            // Add new configuration
                            _database.Connection.Insert(config);
                            imported++;
                        }
                    }
                });
            }
            finally
            {
                _dbLock.Release();
            }
            
            return imported;
        }
        
        public async Task<ModelConfiguration> GetByModelIdentifierAsync(string modelIdentifier)
        {
            await EnsureInitializedAsync();
            
            // Use sync connection and convert result
            var connection = _database.Connection;
            var result = connection.Table<ModelConfiguration>()
                .Where(m => m.ModelIdentifier == modelIdentifier)
                .FirstOrDefault();
                
            return await Task.FromResult(result);
        }
        
        public async Task<ModelConfiguration> GetDefaultConfigurationAsync()
        {
            await EnsureInitializedAsync();
            
            // Get the default configuration
            var result = _database.Connection.Table<ModelConfiguration>()
                .Where(m => m.IsDefault && m.IsEnabled)
                .FirstOrDefault();
            
            return await Task.FromResult(result);
        }
        
        public async Task<List<ModelConfiguration>> GetByProviderAsync(string providerName)
        {
            await EnsureInitializedAsync();
            
            if (string.IsNullOrEmpty(providerName))
                return new List<ModelConfiguration>();
                
            // Use sync connection and convert result
            var connection = _database.Connection;
            var result = connection.Table<ModelConfiguration>()
                .Where(m => m.ProviderName == providerName && m.IsEnabled)
                .ToList();
                
            return await Task.FromResult(result);
        }
        
        public async Task<List<string>> GetEnabledProvidersAsync()
        {
            await EnsureInitializedAsync();
            
            var result = _database.Connection.Table<ModelConfiguration>()
                .Where(m => m.IsEnabled)
                .Select(m => m.ProviderName)
                .Distinct()
                .ToList();
                
            return await Task.FromResult(result);
        }
        
        private async Task EnsureInitializedAsync()
        {
            if (_database.Connection == null)
            {
                await _database.EnsureInitializedAsync();
            }
            
            // Ensure table exists
            _database.Connection.CreateTable<ModelConfiguration>();
        }
    }
    
}
