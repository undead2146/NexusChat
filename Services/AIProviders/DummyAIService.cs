using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Services.Interfaces;

namespace NexusChat.Services.AIProviders
{
    /// <summary>
    /// A dummy AI service implementation for development and testing
    /// </summary>
    public class DummyAIService : IAIService
    {
        // Pre-defined responses for different types of queries
        private readonly Dictionary<string, string> _responseTemplates = new Dictionary<string, string>
        {
            ["default"] = "I'm a dummy AI model for testing. Your message was: \"{0}\"",
            ["greeting"] = "Hello! I'm a test AI assistant. How can I help you today?",
            ["question"] = "That's an interesting question about {0}. As a test AI, I would normally provide comprehensive information on this topic.",
            ["code"] = "Here's a simple example of {0} code:\n\n```\n// Sample code\nfunction helloWorld() {{\n  console.log(\"Hello, world!\");\n}}\n```\n\nThis is just a placeholder since I'm a test AI.",
            ["explanation"] = "I would explain {0} in detail if I were a real AI model. This is just a placeholder response for development purposes.",
            ["list"] = "Here are some key points about {0}:\n\n1. First important point\n2. Second important point\n3. Third important point\n\nThis is a test response format.",
            ["error"] = "I'm sorry, but I encountered an error processing your request about {0}. Please try again or rephrase your question."
        };
        
        // Random phrases to make responses more varied
        private readonly string[] _fillerPhrases = new string[]
        {
            "As a test AI, ",
            "For demonstration purposes, ",
            "If I were a real AI model, ",
            "In a production environment, ",
            "To simulate a response, "
        };

        private readonly Random _random = new Random();
        
        /// <summary>
        /// Gets the name of the AI model
        /// </summary>
        public string ModelName => "Dummy Model v1.0";
        
        /// <summary>
        /// Gets the name of the provider
        /// </summary>
        public string ProviderName => "NexusChat Development";
        
        /// <summary>
        /// Sends a message to the dummy AI service and gets a simulated response
        /// </summary>
        public async Task<string> SendMessageAsync(string prompt, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return "You didn't provide any input. How can I help you?";
            
            try
            {
                Debug.WriteLine($"DummyAIService: Processing message: {prompt}");
                
                // Simulate thinking time
                int thinkingTime = _random.Next(500, 2000);
                await Task.Delay(thinkingTime, cancellationToken);
                
                // Check for cancellation
                cancellationToken.ThrowIfCancellationRequested();
                
                // Generate response
                string response = GenerateResponse(prompt);
                
                // Simulate typing time (longer for longer responses)
                int typingTime = Math.Min(response.Length * 10, 3000);
                await Task.Delay(typingTime, cancellationToken);
                
                return response;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("DummyAIService: Operation was canceled");
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DummyAIService error: {ex.Message}");
                return $"Sorry, I encountered an error: {ex.Message}";
            }
        }
        
        /// <summary>
        /// Gets information about the model's capabilities
        /// </summary>
        public Task<ModelCapabilities> GetCapabilitiesAsync()
        {
            return Task.FromResult(new ModelCapabilities
            {
                MaxTokens = 2048,
                SupportsImageGeneration = false,
                SupportsCodeCompletion = true,
                SupportsFunctionCalling = false,
                DefaultTemperature = 0.7f
            });
        }
        
        /// <summary>
        /// Estimates the number of tokens in the given text
        /// </summary>
        public int EstimateTokens(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;
                
            // Very simple estimate: ~4 characters per token
            return (text.Length + 3) / 4;
        }
        
        /// <summary>
        /// Generates a response based on the input prompt
        /// </summary>
        private string GenerateResponse(string prompt)
        {
            string lowerPrompt = prompt.ToLower();
            string responseTemplate;
            string keyTopic = ExtractTopic(prompt);
            
            // Select response template based on input
            if (lowerPrompt.Contains("hello") || lowerPrompt.Contains("hi ") || lowerPrompt.Contains("hey"))
            {
                responseTemplate = _responseTemplates["greeting"];
                return responseTemplate;
            }
            else if (lowerPrompt.Contains("?"))
            {
                responseTemplate = _responseTemplates["question"];
                return string.Format(responseTemplate, keyTopic);
            }
            else if (lowerPrompt.Contains("code") || lowerPrompt.Contains("program") || lowerPrompt.Contains("function"))
            {
                responseTemplate = _responseTemplates["code"];
                return string.Format(responseTemplate, keyTopic);
            }
            else if (lowerPrompt.Contains("explain") || lowerPrompt.Contains("what is") || lowerPrompt.Contains("how does"))
            {
                responseTemplate = _responseTemplates["explanation"];
                return string.Format(responseTemplate, keyTopic);
            }
            else if (lowerPrompt.Contains("list") || lowerPrompt.Contains("what are") || lowerPrompt.Contains("benefits"))
            {
                responseTemplate = _responseTemplates["list"];
                return string.Format(responseTemplate, keyTopic);
            }
            else
            {
                // Add variety with a random filler phrase
                string fillerPhrase = _fillerPhrases[_random.Next(_fillerPhrases.Length)];
                responseTemplate = fillerPhrase + _responseTemplates["default"];
                return string.Format(responseTemplate, prompt);
            }
        }
        
        /// <summary>
        /// Extracts a key topic from the prompt
        /// </summary>
        private string ExtractTopic(string prompt)
        {
            // Simple extraction of a potential topic
            string[] words = prompt.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            // If prompt is very short, just return it
            if (words.Length <= 3)
                return prompt;
                
            // Find nouns (simplistic approach: longer words that aren't stopwords)
            HashSet<string> stopwords = new HashSet<string>(new[] 
            { 
                "the", "a", "an", "and", "or", "but", "if", "of", "to", "in", "is", "it", "that", "for", "on", "with", "as", "this", "by" 
            });
            
            foreach (string word in words)
            {
                if (word.Length > 4 && !stopwords.Contains(word.ToLower()))
                    return word;
            }
            
            // Fallback to using the first few words
            return string.Join(" ", words.Length > 5 ? words[..5] : words);
        }
    }
}
