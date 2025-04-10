using SQLite;
using System.Collections.Generic;

namespace NexusChat.Core.Models
{
    /// <summary>
    /// Configuration for an AI provider
    /// </summary>
    [Table("ProviderConfigurations")]
    public class ProviderConfiguration
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        
        [SQLite.MaxLength(100)]
        public string ProviderId { get; set; }
        
        [SQLite.MaxLength(100)]
        public string Name { get; set; }
        
        [SQLite.MaxLength(255)]
        public string ApiEndpoint { get; set; }
        
        [SQLite.MaxLength(100)]
        public string ApiKeyName { get; set; }
        
        public bool IsEnabled { get; set; } = true;
        
        public Dictionary<string, string> AdditionalSettings { get; set; } = new Dictionary<string, string>();
    }
}
