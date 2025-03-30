using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Core.Models;
using NexusChat.Data.Context;

namespace NexusChat.Helpers
{
    /// <summary>
    /// Provides data access operations for the database viewer
    /// </summary>
    public class DatabaseDataProvider
    {
        private readonly DatabaseService _databaseService;
        private readonly SemaphoreSlim _databaseLock = new SemaphoreSlim(1, 1);
        
        /// <summary>
        /// Initializes a new instance of DatabaseDataProvider
        /// </summary>
        /// <param name="databaseService">Database service for data access</param>
        public DatabaseDataProvider(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        }
        
        /// <summary>
        /// Gets the total number of records in a table
        /// </summary>
        public async Task<int> GetTableRecordCountAsync(string tableName)
        {
            try
            {
                // Use semaphore to prevent concurrent database access
                await _databaseLock.WaitAsync();
                
                try
                {
                    await _databaseService.Initialize();
                    
                    switch (tableName.ToLower())
                    {
                        case "user":
                            return await _databaseService.Database.Table<User>().CountAsync();
                        case "conversation":
                            return await _databaseService.Database.Table<Conversation>().CountAsync();
                        case "message":
                            return await _databaseService.Database.Table<Message>().CountAsync();
                        case "aimodel":
                            return await _databaseService.Database.Table<AIModel>().CountAsync();
                        default:
                            return 0;
                    }
                }
                finally
                {
                    _databaseLock.Release();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting record count: {ex.Message}");
                return 0;
            }
        }
        
        /// <summary>
        /// Loads a page of data from a specific table
        /// </summary>
        public async Task<List<Dictionary<string, object>>> LoadTableDataPagedAsync(
            string tableName, 
            int limit, 
            int offset, 
            CancellationToken cancellationToken,
            DataObjectConverter converter)
        {
            var result = new List<Dictionary<string, object>>();
            
            try
            {
                // Use semaphore to prevent concurrent database access
                await _databaseLock.WaitAsync(cancellationToken);
                
                try
                {
                    await _databaseService.Initialize();
                    
                    // Add a small delay to prevent UI thread blocking
                    await Task.Delay(10, cancellationToken);
                    
                    switch (tableName.ToLower())
                    {
                        case "user":
                            var users = await _databaseService.Database.Table<User>()
                                .Skip(offset).Take(limit).ToListAsync();
                            
                            foreach (var user in users)
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                result.Add(converter.ObjectToDictionary(user));
                            }
                            break;
                            
                        case "conversation":
                            var conversations = await _databaseService.Database.Table<Conversation>()
                                .Skip(offset).Take(limit).ToListAsync();
                                
                            foreach (var conversation in conversations)
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                result.Add(converter.ObjectToDictionary(conversation));
                            }
                            break;
                            
                        case "message":
                            // For large message tables, we need to be especially careful
                            var messages = await _databaseService.Database.Table<Message>()
                                .Skip(offset).Take(limit).ToListAsync();
                                
                            foreach (var message in messages)
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                
                                // For messages, truncate very long content for display performance
                                var dict = converter.ObjectToDictionary(message);
                                if (dict.ContainsKey("Content") && dict["Content"] is string content && content.Length > 200)
                                {
                                    dict["Content"] = content.Substring(0, 200) + "...";
                                }
                                if (dict.ContainsKey("RawResponse") && dict["RawResponse"] is string raw && raw.Length > 50)
                                {
                                    dict["RawResponse"] = raw.Substring(0, 50) + "...";
                                }
                                
                                result.Add(dict);
                            }
                            break;
                            
                        case "aimodel":
                            var models = await _databaseService.Database.Table<AIModel>()
                                .Skip(offset).Take(limit).ToListAsync();
                                
                            foreach (var model in models)
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                result.Add(converter.ObjectToDictionary(model));
                            }
                            break;
                    }
                }
                finally
                {
                    _databaseLock.Release();
                }
            }
            catch (Exception ex)
            {
                if (ex is not OperationCanceledException) // Don't log cancellations as errors
                {
                    Debug.WriteLine($"Error in LoadTableDataPaged: {ex.Message}");
                }
                throw;
            }
            
            return result;
        }
        
        /// <summary>
        /// Clears all data from the database
        /// </summary>
        public async Task ClearDatabaseAsync()
        {
            await _databaseLock.WaitAsync();
            try
            {
                // Drop tables
                await _databaseService.Initialize();
                await _databaseService.Database.DropTableAsync<Message>();
                await _databaseService.Database.DropTableAsync<Conversation>();
                await _databaseService.Database.DropTableAsync<User>();
                await _databaseService.Database.DropTableAsync<AIModel>();
                
                // Re-create tables
                await _databaseService.Database.CreateTablesAsync(SQLite.CreateFlags.None,
                    typeof(User),
                    typeof(Conversation),
                    typeof(Message),
                    typeof(AIModel));
            }
            finally
            {
                _databaseLock.Release();
            }
        }
        
        /// <summary>
        /// Cleanup resources
        /// </summary>
        public void Dispose()
        {
            _databaseLock?.Dispose();
        }
    }
}
