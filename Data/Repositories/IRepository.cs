using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NexusChat.Data.Repositories
{
    /// <summary>
    /// Generic repository interface for data access operations
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public interface IRepository<T> where T : class
    {
        /// <summary>
        /// Gets an entity by its identifier
        /// </summary>
        /// <param name="id">Entity identifier</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Entity if found, null otherwise</returns>
        Task<T> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets all entities
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>List of all entities</returns>
        Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Adds a new entity
        /// </summary>
        /// <param name="entity">Entity to add</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Added entity with assigned identifier</returns>
        Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Updates an existing entity
        /// </summary>
        /// <param name="entity">Entity to update</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>True if update succeeded, false otherwise</returns>
        Task<bool> UpdateAsync(T entity, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Deletes an entity by its identifier
        /// </summary>
        /// <param name="id">Entity identifier</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>True if deletion succeeded, false otherwise</returns>
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Ensures the database is initialized
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        Task EnsureDatabaseAsync(CancellationToken cancellationToken = default);
    }
}
