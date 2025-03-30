using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Maui.Controls;
using NexusChat.Core.Models;

namespace NexusChat.Views.Converters
{
    /// <summary>
    /// Determines if a message is the last AI message in the collection
    /// </summary>
    public class LastAIMessageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Check if the incoming parameter is a reference to the message
            if (!(parameter is Message currentMessage))
            {
                return false;
            }

            // Check if we have a valid collection
            IEnumerable<Message> messages = null;
            if (value is ObservableCollection<Message> observableMessages)
            {
                messages = observableMessages;
            }
            else if (value is IEnumerable<Message> enumerable)
            {
                messages = enumerable;
            }
            
            if (messages == null)
            {
                return false;
            }
            
            // If this is an AI message with "Typing" status, it's considered the last message
            if (currentMessage.IsAI && currentMessage.Status == "Typing")
            {
                return true;
            }
            
            // Find the last AI message in the collection
            var lastAIMessage = messages.LastOrDefault(m => m.IsAI);
            
            // Check if this message is the last AI message
            return lastAIMessage != null && lastAIMessage.Id == currentMessage.Id;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
