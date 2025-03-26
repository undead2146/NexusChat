using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Core.Models;
using NexusChat.Data.Context;
using NexusChat.Helpers;

namespace NexusChat.Data.Context
{ 
    /// <summary>
    /// Service to handle database search operations
    /// </summary>
    public class DatabaseSearchService
    {
        private readonly DatabaseService _databaseService;
        private readonly DataObjectConverter _converter;
        
        /// <summary>
        /// Initializes a new instance of DatabaseSearchService
        /// </summary>
        public DatabaseSearchService(DatabaseService databaseService, DataObjectConverter converter)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _converter = converter ?? throw new ArgumentNullException(nameof(converter));
        }
        
        /// <summary>
        /// Performs a search across the specified table
        /// </summary>
        public async Task<List<Dictionary<string, object>>> SearchTableAsync(
            string tableName, 
            string searchText, 
            int pageSize, 
            CancellationToken cancellationToken)
        {
            await _databaseService.Initialize();
            
            switch (tableName.ToLower())
            {
                case "user":
                    return await SearchUserTableAsync(searchText, pageSize, cancellationToken);
                case "conversation":
                    return await SearchConversationTableAsync(searchText, pageSize, cancellationToken);
                case "message":
                    return await SearchMessageTableAsync(searchText, pageSize, cancellationToken);
                case "aimodel":
                    return await SearchModelTableAsync(searchText, pageSize, cancellationToken);
                default:
                    return new List<Dictionary<string, object>>();
            }
        }
        
        /// <summary>
        /// Searches the User table
        /// </summary>
        private async Task<List<Dictionary<string, object>>> SearchUserTableAsync(
            string searchText, 
            int pageSize,
            CancellationToken token)
        {
            var result = new List<Dictionary<string, object>>();
            var searchLower = searchText.ToLower();
            
            var users = await _databaseService.Database.Table<User>().ToListAsync();
            
            foreach (var user in users)
            {
                token.ThrowIfCancellationRequested();
                
                if (ContainsSearchText(user.Username, searchLower) ||
                    ContainsSearchText(user.DisplayName, searchLower) ||
                    ContainsSearchText(user.Email, searchLower))
                {
                    result.Add(_converter.ObjectToDictionary(user));
                }
                
                // Limit search results for better performance
                if (result.Count >= pageSize)
                    break;
            }
            
            return result;
        }
        
        /// <summary>
        /// Searches the Conversation table
        /// </summary>
        private async Task<List<Dictionary<string, object>>> SearchConversationTableAsync(
            string searchText, 
            int pageSize,
            CancellationToken token)
        {
            var result = new List<Dictionary<string, object>>();
            var searchLower = searchText.ToLower();
            
            var conversations = await _databaseService.Database.Table<Conversation>().ToListAsync();
            
            foreach (var conversation in conversations)
            {
                token.ThrowIfCancellationRequested();
                
                if (ContainsSearchText(conversation.Title, searchLower) ||
                    ContainsSearchText(conversation.Category, searchLower) ||
                    ContainsSearchText(conversation.Summary, searchLower))
                {
                    result.Add(_converter.ObjectToDictionary(conversation));
                }
                
                // Limit search results for better performance
                if (result.Count >= pageSize)
                    break;
            }
            
            return result;
        }
        
        /// <summary>
        /// Searches the Message table
        /// </summary>
        private async Task<List<Dictionary<string, object>>> SearchMessageTableAsync(
            string searchText, 
            int pageSize,
            CancellationToken token)
        {
            var result = new List<Dictionary<string, object>>();
            var searchLower = searchText.ToLower();
            
            // For messages, use paged approach to avoid loading all at once
            int batchSize = 100;
            int offset = 0;
            bool moreRecords = true;
            
            while (moreRecords && result.Count < pageSize)
            {
                token.ThrowIfCancellationRequested();
                
                var messages = await _databaseService.Database.Table<Message>()
                    .Skip(offset).Take(batchSize).ToListAsync();
                
                if (messages.Count == 0)
                    moreRecords = false;
                
                foreach (var message in messages)
                {
                    token.ThrowIfCancellationRequested();
                    
                    if (ContainsSearchText(message.Content, searchLower) ||
                        ContainsSearchText(message.MessageType, searchLower) ||
                        ContainsSearchText(message.Status, searchLower))
                    {
                        var dict = _converter.ObjectToDictionary(message);
                        
                        // Further reduce content size for search results to improve performance
                        if (dict.ContainsKey("Content") && dict["Content"] is string content && content.Length > 200)
                        {
                            dict["Content"] = content.Substring(0, 200) + "...";
                        }
                        if (dict.ContainsKey("RawResponse") && dict["RawResponse"] is string raw && raw.Length > 50)
                        {
                            dict["RawResponse"] = raw.Substring(0, 50) + "...";
                        }
                        
                        result.Add(dict);
                        
                        // Limit search results
                        if (result.Count >= pageSize)
                            break;
                    }
                }
                
                offset += batchSize;
            }
            
            return result;
        }
        
        /// <summary>
        /// Searches the AIModel table
        /// </summary>
        private async Task<List<Dictionary<string, object>>> SearchModelTableAsync(
            string searchText, 
            int pageSize,
            CancellationToken token)
        {
            var result = new List<Dictionary<string, object>>();
            var searchLower = searchText.ToLower();
            
            var models = await _databaseService.Database.Table<AIModel>().ToListAsync();
            
            foreach (var model in models)
            {
                token.ThrowIfCancellationRequested();
                
                if (ContainsSearchText(model.ModelName, searchLower) ||
                    ContainsSearchText(model.ProviderName, searchLower) ||
                    ContainsSearchText(model.Description, searchLower))
                {
                    result.Add(_converter.ObjectToDictionary(model));
                }
                
                // Limit search results for better performance
                if (result.Count >= pageSize)
                    break;
            }
            
            return result;
        }
        
        /// <summary>
        /// Helper method to check if string contains search text (null-safe)
        /// </summary>
        private bool ContainsSearchText(string value, string searchText)
        {
            return value?.ToLower().Contains(searchText) == true;
        }
    }
}
