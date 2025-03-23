using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Data.Context;

namespace NexusChat.Data.Repositories
{
    /// <summary>
    /// Base repository implementation with common functionality
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public abstract class BaseRepository<T> : IRepository<T> where T : class, new()
    {
        protected readonly DatabaseService _databaseService;
        
        /// <summary>
        /// Initializes a new repository instance
        /// </summary>
        /// <param name="databaseService">Database service</param>
        public BaseRepository(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        }
        
        /// <summary>
        /// Gets entity by its identifier
        /// </summary>
        public virtual async Task<T> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                await _databaseService.Initialize(cancellationToken);
                return await _databaseService.Database.FindAsync<T>(id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in {GetType().Name}.GetByIdAsync: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Gets all entities
        /// </summary>
        public virtual async Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _databaseService.Initialize(cancellationToken);
                return await _databaseService.Database.Table<T>().ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in {GetType().Name}.GetAllAsync: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Adds a new entity
        /// </summary>
        public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            
            try
            {
                await _databaseService.Initialize(cancellationToken);
                await _databaseService.Database.InsertAsync(entity);
                return entity;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in {GetType().Name}.AddAsync: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Updates an existing entity
        /// </summary>
        public virtual async Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            
            try
            {
                await _databaseService.Initialize(cancellationToken);
                await _databaseService.Database.UpdateAsync(entity);
                return entity;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in {GetType().Name}.UpdateAsync: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Deletes an entity
        /// </summary>
        public virtual async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                await _databaseService.Initialize(cancellationToken);
                var entity = await GetByIdAsync(id, cancellationToken);
                if (entity == null) return false;
                
                int result = await _databaseService.Database.DeleteAsync(entity);
                return result > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in {GetType().Name}.DeleteAsync: {ex.Message}");
                throw;
            }
        }
    }
}
