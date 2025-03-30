using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Data.Context;
using SQLite;

namespace NexusChat.Data.Repositories
{
    /// <summary>
    /// Base repository implementation for data access operations
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public abstract class BaseRepository<T> : IRepository<T> where T : class, new()
    {
        protected readonly DatabaseService _databaseService;
        
        /// <summary>
        /// Initializes a new repository instance
        /// </summary>
        /// <param name="databaseService">Database service</param>
        protected BaseRepository(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        }
        
        /// <summary>
        /// Ensures the database is initialized
        /// </summary>
        public virtual async Task EnsureDatabaseAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _databaseService.Initialize(cancellationToken);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing database: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Gets an entity by its identifier
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
                Debug.WriteLine($"Error in GetByIdAsync: {ex.Message}");
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
                Debug.WriteLine($"Error in GetAllAsync: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Adds a new entity
        /// </summary>
        public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            try
            {
                await _databaseService.Initialize(cancellationToken);
                await _databaseService.Database.InsertAsync(entity);
                return entity;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in AddAsync: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Updates an existing entity
        /// </summary>
        public virtual async Task<bool> UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            try
            {
                await _databaseService.Initialize(cancellationToken);
                int result = await _databaseService.Database.UpdateAsync(entity);
                return result > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in UpdateAsync: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Deletes an entity by its identifier
        /// </summary>
        public virtual async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                await _databaseService.Initialize(cancellationToken);
                int result = await _databaseService.Database.DeleteAsync<T>(id);
                return result > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in DeleteAsync: {ex.Message}");
                throw;
            }
        }
    }
}
