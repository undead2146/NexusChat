using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Data.Context;
using NexusChat.Data.Interfaces;
using SQLite;

namespace NexusChat.Data.Repositories
{
    /// <summary>
    /// Base repository implementation for database operations
    /// </summary>
    /// <typeparam name="T">Entity type for the repository</typeparam>
    public abstract class BaseRepository<T> : IRepository<T> where T : class, new()
    {
        /// <summary>
        /// The database service
        /// </summary>
        protected readonly DatabaseService DatabaseService;

        /// <summary>
        /// Gets the SQLite database connection
        /// </summary>
        protected SQLiteAsyncConnection Database => DatabaseService.Database;

        /// <summary>
        /// Creates a new instance of BaseRepository
        /// </summary>
        /// <param name="databaseService">Database service</param>
        public BaseRepository(DatabaseService databaseService)
        {
            DatabaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        }
        
        /// <summary>
        /// Ensures the database is initialized
        /// </summary>
        public virtual async Task EnsureDatabaseAsync(CancellationToken cancellationToken = default)
        {
            await DatabaseService.Initialize(cancellationToken);
        }

        /// <summary>
        /// Initializes the repository, ensuring database is ready
        /// </summary>
        public virtual async Task InitializeRepositoryAsync()
        {
            await DatabaseService.Initialize();
        }

        /// <summary>
        /// Initialize database connection
        /// </summary>
        public async Task InitializeAsync()
        {
            await DatabaseService.Initialize();
            
            // Create table for this entity if it doesn't exist
            await Database.CreateTableAsync<T>();
        }

        /// <summary>
        /// Gets all entities
        /// </summary>
        public virtual async Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await EnsureDatabaseAsync(cancellationToken);
                return await Database.Table<T>().ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in {GetType().Name}.GetAllAsync: {ex.Message}");
                return new List<T>();
            }
        }

        /// <summary>
        /// Gets all entities
        /// </summary>
        public virtual async Task<List<T>> GetAllAsync()
        {
            return await GetAllAsync(CancellationToken.None);
        }

        /// <summary>
        /// Gets entities by a predicate
        /// </summary>
        public virtual async Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                await EnsureDatabaseAsync();
                return await Database.Table<T>().Where(predicate).ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in {GetType().Name}.FindAsync: {ex.Message}");
                return new List<T>();
            }
        }

        /// <summary>
        /// Gets an entity by ID
        /// </summary>
        public virtual async Task<T> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                await EnsureDatabaseAsync(cancellationToken);
                return await Database.FindAsync<T>(id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in {GetType().Name}.GetByIdAsync: {ex.Message}");
                return default;
            }
        }

        /// <summary>
        /// Gets an entity by ID
        /// </summary>
        public virtual async Task<T> GetByIdAsync(int id)
        {
            return await GetByIdAsync(id, CancellationToken.None);
        }

        /// <summary>
        /// Adds a new entity
        /// </summary>
        public virtual async Task<int> AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            try
            {
                await EnsureDatabaseAsync(cancellationToken);
                return await Database.InsertAsync(entity);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in {GetType().Name}.AddAsync: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// Adds a new entity
        /// </summary>
        public virtual async Task<int> AddAsync(T entity)
        {
            return await AddAsync(entity, CancellationToken.None);
        }

        /// <summary>
        /// Updates an existing entity
        /// </summary>
        public virtual async Task<bool> UpdateAsync(T entity)
        {
            try
            {
                await EnsureDatabaseAsync();
                int result = await Database.UpdateAsync(entity);
                return result > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in {GetType().Name}.UpdateAsync: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Updates an existing entity with cancellation support
        /// </summary>
        public virtual async Task<bool> UpdateAsync(T entity, CancellationToken cancellationToken)
        {
            try
            {
                await EnsureDatabaseAsync(cancellationToken);
                int result = await Database.UpdateAsync(entity);
                return result > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in {GetType().Name}.UpdateAsync: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Deletes an entity
        /// </summary>
        public virtual async Task<bool> DeleteAsync(T entity)
        {
            try
            {
                await EnsureDatabaseAsync();
                int result = await Database.DeleteAsync(entity);
                return result > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in {GetType().Name}.DeleteAsync: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Deletes an entity with cancellation support
        /// </summary>
        public virtual async Task<bool> DeleteAsync(T entity, CancellationToken cancellationToken)
        {
            try
            {
                await EnsureDatabaseAsync(cancellationToken);
                int result = await Database.DeleteAsync(entity);
                return result > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in {GetType().Name}.DeleteAsync: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Deletes an entity by ID
        /// </summary>
        public virtual async Task<bool> DeleteAsync(int id)
        {
            try
            {
                await EnsureDatabaseAsync();
                int result = await Database.DeleteAsync<T>(id);
                return result > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in {GetType().Name}.DeleteAsync(id): {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Deletes an entity by ID with cancellation support
        /// </summary>
        public virtual async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
        {
            try
            {
                await EnsureDatabaseAsync(cancellationToken);
                int result = await Database.DeleteAsync<T>(id);
                return result > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in {GetType().Name}.DeleteAsync(id): {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the first entity matching a predicate
        /// </summary>
        public virtual async Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                await EnsureDatabaseAsync();
                return await Database.Table<T>().Where(predicate).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in {GetType().Name}.FirstOrDefaultAsync: {ex.Message}");
                return default;
            }
        }
    }
}
