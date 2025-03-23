using System;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Services.Interfaces;

namespace NexusChat.Services.AIProviders
{
    /// <summary>
    /// Dummy implementation of IAIService for testing
    /// </summary>
    public class DummyAIService : IAIService
    {
        private static readonly string[] _responseTemplates = new[]
        {
            "I understand what you're asking about {0}. Let me help with that.",
            "That's an interesting question about {0}. Here's what I know.",
            "When it comes to {0}, there are several things to consider.",
            "I'd be happy to discuss {0} with you. Here's my perspective.",
            "Thanks for asking about {0}. Here's some information that might help."
        };
        
        private static readonly Random _random = new Random();
        
        /// <summary>
        /// Gets the name of the current AI model
        /// </summary>
        public string ModelName { get; } = "Dummy AI Model";
        
        /// <summary>
        /// Gets the provider name
        /// </summary>
        public string ProviderName { get; } = "NexusChat";
        
        /// <summary>
        /// Sends a message to the dummy AI service and gets a response
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>The AI's response text</returns>
        public async Task<string> SendMessageAsync(string message, CancellationToken cancellationToken = default)
        {
            // Extract topic from message for more realistic responses
            string topic = ExtractTopic(message);
            
            // Get random response template
            string template = _responseTemplates[_random.Next(_responseTemplates.Length)];
            
            // Build basic response
            string response = string.Format(template, topic);
            
            // Add some additional content based on message length
            if (message.Length > 20)
            {
                response += "\n\nI notice you provided quite a bit of detail in your question. " +
                          "That's helpful for providing a more specific response. " +
                          "Let me elaborate further on this topic.";
            }
            
            if (message.Contains("?"))
            {
                response += "\n\nTo directly answer your question: yes, that's generally correct, " +
                          "though there are some nuances to consider depending on the specific context.";
            }
            
            // Add a standard closing
            response += "\n\nIs there anything specific about this topic you'd like me to explain in more detail?";
            
            // Simulate network delay
            await Task.Delay(_random.Next(500, 2000), cancellationToken);
            
            return response;
        }
        
        private string ExtractTopic(string message)
        {
            // Very basic topic extraction - in a real AI, this would be much more sophisticated
            string topic = message;
            
            // Try to extract a key phrase
            if (message.Contains("about "))
            {
                int index = message.IndexOf("about ");
                if (index >= 0 && index + 6 < message.Length)
                {
                    string afterAbout = message.Substring(index + 6);
                    int endIndex = afterAbout.IndexOf('.');
                    if (endIndex > 0)
                    {
                        topic = afterAbout.Substring(0, endIndex);
                    }
                    else
                    {
                        topic = afterAbout;
                    }
                }
            }
            
            // Limit length
            if (topic.Length > 30)
            {
                topic = topic.Substring(0, 27) + "...";
            }
            
            return topic;
        }
    }
}
