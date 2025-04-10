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
    /// AI Service implementation for the OpenRouter API
    /// </summary>
    public class OpenRouterAIService : BaseAIService
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
        public override string ProviderName => "OpenRouter";
        
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
        /// Gets the base URL for OpenRouter API requests
        /// </summary>
        private string BaseUrl => "https://openrouter.ai/api/v1/chat/completions";

        /// <summary>
        /// Creates a new instance of the OpenRouter AI service
        /// </summary>
        /// <param name="apiKeyManager">The API key manager</param>
        /// <param name="modelName">The model to use</param>
        public OpenRouterAIService(IApiKeyManager apiKeyManager, string modelName = "anthropic/claude-3-opus")
            : base(apiKeyManager)
        {
            _selectedModel = modelName ?? "anthropic/claude-3-opus";
            
            // Initialize model configurations from environment variables or defaults
            _modelConfigs = new Dictionary<string, ModelCapabilities>(StringComparer.OrdinalIgnoreCase)
            {
                // Claude models
                ["anthropic/claude-3-opus"] = new ModelCapabilities 
                { 
                    MaxTokens = 8192, 
                    MaxContextWindow = 200000, 
                    SupportsStreaming = true,
                    DefaultTemperature = 0.7f,
                    SupportsCodeCompletion = true
                },
                ["anthropic/claude-3-sonnet"] = new ModelCapabilities 
                { 
                    MaxTokens = 4096, 
                    MaxContextWindow = 180000, 
                    SupportsStreaming = true,
                    DefaultTemperature = 0.7f,
                    SupportsCodeCompletion = true
                },
                
                // Gemini models
                ["google/gemini-pro"] = new ModelCapabilities 
                { 
                    MaxTokens = 4096, 
                    MaxContextWindow = 32768, 
                    SupportsStreaming = true,
                    DefaultTemperature = 0.7f
                },
                ["gemini-pro-2-5"] = new ModelCapabilities 
                { 
                    MaxTokens = 4096, 
                    MaxContextWindow = 32768, 
                    SupportsStreaming = true,
                    DefaultTemperature = 0.7f
                },
                ["gemini-2-0-flash-thinking"] = new ModelCapabilities 
                { 
                    MaxTokens = 4096, 
                    MaxContextWindow = 32768, 
                    SupportsStreaming = true,
                    DefaultTemperature = 0.7f
                },
                ["gemini-2-0-flash-lite-preview"] = new ModelCapabilities 
                { 
                    MaxTokens = 4096, 
                    MaxContextWindow = 16384, 
                    SupportsStreaming = true,
                    DefaultTemperature = 0.7f
                },
                
                // DeepSeek models
                ["deepseek-v3"] = new ModelCapabilities 
                { 
                    MaxTokens = 4096, 
                    MaxContextWindow = 16384, 
                    SupportsStreaming = true,
                    DefaultTemperature = 0.7f
                },
                ["deepseek-r1-zero"] = new ModelCapabilities 
                { 
                    MaxTokens = 4096, 
                    MaxContextWindow = 16384, 
                    SupportsStreaming = true,
                    DefaultTemperature = 0.7f
                },
                ["deepseek-r1-distill-llama-70b"] = new ModelCapabilities 
                { 
                    MaxTokens = 4096, 
                    MaxContextWindow = 16384, 
                    SupportsStreaming = true,
                    DefaultTemperature = 0.7f
                },
                
                // Mistral models
                ["mistral-small-3-1-24b"] = new ModelCapabilities 
                { 
                    MaxTokens = 4096, 
                    MaxContextWindow = 32768, 
                    SupportsStreaming = true,
                    DefaultTemperature = 0.7f
                },
                ["molmo-7bd"] = new ModelCapabilities 
                { 
                    MaxTokens = 4096, 
                    MaxContextWindow = 8192, 
                    SupportsStreaming = true,
                    DefaultTemperature = 0.7f
                }
            };
        }

        /// <summary>
        /// Configures the HTTP client with OpenRouter-specific settings
        /// </summary>
        protected override void ConfigureHttpClient()
        {
            base.ConfigureHttpClient();
            
            // Add OpenRouter-specific headers
            HttpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://nexuschat.app");
            HttpClient.DefaultRequestHeaders.Add("X-Title", "NexusChat");
        }

        /// <summary>
        /// Sends a message to the OpenRouter API
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
            }, "Error calling OpenRouter API");
        }

        /// <summary>
        /// Sends a message to the OpenRouter API with streaming response
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
            }, "Error calling OpenRouter streaming API");
        }
        
        /// <summary>
        /// Gets the API key for this model with fallbacks
        /// </summary>
        private string GetApiKey()
        {
            // For OpenRouter, model names can include slashes, so normalize them
            string normalizedModel = _selectedModel.Replace('/', '_').ToUpperInvariant();
            
            // Try model-specific key first
            string apiKey = ApiKeyManager.GetApiKey($"OPENROUTER_{normalizedModel}");
            
            // If not found, try generic OpenRouter key
            if (string.IsNullOrEmpty(apiKey))
            {
                apiKey = ApiKeyManager.GetApiKey("OPENROUTER");
            }
            
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException($"No API key found for OpenRouter model {_selectedModel}");
            }
            
            return apiKey;
        }
        
        /// <summary>
        /// Estimates tokens for text
        /// </summary>
        public override int EstimateTokens(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;
                
            // Different models have different tokenization ratios
            if (_selectedModel.Contains("claude"))
            {
                // Claude tends to be more efficient with tokenization
                return text.Length / 5 + 1; 
            }
            else if (_selectedModel.Contains("gemini"))
            {
                // Gemini similar to Claude
                return text.Length / 5 + 1;
            }
            
            // Default estimation
            return text.Length / 4 + 1;
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
