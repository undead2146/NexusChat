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
    /// Base implementation of the repository pattern
    /// </summary>
    public abstract class BaseRepository<T> : IRepository<T> where T : class, new()
    {
        protected readonly DatabaseService _dbService;
        protected readonly string _tableName;

        public BaseRepository(DatabaseService dbService)
        {
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
            _tableName = typeof(T).Name;
        }

        /// <summary>
        /// Execute database operation with standardized error handling and cancellation support
        /// </summary>
        protected async Task<TResult> ExecuteDbOperationAsync<TResult>(
            Func<SQLiteAsyncConnection, CancellationToken, Task<TResult>> operation, 
            string operationName,
            CancellationToken cancellationToken = default,
            TResult defaultValue = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var db = await _dbService.GetConnectionAsync();
                return await operation(db, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine($"Operation {operationName} on {_tableName} was cancelled");
                throw; // Rethrow cancellation to allow proper handling
            }
            catch (SQLiteException ex)
            {
                Debug.WriteLine($"SQLite error in {operationName} on {_tableName}: {ex.Message}");
                return defaultValue;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in {operationName} on {_tableName}: {ex.Message}");
                throw;  // Re-throw non-SQLite errors as they may need different handling
            }
        }

        // IRepository implementation
        public async Task<T> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await ExecuteDbOperationAsync(
                async (db, ct) => await db.GetAsync<T>(id),
                "GetById",
                cancellationToken);
        }

        public virtual async Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await ExecuteDbOperationAsync(
                async (db, ct) => await db.Table<T>().ToListAsync(),
                "GetAll",
                cancellationToken,
                new List<T>());
        }

        public async Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await ExecuteDbOperationAsync(
                async (db, ct) => await db.Table<T>().Where(predicate).ToListAsync(),
                "Find",
                CancellationToken.None,
                new List<T>());
        }

        public virtual async Task<int> AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            return await ExecuteDbOperationAsync(
                async (db, ct) => await db.InsertAsync(entity),
                "Add",
                cancellationToken,
                -1);
        }

        public async Task<bool> UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            return await ExecuteDbOperationAsync(
                async (db, ct) =>
                {
                    int result = await db.UpdateAsync(entity);
                    return result > 0;
                },
                "Update",
                cancellationToken,
                false);
        }

        // For backward compatibility - delegates to cancellation-aware version
        public Task<bool> UpdateAsync(T entity) => UpdateAsync(entity, CancellationToken.None);

        public async Task<bool> DeleteAsync(T entity, CancellationToken cancellationToken = default)
        {
            return await ExecuteDbOperationAsync(
                async (db, ct) =>
                {
                    int result = await db.DeleteAsync(entity);
                    return result > 0;
                },
                "Delete",
                cancellationToken,
                false);
        }

        // For backward compatibility - delegates to cancellation-aware version
        public Task<bool> DeleteAsync(T entity) => DeleteAsync(entity, CancellationToken.None);

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            return await ExecuteDbOperationAsync(
                async (db, ct) =>
                {
                    int result = await db.DeleteAsync<T>(id);
                    return result > 0;
                },
                "DeleteById",
                cancellationToken,
                false);
        }

        // For backward compatibility - delegates to cancellation-aware version
        public Task<bool> DeleteAsync(int id) => DeleteAsync(id, CancellationToken.None);

        public async Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await ExecuteDbOperationAsync(
                async (db, ct) => await db.Table<T>().Where(predicate).FirstOrDefaultAsync(),
                "FirstOrDefault",
                CancellationToken.None);
        }

        public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            var result = await ExecuteDbOperationAsync(
                async (db, ct) => await db.Table<T>().Where(predicate).FirstOrDefaultAsync(),
                "Exists",
                CancellationToken.None);
                
            return result != null;
        }

        public abstract Task<List<T>> SearchAsync(string searchText, int limit = 50);
    }
}
