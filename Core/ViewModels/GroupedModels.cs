using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NexusChat.Core.Models;

namespace NexusChat.Core.ViewModels
{
    /// <summary>
    /// Represents a group of AI models organized by provider
    /// </summary>
    public class GroupedModels : ObservableCollection<AIModelItemViewModel>
    {
        /// <summary>
        /// Gets or sets the provider name for this group
        /// </summary>
        public string Provider { get; private set; }
        
        /// <summary>
        /// Gets or sets the display name for this group (formatted provider name)
        /// </summary>
        public string DisplayName { get; private set; }
        
        /// <summary>
        /// Gets or sets the count of models in this group
        /// </summary>
        public int Count => Items.Count;
        
        /// <summary>
        /// Gets or sets the provider color key for styling
        /// </summary>
        public string ColorKey { get; private set; }
        
        /// <summary>
        /// Gets or sets whether the provider is configured with an API key
        /// </summary>
        public bool IsConfigured { get; set; }
        
        /// <summary>
        /// Creates a new instance of GroupedModels
        /// </summary>
        public GroupedModels(string provider, IEnumerable<AIModelItemViewModel> models = null) : base(models)
        {
            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            DisplayName = NormalizeProviderName(provider);
            ColorKey = provider.ToLowerInvariant();
            
            if (models != null)
            {
                foreach (var model in models)
                {
                    Add(model);
                }
            }
        }
        
        /// <summary>
        /// Formats a provider name for display (e.g., "openai" -> "OpenAI")
        /// </summary>
        private string NormalizeProviderName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;
                
            // Handle known provider names with specific formatting
            switch (name.ToLowerInvariant())
            {
                case "openai":
                    return "OpenAI";
                case "openrouter":
                    return "OpenRouter";
                case "anthropic":
                    return "Anthropic";
                case "groq":
                    return "Groq";
                case "mistral":
                    return "Mistral AI";
                case "google":
                    return "Google AI";
                default:
                    // Capitalize first letter, keep rest as is
                    if (name.Length == 0)
                        return string.Empty;
                    if (name.Length == 1)
                        return name.ToUpperInvariant();
                    return char.ToUpperInvariant(name[0]) + name.Substring(1);
            }
        }
    }
}
