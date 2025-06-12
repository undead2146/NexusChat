using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Core.Models;
using NexusChat.Data.Interfaces;
using NexusChat.Helpers;

namespace NexusChat.Data.Context
{ 
    /// <summary>
    /// Service to handle database search operations
    /// </summary>
    public class DatabaseSearchService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConversationRepository _conversationRepository;
        private readonly IMessageRepository _messageRepository;
        private readonly IAIModelRepository _modelRepository;
        private readonly DataObjectConverter _converter;
        
        /// <summary>
        /// Initializes a new instance of DatabaseSearchService
        /// </summary>
        public DatabaseSearchService(
            IUserRepository userRepository,
            IConversationRepository conversationRepository,
            IMessageRepository messageRepository,
            IAIModelRepository modelRepository,
            DataObjectConverter converter)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
            _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
            _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
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
            
            // Use repository instead of direct database access
            var users = await _userRepository.SearchAsync(searchText, pageSize);
            
            foreach (var user in users)
            {
                token.ThrowIfCancellationRequested();
                result.Add(_converter.ObjectToDictionary(user));
                
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
            
            // Use repository instead of direct database access
            var conversations = await _conversationRepository.SearchAsync(searchText, pageSize);
            
            foreach (var conversation in conversations)
            {
                token.ThrowIfCancellationRequested();
                result.Add(_converter.ObjectToDictionary(conversation));
                
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
            
            // Use repository instead of direct database access
            var messages = await _messageRepository.SearchAsync(searchText, pageSize);
            
            foreach (var message in messages)
            {
                token.ThrowIfCancellationRequested();
                
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
                
                if (result.Count >= pageSize)
                    break;
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
            
            // Use repository instead of direct database access
            var models = await _modelRepository.SearchAsync(searchText, pageSize);
            
            foreach (var model in models)
            {
                token.ThrowIfCancellationRequested();
                result.Add(_converter.ObjectToDictionary(model));
                
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
