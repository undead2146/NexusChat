using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NexusChat.Data.Repositories
{
    /// <summary>
    /// Generic repository interface for data access operations
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    public interface IRepository<T> where T : class
    {
        /// <summary>
        /// Gets entity by its identifier
        /// </summary>
        /// <param name="id">The entity identifier</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>The entity, or null if not found</returns>
        Task<T> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets all entities
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>List of entities</returns>
        Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Adds a new entity
        /// </summary>
        /// <param name="entity">The entity to add</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>The added entity with its generated ID</returns>
        Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Updates an existing entity
        /// </summary>
        /// <param name="entity">The entity to update</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>The updated entity</returns>
        Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Deletes an entity
        /// </summary>
        /// <param name="id">The entity identifier</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>True if deleted, false if not found</returns>
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}
