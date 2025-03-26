using System;
using Microsoft.Maui.Controls;
using NexusChat.Core.Models;

namespace NexusChat.Views.Pages.DevTools
{
    /// <summary>
    /// Data template selector for different entity types in the ModelTestingPage
    /// </summary>
    public class EntityDataTemplateSelector : DataTemplateSelector
    {
        /// <summary>
        /// Template for User entities
        /// </summary>
        public DataTemplate UserTemplate { get; set; }
        
        /// <summary>
        /// Template for Conversation entities
        /// </summary>
        public DataTemplate ConversationTemplate { get; set; }
        
        /// <summary>
        /// Template for Message entities
        /// </summary>
        public DataTemplate MessageTemplate { get; set; }
        
        /// <summary>
        /// Template for AIModel entities
        /// </summary>
        public DataTemplate ModelTemplate { get; set; }
        
        /// <summary>
        /// Default template for other entity types
        /// </summary>
        public DataTemplate DefaultTemplate { get; set; }

        /// <summary>
        /// Selects the appropriate template based on item type
        /// </summary>
        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            if (item is User)
                return UserTemplate;
                
            if (item is Conversation)
                return ConversationTemplate;
                
            if (item is Message)
                return MessageTemplate;
                
            if (item is AIModel)
                return ModelTemplate;
                
            return DefaultTemplate;
        }
    }
}
