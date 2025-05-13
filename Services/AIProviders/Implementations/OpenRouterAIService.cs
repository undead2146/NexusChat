using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Core.Models;
using NexusChat.Services.Interfaces;
using RestSharp;

namespace NexusChat.Services.AIProviders.Implementations
{
    /// <summary>
    /// OpenRouter AI provider service implementation
    /// </summary>
    public class OpenRouterAIService : IAIProviderService
    {
        private readonly IApiKeyManager _apiKeyManager;
        private readonly string _modelName;
        private readonly string _baseUrl = "https://openrouter.ai/api/v1";
        private RestClient _client;
        
        /// <summary>
        /// Gets the name of the model
        /// </summary>
        public string ModelName => _modelName;
        
        /// <summary>
        /// Gets the name of the provider
        /// </summary>
        public string ProviderName => "OpenRouter";
        
        /// <summary>
        /// Gets whether streaming is supported
        /// </summary>
        public bool SupportsStreaming => true;
        
        /// <summary>
        /// Gets the maximum context window size
        /// </summary>
        public int MaxContextWindow => GetContextWindowSize(_modelName);
        
        /// <summary>
        /// Creates a new OpenRouter AI service with the specified model name
        /// </summary>
        public OpenRouterAIService(IApiKeyManager apiKeyManager, string modelName = "anthropic/claude-3-sonnet")
        {
            _apiKeyManager = apiKeyManager ?? throw new ArgumentNullException(nameof(apiKeyManager));
            _modelName = !string.IsNullOrEmpty(modelName) ? modelName : "anthropic/claude-3-sonnet";
            _client = new RestClient(_baseUrl);
            
            Debug.WriteLine($"OpenRouterAIService created with model: {_modelName}");
        }
        
        /// <summary>
        /// Gets context window size based on model name
        /// </summary>
        private int GetContextWindowSize(string modelName)
        {
            if (string.IsNullOrEmpty(modelName))
                return 16384; // Default
                
            string name = modelName.ToLowerInvariant();
            
            // Anthropic Claude models
            if (name.Contains("claude-3-opus"))
                return 200000;
                
            if (name.Contains("claude-3-sonnet"))
                return 200000;
                
            if (name.Contains("claude-3-haiku"))
                return 200000;
                
            if (name.Contains("claude-2"))
                return 100000;
                
            // OpenAI models
            if (name.Contains("gpt-4o") || name.Contains("gpt-4-turbo"))
                return 128000;
                
            if (name.Contains("gpt-3.5"))
                return 16385;
                
            // Google models
            if (name.Contains("gemini-1.5"))
                return 128000;
                
            if (name.Contains("gemini-pro"))
                return 32768;
                
            // Mistral models
            if (name.Contains("mistral"))
                return 32768;
                
            // Meta Llama models
            if (name.Contains("llama"))
                return 8192;
                
            return 16384; // Default fallback
        }
        
        /// <summary>
        /// Gets all available models for OpenRouter - Implemented as static for factory usage
        /// </summary>
        public static IEnumerable<AIModel> GetAvailableModels()
        {
            Debug.WriteLine("OpenRouterAIService.GetAvailableModels [static] called");
            
            var models = new List<AIModel>
            {
                // Anthropic models
                new AIModel {
                    ModelName = "anthropic/claude-3-opus",
                    ProviderName = "OpenRouter",
                    Description = "Claude 3 Opus - Anthropic's most powerful model",
                    MaxTokens = 4096,
                    MaxContextWindow = 200000,
                    SupportsStreaming = true,
                    SupportsVision = true,
                    IsAvailable = true,
                    ApiKeyVariable = "API_KEY_OPENROUTER"
                },
                new AIModel {
                    ModelName = "anthropic/claude-3-sonnet",
                    ProviderName = "OpenRouter",
                    Description = "Claude 3 Sonnet - balanced performance & quality",
                    MaxTokens = 4096,
                    MaxContextWindow = 200000,
                    SupportsStreaming = true,
                    SupportsVision = true,
                    IsAvailable = true,
                    ApiKeyVariable = "API_KEY_OPENROUTER"
                },
                new AIModel {
                    ModelName = "anthropic/claude-3-haiku",
                    ProviderName = "OpenRouter",
                    Description = "Claude 3 Haiku - fastest & most compact Claude model",
                    MaxTokens = 4096,
                    MaxContextWindow = 200000,
                    SupportsStreaming = true,
                    SupportsVision = true,
                    IsAvailable = true,
                    ApiKeyVariable = "API_KEY_OPENROUTER"
                },
                
                // Google models
                new AIModel {
                    ModelName = "google/gemini-pro",
                    ProviderName = "OpenRouter",
                    Description = "Google's Gemini Pro model",
                    MaxTokens = 8192,
                    MaxContextWindow = 32768,
                    SupportsStreaming = true,
                    IsAvailable = true,
                    ApiKeyVariable = "API_KEY_OPENROUTER"
                },
                new AIModel {
                    ModelName = "google/gemini-1.5-flash",
                    ProviderName = "OpenRouter",
                    Description = "Gemini 1.5 Flash - Fast response model",
                    MaxTokens = 8192,
                    MaxContextWindow = 128000,
                    SupportsStreaming = true,
                    SupportsVision = true,
                    IsAvailable = true,
                    ApiKeyVariable = "API_KEY_OPENROUTER"
                },
                
                // Mistral models
                new AIModel {
                    ModelName = "mistralai/mistral-small",
                    ProviderName = "OpenRouter",
                    Description = "Mistral Small - efficient and powerful",
                    MaxTokens = 4096,
                    MaxContextWindow = 32768,
                    SupportsStreaming = true,
                    IsAvailable = true,
                    ApiKeyVariable = "API_KEY_OPENROUTER"
                },
                new AIModel {
                    ModelName = "mistralai/mistral-medium",
                    ProviderName = "OpenRouter",
                    Description = "Mistral Medium - versatile model",
                    MaxTokens = 8192,
                    MaxContextWindow = 32768,
                    SupportsStreaming = true,
                    IsAvailable = true,
                    ApiKeyVariable = "API_KEY_OPENROUTER"
                },
                new AIModel {
                    ModelName = "mistralai/mistral-large",
                    ProviderName = "OpenRouter",
                    Description = "Mistral Large - advanced reasoning",
                    MaxTokens = 8192,
                    MaxContextWindow = 32768,
                    SupportsStreaming = true,
                    IsAvailable = true,
                    ApiKeyVariable = "API_KEY_OPENROUTER"
                },
                
                // Meta Llama models
                new AIModel {
                    ModelName = "meta-llama/llama-3-70b-instruct",
                    ProviderName = "OpenRouter",
                    Description = "Llama 3 70B - Meta's flagship model",
                    MaxTokens = 8192,
                    MaxContextWindow = 8192,
                    SupportsStreaming = true,
                    IsAvailable = true,
                    ApiKeyVariable = "API_KEY_OPENROUTER"
                },
                new AIModel {
                    ModelName = "meta-llama/llama-3-8b-instruct",
                    ProviderName = "OpenRouter",
                    Description = "Llama 3 8B - Compact but capable",
                    MaxTokens = 8192,
                    MaxContextWindow = 8192,
                    SupportsStreaming = true,
                    IsAvailable = true,
                    ApiKeyVariable = "API_KEY_OPENROUTER"
                },
                
                // More models
                new AIModel {
                    ModelName = "anthropic/claude-2",
                    ProviderName = "OpenRouter",
                    Description = "Claude 2 - Anthropic's previous generation model",
                    MaxTokens = 4096,
                    MaxContextWindow = 100000,
                    SupportsStreaming = true,
                    IsAvailable = true,
                    ApiKeyVariable = "API_KEY_OPENROUTER"
                },
                new AIModel {
                    ModelName = "openai/gpt-4o",
                    ProviderName = "OpenRouter",
                    Description = "GPT-4o - OpenAI's multimodal model",
                    MaxTokens = 8192,
                    MaxContextWindow = 128000,
                    SupportsStreaming = true,
                    SupportsVision = true,
                    IsAvailable = true,
                    ApiKeyVariable = "API_KEY_OPENROUTER"
                },
                new AIModel {
                    ModelName = "openai/gpt-4-turbo",
                    ProviderName = "OpenRouter",
                    Description = "GPT-4 Turbo - Latest OpenAI model",
                    MaxTokens = 4096,
                    MaxContextWindow = 128000,
                    SupportsStreaming = true,
                    IsAvailable = true,
                    ApiKeyVariable = "API_KEY_OPENROUTER"
                },
                new AIModel {
                    ModelName = "openai/gpt-3.5-turbo",
                    ProviderName = "OpenRouter",
                    Description = "GPT-3.5 Turbo - Fast and economical",
                    MaxTokens = 4096,
                    MaxContextWindow = 16385,
                    SupportsStreaming = true,
                    IsAvailable = true,
                    ApiKeyVariable = "API_KEY_OPENROUTER"
                }
            };
            
            Debug.WriteLine($"OpenRouterAIService: Returning {models.Count} models");
            return models;
        }

        /// <summary>
        /// Checks if a model is supported by OpenRouter
        /// </summary>
        public static bool SupportsModel(string modelName)
        {
            if (string.IsNullOrEmpty(modelName)) 
                return false;
            
            // Normalize model name for comparison
            string normalizedName = modelName.ToLowerInvariant();
            
            // Get all available models
            var availableModels = GetAvailableModels();
            
            // Check for exact match
            if (availableModels.Any(m => m.ModelName.Equals(normalizedName, StringComparison.OrdinalIgnoreCase)))
                return true;
            
            // OpenRouter is special - it supports many models including those with provider/model pattern
            // If the model contains a slash, it's likely a valid OpenRouter model identifier
            if (normalizedName.Contains("/"))
                return true;
            
            // Check for model families
            string[] supportedFamilies = new[] {
                "claude", "gpt", "llama", "mistral", "gemini", "command", "phi"
            };
            
            foreach (var family in supportedFamilies)
            {
                if (normalizedName.Contains(family))
                    return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Sends a message to the OpenRouter API
        /// </summary>
        public async Task<string> SendMessageAsync(string prompt, CancellationToken cancellationToken)
        {
            try
            {
                string apiKey = await GetApiKeyAsync();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return $"Error: No API key found for {ProviderName}";
                }
                
                var request = new RestRequest("/chat/completions", Method.Post);
                request.AddHeader("Authorization", $"Bearer {apiKey}");
                request.AddHeader("Content-Type", "application/json");
                request.AddHeader("HTTP-Referer", "https://nexuschat.app"); // Required by OpenRouter
                request.AddHeader("X-Title", "NexusChat");
                
                var requestBody = new
                {
                    model = _modelName,
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.7,
                    max_tokens = 1024
                };
                
                request.AddJsonBody(requestBody);
                
                var response = await _client.ExecuteAsync(request, cancellationToken);
                if (!response.IsSuccessful)
                {
                    Debug.WriteLine($"OpenRouter API error: {response.StatusCode} - {response.Content}");
                    return $"Error communicating with OpenRouter: {response.ErrorMessage ?? response.StatusCode.ToString()}";
                }
                
                var jsonResponse = JsonDocument.Parse(response.Content);
                var choices = jsonResponse.RootElement.GetProperty("choices");
                if (choices.GetArrayLength() > 0)
                {
                    var message = choices[0].GetProperty("message");
                    var content = message.GetProperty("content").GetString();
                    
                    return content;
                }
                
                return "No response content found.";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OpenRouterAIService.SendMessageAsync: {ex.Message}");
                return $"Error communicating with OpenRouter: {ex.Message}";
            }
        }

        /// <summary>
        /// Sends a message with streaming response
        /// </summary>
        public async Task<Stream> SendStreamedMessageAsync(string prompt, CancellationToken cancellationToken, Action<string> onMessageUpdate)
        {
            var responseStream = new MemoryStream();
            var writer = new StreamWriter(responseStream);
            
            try
            {
                string apiKey = await GetApiKeyAsync();
                if (string.IsNullOrEmpty(apiKey))
                {
                    await WriteErrorToStreamAsync(writer, "No API key found for OpenRouter");
                    return responseStream;
                }
                
                string streamBuffer = string.Empty;
                var handler = new HttpClientHandler();
                var client = new HttpClient(handler);
                
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                client.DefaultRequestHeaders.Add("HTTP-Referer", "https://nexuschat.app"); // Required by OpenRouter
                client.DefaultRequestHeaders.Add("X-Title", "NexusChat");
                
                var content = new
                {
                    model = _modelName,
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.7,
                    max_tokens = 1024,
                    stream = true // Enable streaming
                };
                
                var httpContent = new StringContent(
                    JsonSerializer.Serialize(content),
                    Encoding.UTF8,
                    "application/json");
                    
                var response = await client.PostAsync($"{_baseUrl}/chat/completions", httpContent, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    Debug.WriteLine($"OpenRouter API error: {response.StatusCode} - {errorContent}");
                    await WriteErrorToStreamAsync(writer, $"Error: {response.StatusCode}");
                    return responseStream;
                }
                
                using (var stream = await response.Content.ReadAsStreamAsync(cancellationToken))
                using (var reader = new StreamReader(stream))
                {
                    StringBuilder fullResponseBuilder = new StringBuilder();
                    string line;
                    
                    // Process each line of the streaming response
                    while ((line = await reader.ReadLineAsync()) != null && !cancellationToken.IsCancellationRequested)
                    {
                        if (string.IsNullOrEmpty(line) || line.StartsWith(":"))
                            continue;
                            
                        if (line.StartsWith("data: "))
                            line = line.Substring("data: ".Length);
                        
                        if (line == "[DONE]")
                            break;
                            
                        try
                        {
                            var jsonData = JsonDocument.Parse(line);
                            var choices = jsonData.RootElement.GetProperty("choices");
                            
                            
                            if (choices.GetArrayLength() > 0)
                            {
                                var delta = choices[0].GetProperty("delta");
                                if (delta.TryGetProperty("content", out var contentElement))
                                {
                                    string deltaContent = contentElement.GetString();
                                    if (!string.IsNullOrEmpty(deltaContent))
                                    {
                                        // Append to buffer
                                        streamBuffer += deltaContent;
                                        fullResponseBuilder.Append(deltaContent);

                                        // Call update callback
                                        onMessageUpdate?.Invoke(fullResponseBuilder.ToString());
                                    }
                                }
                            }
                        }
                        catch (JsonException jsonEx)
                        {
                            Debug.WriteLine($"JSON parsing error: {jsonEx.Message}");
                            // Skip malformed JSON but continue processing
                        }
                    }
                    
                    // Write full response
                    await writer.WriteAsync(fullResponseBuilder.ToString());
                    await writer.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OpenRouterAIService.SendStreamedMessageAsync: {ex.Message}");
                await WriteErrorToStreamAsync(writer, $"Error communicating with OpenRouter: {ex.Message}");
            }
            
            // Rewind the stream so it can be read from the beginning
            responseStream.Position = 0;
            return responseStream;
        }
        
        /// <summary>
        /// Writes error message to stream
        /// </summary>
        private async Task WriteErrorToStreamAsync(StreamWriter writer, string errorMessage)
        {
            await writer.WriteAsync(errorMessage);
            await writer.FlushAsync();
            
            // Rewind the memory stream
            writer.BaseStream.Position = 0;
        }
        
        /// <summary>
        /// Gets API key from the manager
        /// </summary>
        private async Task<string> GetApiKeyAsync()
        {
            try
            {
                return await _apiKeyManager.GetApiKeyAsync("OpenRouter");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting OpenRouter API key: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets the capabilities of this model
        /// </summary>
        public async Task<AIModel> GetCapabilitiesAsync()
        {
            return new AIModel
            {
                ModelName = ModelName,
                ProviderName = ProviderName,
                MaxTokens = 4096,
                MaxContextWindow = MaxContextWindow,
                SupportsStreaming = SupportsStreaming,
                SupportsVision = ModelName.Contains("claude-3") || ModelName.Contains("gpt-4o") || ModelName.Contains("gemini-1.5"),
                SupportsCodeCompletion = true
            };
        }

        /// <summary>
        /// Estimates token count for text
        /// </summary>
        public int EstimateTokens(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;
                
            // Simple approximation: ~4 characters per token
            return text.Length / 4 + 1;
        }
    }
}
