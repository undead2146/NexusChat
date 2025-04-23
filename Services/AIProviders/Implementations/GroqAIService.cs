using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Core.Models;
using NexusChat.Services.Interfaces;

namespace NexusChat.Services.AIProviders.Implementations
{
    /// <summary>
    /// AI Service implementation for the Groq API
    /// </summary>
    public class GroqAIService : BaseAIService
    {
        private readonly string _selectedModel;
        private readonly Dictionary<string, ModelCapabilities> _modelConfigs;
        
        /// <summary>
        /// Gets the name of the model being used
        /// </summary>
        public override string ModelName => _selectedModel;
        
        /// <summary>
        /// Gets the provider name
        /// </summary>
        public override string ProviderName => "Groq";
        
        /// <summary>
        /// Gets whether streaming is supported by this model
        /// </summary>
        public override bool SupportsStreaming => 
            _modelConfigs.TryGetValue(_selectedModel, out var capabilities) 
                ? capabilities.SupportsStreaming 
                : base.SupportsStreaming;
        
        /// <summary>
        /// Gets the maximum context window for this model
        /// </summary>
        public override int MaxContextWindow => 
            _modelConfigs.TryGetValue(_selectedModel, out var capabilities) 
                ? capabilities.MaxContextWindow 
                : base.MaxContextWindow;
        
        /// <summary>
        /// Gets the base URL for Groq API requests
        /// </summary>
        private string BaseUrl => "https://api.groq.com/openai/v1/chat/completions";

        /// <summary>
        /// Creates a new instance of the Groq AI service
        /// </summary>
        /// <param name="apiKeyManager">The API key manager</param>
        /// <param name="modelName">The model to use</param>
        public GroqAIService(IApiKeyManager apiKeyManager, string modelName = "llama3-70b-8192")
            : base(apiKeyManager)
        {
            _selectedModel = modelName ?? "llama3-70b-8192";
            
            // Initialize model configurations from environment variables or defaults
            _modelConfigs = new Dictionary<string, ModelCapabilities>(StringComparer.OrdinalIgnoreCase)
            {
                // Production models
                ["llama3-70b-8192"] = new ModelCapabilities 
                { 
                    MaxTokens = 4096, 
                    MaxContextWindow = 8192, 
                    SupportsStreaming = true,
                    DefaultTemperature = 0.7f,
                    SupportsCodeCompletion = true
                },
                ["llama3-8b-8192"] = new ModelCapabilities 
                { 
                    MaxTokens = 4096, 
                    MaxContextWindow = 8192, 
                    SupportsStreaming = true,
                    DefaultTemperature = 0.7f
                },
                ["mixtral-8x7b-32768"] = new ModelCapabilities 
                { 
                    MaxTokens = 8192, 
                    MaxContextWindow = 32768, 
                    SupportsStreaming = true,
                    DefaultTemperature = 0.7f
                },
                ["gemma-7b-it"] = new ModelCapabilities 
                { 
                    MaxTokens = 4096, 
                    MaxContextWindow = 8192, 
                    SupportsStreaming = true,
                    DefaultTemperature = 0.7f
                },
                ["llama-3-3-70b-versatile"] = new ModelCapabilities 
                { 
                    MaxTokens = 4096, 
                    MaxContextWindow = 8192, 
                    SupportsStreaming = true,
                    DefaultTemperature = 0.7f
                },
                ["llama-3-1-8b-instant"] = new ModelCapabilities 
                { 
                    MaxTokens = 2048, 
                    MaxContextWindow = 8192, 
                    SupportsStreaming = true,
                    DefaultTemperature = 0.7f
                },
                ["llama-guard-3-8b"] = new ModelCapabilities 
                { 
                    MaxTokens = 2048, 
                    MaxContextWindow = 8192, 
                    SupportsStreaming = true,
                    DefaultTemperature = 0.2f
                },
                
                // Preview models
                ["llama-3-3-70b-specdec"] = new ModelCapabilities 
                { 
                    MaxTokens = 4096, 
                    MaxContextWindow = 32768, 
                    SupportsStreaming = true,
                    DefaultTemperature = 0.7f
                },
                ["qwen-2-5-32b"] = new ModelCapabilities 
                { 
                    MaxTokens = 4096, 
                    MaxContextWindow = 32768, 
                    SupportsStreaming = true,
                    DefaultTemperature = 0.7f
                },
                ["qwen-2-5-coder-32b"] = new ModelCapabilities 
                { 
                    MaxTokens = 4096, 
                    MaxContextWindow = 32768, 
                    SupportsStreaming = true,
                    DefaultTemperature = 0.7f,
                    SupportsCodeCompletion = true
                },
                ["deepseek-r1-distill-llama-70b"] = new ModelCapabilities 
                { 
                    MaxTokens = 4096, 
                    MaxContextWindow = 16384, 
                    SupportsStreaming = true,
                    DefaultTemperature = 0.7f
                }
                // Add other models as needed
            };
        }

        /// <summary>
        /// Sends a message to the Groq API
        /// </summary>
        public override async Task<string> SendMessageAsync(string prompt, CancellationToken cancellationToken)
        {
            return await HandleApiRequestAsync(async () =>
            {
                // Get API key with fallbacks
                string apiKey = GetApiKey();
                
                // Prepare request
                var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                // Create payload
                var payload = new
                {
                    model = _selectedModel,
                    messages = new[] { new { role = "user", content = prompt } },
                    temperature = _modelConfigs.TryGetValue(_selectedModel, out var config) 
                        ? config.DefaultTemperature 
                        : 0.7f,
                    max_tokens = _modelConfigs.TryGetValue(_selectedModel, out var cap) 
                        ? Math.Min(cap.MaxTokens, 4096)  
                        : 4096
                };

                var jsonContent = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                // Send request
                var response = await HttpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();
                
                // Extract content from response
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                using var document = JsonDocument.Parse(responseContent);
                
                string content = document.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();
                    
                return content;
            }, "Error calling Groq API");
        }

        /// <summary>
        /// Sends a message to the Groq API with streaming response
        /// </summary>
        public override async Task<Stream> SendStreamedMessageAsync(string prompt, CancellationToken cancellationToken, Action<string> onMessageUpdate)
        {
            return await HandleApiRequestAsync(async () =>
            {
                // Get API key with fallbacks
                string apiKey = GetApiKey();
                
                // Prepare request
                var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                
                // Create payload with streaming enabled
                var payload = new
                {
                    model = _selectedModel,
                    messages = new[] { new { role = "user", content = prompt } },
                    temperature = _modelConfigs.TryGetValue(_selectedModel, out var config) 
                        ? config.DefaultTemperature 
                        : 0.7f,
                    max_tokens = _modelConfigs.TryGetValue(_selectedModel, out var cap) 
                        ? Math.Min(cap.MaxTokens, 4096)  
                        : 4096,
                    stream = true
                };
                
                var jsonContent = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                // Send request
                var response = await HttpClient.SendAsync(
                    request, 
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);
                    
                response.EnsureSuccessStatusCode();
                
                // Get the stream
                var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                
                // Process stream in background
                _ = ProcessStreamAsync(stream, onMessageUpdate, cancellationToken);
                
                return stream;
            }, "Error calling Groq streaming API");
        }
        
        /// <summary>
        /// Gets the API key for this model with fallbacks
        /// </summary>
        private string GetApiKey()
        {
            // Try model-specific key first (e.g., AI_KEY_GROQ_LLAMA3_70B_8192)
            string normalizedModel = _selectedModel.Replace('-', '_').ToUpperInvariant();
            string apiKey = ApiKeyManager.GetApiKey($"GROQ_{normalizedModel}");
            
            // If not found, try generic Groq key
            if (string.IsNullOrEmpty(apiKey))
            {
                apiKey = ApiKeyManager.GetApiKey("GROQ");
            }
            
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException($"No API key found for Groq model {_selectedModel}");
            }
            
            return apiKey;
        }
        
        /// <summary>
        /// Processes a streaming response
        /// </summary>
        private async Task ProcessStreamAsync(Stream stream, Action<string> onMessageUpdate, CancellationToken cancellationToken)
        {
            using var reader = new StreamReader(stream);
            var fullResponse = new StringBuilder();
            
            try
            {
                string line;
                while (!cancellationToken.IsCancellationRequested && 
                      (line = await reader.ReadLineAsync()) != null)
                {
                    // Check for data prefix and skip empty lines
                    if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: "))
                        continue;
                        
                    // Remove "data: " prefix
                    line = line.Substring(6);
                    
                    // Check for stream completion
                    if (line == "[DONE]")
                        break;
                        
                    try
                    {
                        // Parse the JSON
                        using var jsonDoc = JsonDocument.Parse(line);
                        var root = jsonDoc.RootElement;
                        
                        // Extract content
                        if (root.TryGetProperty("choices", out var choices) &&
                            choices.GetArrayLength() > 0)
                        {
                            var choice = choices[0];
                            
                            if (choice.TryGetProperty("delta", out var delta) &&
                                delta.TryGetProperty("content", out var content))
                            {
                                string contentText = content.GetString();
                                if (!string.IsNullOrEmpty(contentText))
                                {
                                    fullResponse.Append(contentText);
                                    onMessageUpdate?.Invoke(fullResponse.ToString());
                                }
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        // Skip malformed lines
                        continue;
                    }
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                Debug.WriteLine($"Error processing stream: {ex.Message}");
            }
        }
    }
}
