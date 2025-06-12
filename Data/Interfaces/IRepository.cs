using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace NexusChat.Data.Interfaces
{
    /// <summary>
    /// Interface for generic repository operations
    /// </summary>
    /// <typeparam name="T">Entity type for the repository</typeparam>
    public interface IRepository<T> where T : class, new()
    {
        
        /// <summary>
        /// Gets all entities
        /// </summary>
        Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets an entity by ID
        /// </summary>
        Task<T> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets entities by a predicate
        /// </summary>
        Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Gets the first entity matching a predicate
        /// </summary>
        Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Adds a new entity
        /// </summary>
        Task<int> AddAsync(T entity, CancellationToken cancellationToken = default);


        /// <summary>
        /// Updates an existing entity with cancellation support
        /// </summary>
        Task<bool> UpdateAsync(T entity, CancellationToken cancellationToken);
 
        /// <summary>
        /// Deletes an entity with cancellation support
        /// </summary>
        Task<bool> DeleteAsync(T entity, CancellationToken cancellationToken);


        /// <summary>
        /// Deletes an entity by ID with cancellation support
        /// </summary>
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);

        /// <summary>
        /// Checks if any entity matches a predicate
        /// </summary>
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Search entities using text search across relevant fields
        /// </summary>
        /// <param name="searchText">Text to search for</param>
        /// <param name="limit">Maximum number of results to return</param>
        /// <returns>List of entities matching the search criteria</returns>
        Task<List<T>> SearchAsync(string searchText, int limit = 50);
    }
}
