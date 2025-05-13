using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Core.Models;
using NexusChat.Services.Interfaces;

namespace NexusChat.Services.AIProviders.Implementations
{
    /// <summary>
    /// A dummy AI service for testing that doesn't make actual API calls
    /// </summary>
    public class DummyAIService : BaseAIService
    {
        private readonly string _selectedModel;
        private static readonly Dictionary<string, AIModel> _models = new Dictionary<string, AIModel>(StringComparer.OrdinalIgnoreCase)
        {
            ["dummygpt"] = new AIModel 
            { 
                ModelName = "dummygpt", 
                ProviderName = "Dummy", 
                Description = "Fake model for testing",
                MaxTokens = 2000,
                MaxContextWindow = 8000,
                SupportsStreaming = true,
                DefaultTemperature = 0.7f,
                IsAvailable = true
            }
        };
        
        private readonly Random _random = new Random();

        public override string ModelName => _selectedModel;
        public override string ProviderName => "Dummy";
        
        /// <summary>
        /// Creates a new instance of DummyAIService
        /// </summary>
        public DummyAIService(IApiKeyManager apiKeyManager, string modelName = "dummygpt")
            : base(apiKeyManager, _models.TryGetValue(modelName ?? "dummygpt", out var model) ? model : null)
        {
            _selectedModel = modelName ?? "dummygpt";
        }
        
        /// <summary>
        /// Gets capabilities of the selected model
        /// </summary>
        public AIModel GetModelCapabilities(string modelName)
        {
            if (_models.TryGetValue(modelName, out var model))
            {
                return model;
            }
            
            return null;
        }
        
        /// <summary>
        /// Gets all available models
        /// </summary>
        public List<AIModel> GetAvailableModels()
        {
            var models = new List<AIModel>();
            
            foreach (var entry in _models)
            {
                models.Add(entry.Value);
            }
            
            return models;
        }

        /// <summary>
        /// Sends a message to the dummy AI service
        /// </summary>
        public override async Task<string> SendMessageAsync(string prompt, CancellationToken cancellationToken)
        {
            // Simulate network delay
            int delay = _random.Next(300, 1500);
            await Task.Delay(delay, cancellationToken);
            
            // Check for cancellation
            cancellationToken.ThrowIfCancellationRequested();
            
            // Generate dummy response
            string response = GenerateDummyResponse(prompt);
            
            Debug.WriteLine($"DummyAIService generated response in {delay}ms");
            return response;
        }

        /// <summary>
        /// Sends a message with streaming response
        /// </summary>
        public override async Task<Stream> SendStreamedMessageAsync(string prompt, CancellationToken cancellationToken, Action<string> onMessageUpdate)
        {
            // Simulate streaming response
            string[] words = GenerateDummyResponse(prompt).Split(' ');
            var responseStream = new MemoryStream();
            var writer = new StreamWriter(responseStream);
            
            // Process stream in background with word-by-word updates
            _ = Task.Run(async () =>
            {
                var fullResponse = new StringBuilder();
                
                foreach (string word in words)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                        
                    // Append word to response
                    fullResponse.Append(word).Append(" ");
                    onMessageUpdate?.Invoke(fullResponse.ToString());
                    
                    // Simulate variable thinking time
                    await Task.Delay(_random.Next(50, 250), cancellationToken);
                }
                
                Debug.WriteLine("DummyAIService streaming completed");
            }, cancellationToken);
            
            return responseStream;
        }
        
        /// <summary>
        /// Gets the capabilities of this model
        /// </summary>
        public override Task<AIModel> GetCapabilitiesAsync()
        {
            if (_models.TryGetValue(_selectedModel, out var model))
            {
                return Task.FromResult(model);
            }
            
            // Return default capabilities if model not found
            return Task.FromResult(new AIModel
            {
                ModelName = _selectedModel,
                ProviderName = "Dummy",
                MaxTokens = 2000,
                MaxContextWindow = 8000,
                SupportsStreaming = true,
                DefaultTemperature = 0.7f
            });
        }

        #region Helper Methods

        /// <summary>
        /// Generates a dummy response based on the input prompt
        /// </summary>
        private string GenerateDummyResponse(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return "I didn't receive a question. Please ask me something.";
                
            if (prompt.Contains("hello", StringComparison.OrdinalIgnoreCase) || 
                prompt.Contains("hi ", StringComparison.OrdinalIgnoreCase))
            {
                return "Hello! I'm DummyGPT, a test AI model that doesn't actually make API calls. " +
                       "I'm here to help with testing the interface without using actual API resources.";
            }
            
            if (prompt.Contains("name", StringComparison.OrdinalIgnoreCase))
            {
                return "I'm DummyGPT, a simulated AI assistant used for testing the NexusChat application. " +
                       "I don't use any actual AI services or make API calls.";
            }
            
            // Default responses for various prompt lengths
            if (prompt.Length < 20)
            {
                return "That's a short question! I'm DummyGPT, simulating a response for testing purposes. " +
                       "Your actual AI would provide a meaningful answer here based on your input.";
            }
            else if (prompt.Length < 50)
            {
                return "I notice your question is of medium length. This is a simulated response to test " +
                       "the chat interface. In a real conversation with an actual AI model, you'd receive " +
                       "a proper response tailored to your specific question.";
            }
            
            // For longer prompts, generate a multi-paragraph response
            return "Thank you for your detailed question. As DummyGPT, I'm generating a simulated multi-paragraph " +
                   "response to help test the chat interface's handling of longer content.\n\n" +
                   
                   "In a real implementation, this would be a thoughtful answer from your selected AI model. " +
                   "The actual response would analyze your question and provide relevant information.\n\n" +
                   
                   "This dummy response simply exists to test the UI, scrolling behavior, and message formatting " +
                   "without making actual API calls or using tokens. It's perfect for development and testing!\n\n" +
                   
                   $"Your original prompt was {prompt.Length} characters long, which would be approximately " +
                   $"{EstimateTokens(prompt)} tokens with a typical tokenizer.";
        }

        #endregion
    }
}
