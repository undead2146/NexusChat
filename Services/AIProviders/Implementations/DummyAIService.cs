using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Core.Models;
using NexusChat.Services.Interfaces;

namespace NexusChat.Services.AIProviders.Implementations
{
    /// <summary>
    /// A dummy AI service for testing and development
    /// </summary>
    public class DummyAIService : IAIService
    {
        private readonly Random _random = new Random();
        private readonly string _modelName;
        private readonly ModelCapabilities _capabilities;

        /// <summary>
        /// Gets the name of the model
        /// </summary>
        public string ModelName => _modelName;

        /// <summary>
        /// Gets the provider name
        /// </summary>
        public string ProviderName => "Dummy";

        /// <summary>
        /// Gets whether streaming is supported
        /// </summary>
        public bool SupportsStreaming => _capabilities.SupportsStreaming;

        /// <summary>
        /// Gets the maximum context window size
        /// </summary>
        public int MaxContextWindow => _capabilities.MaxContextWindow;

        /// <summary>
        /// Creates a new instance of DummyAIService
        /// </summary>
        /// <param name="modelName">The name of the model to simulate</param>
        public DummyAIService(string modelName = "DummyModel")
        {
            _modelName = modelName;
            _capabilities = new ModelCapabilities
            {
                MaxTokens = 4096,
                MaxContextWindow = 8192,
                DefaultTemperature = 0.7f,
                SupportsCodeCompletion = true,
                SupportsImageGeneration = false,
                SupportsFunctionCalling = false,
                SupportsStreaming = true,
                SupportedLanguages = new[] { "en" }
            };
        }

        /// <summary>
        /// Sends a message to the dummy AI service
        /// </summary>
        public async Task<string> SendMessageAsync(string prompt, CancellationToken cancellationToken)
        {
            // Simulate processing delay
            int delayMs = _random.Next(500, 2000);
            await Task.Delay(delayMs, cancellationToken);

            // Handle empty prompts
            if (string.IsNullOrWhiteSpace(prompt))
            {
                return "I didn't receive any input. How can I help you?";
            }

            // Generate response based on prompt content
            string lowercasePrompt = prompt.ToLowerInvariant();

            if (cancellationToken.IsCancellationRequested)
            {
                return "The operation was canceled.";
            }

            if (lowercasePrompt.Contains("hello") || lowercasePrompt.Contains("hi"))
            {
                return "Hello! I'm a dummy AI assistant. How can I help you today?";
            }
            else if (lowercasePrompt.Contains("name"))
            {
                return "My name is DummyGPT, a simulated AI assistant for development purposes.";
            }
            else if (lowercasePrompt.Contains("weather"))
            {
                return "I'm sorry, I can't check the weather as I'm just a dummy assistant for testing.";
            }
            else if (lowercasePrompt.Contains("help"))
            {
                return "I'm a dummy assistant created for development and testing. I can simulate responses but don't have actual AI capabilities.";
            }
            else if (lowercasePrompt.Contains("code") || lowercasePrompt.Contains("programming"))
            {
                return "Here's a sample code snippet for testing:\n\n```csharp\npublic class HelloWorld\n{\n    public static void Main()\n    {\n        Console.WriteLine(\"Hello, World!\");\n    }\n}\n```";
            }
            else
            {
                return $"This is a simulated response from the dummy AI service using model '{ModelName}'. Your input was: \"{prompt}\"";
            }
        }

        /// <summary>
        /// Sends a message to the dummy AI and simulates streaming the response
        /// </summary>
        public async Task<Stream> SendStreamedMessageAsync(string prompt, CancellationToken cancellationToken, Action<string> onMessageUpdate)
        {
            // Generate the complete response first
            string completeResponse = await SendMessageAsync(prompt, cancellationToken);
            
            // Split it into chunks to simulate streaming
            await SimulateStreamingResponse(completeResponse, onMessageUpdate, cancellationToken);
            
            // Return the complete response as a stream
            byte[] responseBytes = Encoding.UTF8.GetBytes(completeResponse);
            return new MemoryStream(responseBytes);
        }

        /// <summary>
        /// Gets the capabilities of the dummy model
        /// </summary>
        public Task<ModelCapabilities> GetCapabilitiesAsync()
        {
            return Task.FromResult(_capabilities);
        }

        /// <summary>
        /// Estimates tokens in the provided text
        /// </summary>
        public int EstimateTokens(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;
            
            // Simple estimation: ~4 characters per token on average
            return text.Length / 4 + 1;
        }

        /// <summary>
        /// Simulates streaming a response by sending it in chunks
        /// </summary>
        private async Task SimulateStreamingResponse(string response, Action<string> onMessageUpdate, CancellationToken cancellationToken)
        {
            // Split the response into words
            string[] words = response.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            StringBuilder builder = new StringBuilder();
            
            // Send words in chunks of 1-5 words at a time
            for (int i = 0; i < words.Length; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                // Add current word
                builder.Append(words[i]);
                if (i < words.Length - 1)
                    builder.Append(' '); // Add space if not the last word
                
                // Send update every 1-5 words or at the end
                if (i % _random.Next(1, 6) == 0 || i == words.Length - 1)
                {
                    onMessageUpdate?.Invoke(builder.ToString());
                    
                    // Random typing speed
                    int delay = _random.Next(25, 100);
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }
    }
}
