using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Data.Interfaces;
using SQLite;

namespace NexusChat.Data.Repositories
{
    /// <summary>
    /// Base repository implementation for data access operations
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public abstract class BaseRepository<T> : IRepository<T> where T : class, new()
    {
        protected readonly SQLiteAsyncConnection _database;
        
        /// <summary>
        /// Initializes a new repository instance
        /// </summary>
        /// <param name="database">SQLite database connection</param>
        protected BaseRepository(SQLiteAsyncConnection database)
        {
            _database = database;
        }
        
        /// <summary>
        /// Ensures the database table exists for this entity
        /// </summary>
        public virtual async Task EnsureDatabaseAsync(CancellationToken cancellationToken = default)
        {
            var task = _database.CreateTableAsync<T>();
            await WithCancellation(task, cancellationToken);
        }
        
        /// <summary>
        /// Gets all entities
        /// </summary>
        public virtual async Task<List<T>> GetAllAsync()
        {
            return await _database.Table<T>().ToListAsync();
        }
        
        /// <summary>
        /// Gets all entities with cancellation support
        /// </summary>
        public virtual async Task<List<T>> GetAllAsync(CancellationToken cancellationToken)
        {
            var task = _database.Table<T>().ToListAsync();
            return await WithCancellation(task, cancellationToken);
        }
        
        /// <summary>
        /// Gets an entity by its identifier
        /// </summary>
        public virtual async Task<T> GetByIdAsync(int id)
        {
            return await _database.FindAsync<T>(id);
        }
        
        /// <summary>
        /// Gets an entity by its identifier with cancellation support
        /// </summary>
        public virtual async Task<T> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            var task = _database.FindAsync<T>(id);
            return await WithCancellation(task, cancellationToken);
        }
        
        /// <summary>
        /// Adds a new entity
        /// </summary>
        public virtual async Task<int> AddAsync(T entity)
        {
            return await _database.InsertAsync(entity);
        }
        
        /// <summary>
        /// Adds a new entity with cancellation support
        /// </summary>
        public virtual async Task<int> AddAsync(T entity, CancellationToken cancellationToken)
        {
            var task = _database.InsertAsync(entity);
            return await WithCancellation(task, cancellationToken);
        }
        
        /// <summary>
        /// Updates an existing entity
        /// </summary>
        public virtual async Task<int> UpdateAsync(T entity)
        {
            return await _database.UpdateAsync(entity);
        }
        
        /// <summary>
        /// Updates an existing entity with cancellation support
        /// </summary>
        public virtual async Task<int> UpdateAsync(T entity, CancellationToken cancellationToken)
        {
            var task = _database.UpdateAsync(entity);
            return await WithCancellation(task, cancellationToken);
        }
        
        /// <summary>
        /// Deletes an entity
        /// </summary>
        public virtual async Task<int> DeleteAsync(T entity)
        {
            return await _database.DeleteAsync(entity);
        }
        
        /// <summary>
        /// Deletes an entity with cancellation support
        /// </summary>
        public virtual async Task<int> DeleteAsync(T entity, CancellationToken cancellationToken)
        {
            var task = _database.DeleteAsync(entity);
            return await WithCancellation(task, cancellationToken);
        }
        
        /// <summary>
        /// Deletes an entity by its identifier
        /// </summary>
        public virtual async Task<int> DeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity == null)
                return 0;
                
            return await DeleteAsync(entity);
        }
        
        /// <summary>
        /// Deletes an entity by its identifier with cancellation support
        /// </summary>
        public virtual async Task<int> DeleteAsync(int id, CancellationToken cancellationToken)
        {
            var entity = await GetByIdAsync(id, cancellationToken);
            if (entity == null)
                return 0;
                
            return await DeleteAsync(entity, cancellationToken);
        }
        
        /// <summary>
        /// Helper method to support cancellation for tasks that don't natively support it
        /// </summary>
        protected async Task<TResult> WithCancellation<TResult>(Task<TResult> task, CancellationToken cancellationToken)
        {
            // For SQLite operations that don't natively support cancellation
            var tcs = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(() => tcs.TrySetResult(true)))
            {
                if (await Task.WhenAny(task, tcs.Task) == tcs.Task && cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(cancellationToken);
                }
            }
            
            return await task;
        }
    }
}
