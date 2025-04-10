using System.Collections.Generic;
using System.Threading.Tasks;
using NexusChat.Core.Models;

namespace NexusChat.Data.Repositories
{
    public interface IModelConfigurationRepository
    {
        Task<List<ModelConfiguration>> GetAllAsync();
        Task<ModelConfiguration> GetByIdAsync(int id);
        Task<ModelConfiguration> GetByProviderAndModelAsync(string providerName, string modelName);
        Task<ModelConfiguration> GetDefaultAsync();
        Task<bool> SetDefaultAsync(int modelId);
        Task<int> ImportFromEnvironmentAsync(List<ModelConfiguration> configs);
        Task<int> AddAsync(ModelConfiguration model);
        Task<bool> UpdateAsync(ModelConfiguration model);
        Task<bool> DeleteAsync(int id);
        Task<List<ModelConfiguration>> GetByProviderAsync(string providerName);
        Task<ModelConfiguration> GetDefaultConfigurationAsync();
        Task<ModelConfiguration> GetByModelIdentifierAsync(string modelIdentifier);
    }
}
