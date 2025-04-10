using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NexusChat.Data.Interfaces
{
    /// <summary>
    /// Generic repository interface for data access operations
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public interface IRepository<T> where T : class, new()
    {
        /// <summary>
        /// Ensures the database table exists for this entity
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task EnsureDatabaseAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets all entities
        /// </summary>
        Task<List<T>> GetAllAsync();
        
        /// <summary>
        /// Gets all entities with cancellation support
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task<List<T>> GetAllAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Gets an entity by ID
        /// </summary>
        /// <param name="id">Entity ID</param>
        Task<T> GetByIdAsync(int id);
        
        /// <summary>
        /// Gets an entity by ID with cancellation support
        /// </summary>
        /// <param name="id">Entity ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task<T> GetByIdAsync(int id, CancellationToken cancellationToken);

        /// <summary>
        /// Adds a new entity
        /// </summary>
        /// <param name="entity">Entity to add</param>
        Task<int> AddAsync(T entity);

        /// <summary>
        /// Adds a new entity with cancellation support
        /// </summary>
        /// <param name="entity">Entity to add</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task<int> AddAsync(T entity, CancellationToken cancellationToken);

        /// <summary>
        /// Updates an entity
        /// </summary>
        /// <param name="entity">Entity to update</param>
        Task<int> UpdateAsync(T entity);

        /// <summary>
        /// Updates an entity with cancellation support
        /// </summary>
        /// <param name="entity">Entity to update</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task<int> UpdateAsync(T entity, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes an entity
        /// </summary>
        /// <param name="entity">Entity to delete</param>
        Task<int> DeleteAsync(T entity);

        /// <summary>
        /// Deletes an entity with cancellation support
        /// </summary>
        /// <param name="entity">Entity to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task<int> DeleteAsync(T entity, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes an entity by ID
        /// </summary>
        /// <param name="id">Entity ID</param>
        Task<int> DeleteAsync(int id);

        /// <summary>
        /// Deletes an entity by ID with cancellation support
        /// </summary>
        /// <param name="id">Entity ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task<int> DeleteAsync(int id, CancellationToken cancellationToken);
    }
}
