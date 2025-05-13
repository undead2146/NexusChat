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
    /// Implementation of IAIProviderService for Groq
    /// </summary>
    public class GroqAIService : IAIProviderService
    {
        private readonly IApiKeyManager _apiKeyManager;
        private readonly string _modelName;
        private readonly string _baseUrl = "https://api.groq.com/openai/v1";
        private RestClient _client;

        /// <summary>
        /// Gets the name of the model
        /// </summary>
        public string ModelName => _modelName;

        /// <summary>
        /// Gets the name of the provider
        /// </summary>
        public string ProviderName => "Groq";

        /// <summary>
        /// Gets whether streaming is supported
        /// </summary>
        public bool SupportsStreaming => true;

        /// <summary>
        /// Gets the maximum context window size
        /// </summary>
        public int MaxContextWindow => GetContextWindowSize(_modelName);

        /// <summary>
        /// Creates a new Groq AI service
        /// </summary>
        public GroqAIService(IApiKeyManager apiKeyManager, string modelName = "llama3-70b-8192")
        {
            _apiKeyManager = apiKeyManager ?? throw new ArgumentNullException(nameof(apiKeyManager));
            _modelName = !string.IsNullOrEmpty(modelName) ? modelName : "llama3-70b-8192";
            _client = new RestClient(_baseUrl);
            Debug.WriteLine($"GroqAIService created with model: {_modelName}");
        }

        /// <summary>
        /// Gets context window size based on model name
        /// </summary>
        private int GetContextWindowSize(string modelName)
        {
            if (string.IsNullOrEmpty(modelName))
                return 8192;

            // Different context sizes based on model
            if (modelName.Contains("mixtral") && modelName.Contains("32768"))
                return 32768;

            return 8192; // Default for most models
        }

        /// <summary>
        /// Gets all available models for Groq - Implemented as static for factory usage
        /// </summary>
        public static IEnumerable<AIModel> GetAvailableModels()
        {
            Debug.WriteLine("GroqAIService.GetAvailableModels [static] called");

            return new List<AIModel>
            {
                // Llama 3 models
                new AIModel {
                    ModelName = "llama3-70b-8192",
                    ProviderName = "Groq",
                    Description = "Meta's Llama 3 70B model - optimized for performance",
                    MaxTokens = 4096,
                    MaxContextWindow = 8192,
                    SupportsStreaming = true,
                    IsAvailable = true,
                    ApiKeyVariable = "API_KEY_GROQ"
                },
                new AIModel {
                    ModelName = "llama3-8b-8192",
                    ProviderName = "Groq",
                    Description = "Meta's Llama 3 8B model - balanced speed and quality",
                    MaxTokens = 4096,
                    MaxContextWindow = 8192,
                    SupportsStreaming = true,
                    IsAvailable = true,
                    ApiKeyVariable = "API_KEY_GROQ"
                },
                new AIModel {
                    ModelName = "llama-3-70b-chat",
                    ProviderName = "Groq",
                    Description = "Llama 3 70B - top performance conversational model",
                    MaxTokens = 4096,
                    MaxContextWindow = 8192,
                    SupportsStreaming = true,
                    IsAvailable = true,
                    ApiKeyVariable = "API_KEY_GROQ"
                },
                new AIModel {
                    ModelName = "llama-3-8b-chat",
                    ProviderName = "Groq",
                    Description = "Llama 3 8B - efficient conversational model",
                    MaxTokens = 4096,
                    MaxContextWindow = 8192,
                    SupportsStreaming = true,
                    IsAvailable = true,
                    ApiKeyVariable = "API_KEY_GROQ"
                },

                // Mixtral models
                new AIModel {
                    ModelName = "mixtral-8x7b-32768",
                    ProviderName = "Groq",
                    Description = "Mixtral 8x7B - high performance mixture of experts model",
                    MaxTokens = 8192,
                    MaxContextWindow = 32768,
                    SupportsStreaming = true,
                    IsAvailable = true,
                    ApiKeyVariable = "API_KEY_GROQ"
                },

                // Gemma models
                new AIModel {
                    ModelName = "gemma-7b-it",
                    ProviderName = "Groq",
                    Description = "Google's Gemma 7B instruction-tuned model",
                    MaxTokens = 4096,
                    MaxContextWindow = 8192,
                    SupportsStreaming = true,
                    IsAvailable = true,
                    ApiKeyVariable = "API_KEY_GROQ"
                },
                new AIModel {
                    ModelName = "gemma-2-27b-it",
                    ProviderName = "Groq",
                    Description = "Google's Gemma 2 27B instruction-tuned model",
                    MaxTokens = 4096,
                    MaxContextWindow = 8192,
                    SupportsStreaming = true,
                    IsAvailable = true,
                    ApiKeyVariable = "API_KEY_GROQ"
                },
                new AIModel {
                    ModelName = "gemma-2-9b-it",
                    ProviderName = "Groq",
                    Description = "Google's Gemma 2 9B instruction-tuned model",
                    MaxTokens = 4096,
                    MaxContextWindow = 8192,
                    SupportsStreaming = true,
                    IsAvailable = true,
                    ApiKeyVariable = "API_KEY_GROQ"
                },

                // Preview models
                new AIModel {
                    ModelName = "llama-3-3-70b-specdec",
                    ProviderName = "Groq",
                    Description = "Llama 3.3 (70B) SpecDec - Specialized decoder version with enhanced context",
                    MaxTokens = 4096,
                    MaxContextWindow = 32768,
                    SupportsStreaming = true,
                    DefaultTemperature = 0.7f,
                    IsAvailable = true,
                    ApiKeyVariable = "API_KEY_GROQ"
                },
                new AIModel {
                    ModelName = "qwen-2-5-32b",
                    ProviderName = "Groq",
                    Description = "Qwen 2.5 (32B) - Alibaba's advanced language model",
                    MaxTokens = 4096,
                    MaxContextWindow = 32768,
                    SupportsStreaming = true,
                    DefaultTemperature = 0.7f,
                    IsAvailable = true,
                    ApiKeyVariable = "API_KEY_GROQ"
                },
                new AIModel {
                    ModelName = "qwen-2-5-coder-32b",
                    ProviderName = "Groq",
                    Description = "Qwen 2.5 Coder (32B) - Code-specialized version of Qwen",
                    MaxTokens = 4096,
                    MaxContextWindow = 32768,
                    SupportsStreaming = true,
                    DefaultTemperature = 0.7f,
                    SupportsCodeCompletion = true,
                    IsAvailable = true,
                    ApiKeyVariable = "API_KEY_GROQ"
                },
                new AIModel {
                    ModelName = "deepseek-r1-distill-llama-70b",
                    ProviderName = "Groq",
                    Description = "DeepSeek R1 Distilled Llama (70B) - Knowledge-distilled efficient model",
                    MaxTokens = 4096,
                    MaxContextWindow = 16384,
                    SupportsStreaming = true,
                    DefaultTemperature = 0.7f,
                    IsAvailable = true,
                    ApiKeyVariable = "API_KEY_GROQ"
                }
            };
        }

        /// <summary>
        /// Checks if Groq supports a specific model
        /// </summary>
        public static bool SupportsModel(string modelName)
        {
            if (string.IsNullOrEmpty(modelName))
                return false;
                
            // Normalize the input model name
            string normalizedName = modelName.ToLowerInvariant();
            
            // Get all available models
            var availableModels = GetAvailableModels();
            
            // Check for direct match with any available model
            if (availableModels.Any(m => m.ModelName.Equals(normalizedName, StringComparison.OrdinalIgnoreCase)))
                return true;
            
            // For pattern matching (partial matches like "llama3" for any llama3 model)
            // This helps with flexibility when exact model versions might differ
            string[] modelPatterns = new[] { "llama", "mixtral", "gemma" };
            
            // Check if the model name contains any of our supported model families
            foreach (var pattern in modelPatterns)
            {
                if (normalizedName.Contains(pattern))
                    return true;
            }
            
            return false;
        }

        /// <summary>
        /// Sends a message to Groq API
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
                    Debug.WriteLine($"Groq API error: {response.StatusCode} - {response.Content}");
                    return $"Error communicating with Groq: {response.ErrorMessage ?? response.StatusCode.ToString()}";
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
                Debug.WriteLine($"Error in GroqAIService.SendMessageAsync: {ex.Message}");
                return $"Error communicating with Groq: {ex.Message}";
            }
        }

        /// <summary>
        /// Sends a streamed message to Groq
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
                    await WriteErrorToStreamAsync(writer, "No API key found for Groq");
                    return responseStream;
                }

                string streamBuffer = string.Empty;
                var handler = new HttpClientHandler();
                var client = new HttpClient(handler);

                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

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
                    Debug.WriteLine($"Groq API error: {response.StatusCode} - {errorContent}");
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
                Debug.WriteLine($"Error in GroqAIService.SendStreamedMessageAsync: {ex.Message}");
                await WriteErrorToStreamAsync(writer, $"Error communicating with Groq: {ex.Message}");
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
                return await _apiKeyManager.GetApiKeyAsync("Groq");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting Groq API key: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets model capabilities
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
                SupportsVision = false,
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
