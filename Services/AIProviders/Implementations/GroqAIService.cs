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
using NexusChat.Services.AIProviders.Implementations;

namespace NexusChat.Services.AIProviders.Implementations
{
    /// <summary>
    /// Implementation of IAIProviderService for Groq
    /// </summary>
    public class GroqAIService : BaseAIService
    {
        private readonly string _baseUrl = "https://api.groq.com/openai/v1";

        /// <summary>
        /// Gets the name of the model
        /// </summary>
        public override string ModelName { get; }

        /// <summary>
        /// Gets the name of the provider
        /// </summary>
        public override string ProviderName => "Groq";

        /// <summary>
        /// Creates a new Groq AI service
        /// </summary>
        public GroqAIService(IApiKeyManager apiKeyManager, string modelName) 
            : base(apiKeyManager, CreateGroqModel(modelName))
        {
            if (string.IsNullOrEmpty(modelName))
            {
                throw new ArgumentException("Model name must be provided for GroqAIService.", nameof(modelName));
            }
            ModelName = modelName;
            Debug.WriteLine($"GroqAIService created with model: {ModelName}");
        }

        private static AIModel CreateGroqModel(string modelName)
        {
            if (string.IsNullOrEmpty(modelName))
            {
                // This exception would be caught by the factory if modelName was initially empty.
                // This ensures direct instantiation also requires a modelName for the AIModel.
                throw new ArgumentException("Model name must be provided for creating Groq AIModel.", nameof(modelName));
            }
            // Simplified, as detailed model creation is now in DiscoverModelsAsync
            // The instance model primarily needs its name and provider.
            // Other details can be fetched from a central model manager if needed post-discovery.
            return new AIModel
            {
                ModelName = modelName,
                ProviderName = "Groq",
                MaxContextWindow = GetContextWindowSize(modelName), // Keep for instance specific details
                SupportsStreaming = true, // Keep for instance specific details
                ApiKeyVariable = "API_KEY_GROQ"
            };
        }

        /// <summary>
        /// Discovers models available from Groq.
        /// </summary>
        /// <param name="apiKeyManager">The API key manager to check for key availability.</param>
        /// <returns>A list of AIModels from Groq.</returns>
        public static async Task<List<AIModel>> DiscoverModelsAsync(IApiKeyManager apiKeyManager)
        {
            var models = new List<AIModel>();
            if (apiKeyManager == null)
            {
                Debug.WriteLine("GroqAIService.DiscoverModelsAsync: ApiKeyManager is null.");
                return models;
            }

            try
            {
                Debug.WriteLine("GroqAIService: Discovering Groq models.");
                bool hasApiKey = await apiKeyManager.HasActiveApiKeyAsync("Groq");

                if (!hasApiKey)
                {
                    Debug.WriteLine("GroqAIService: No active Groq API key found. Not listing Groq models.");
                    return models;
                }

                // Model definitions previously in AIModelDiscoveryService.CreateGroqModelsAsync
                // and GroqAIService.GetAvailableModels()
                models.Add(CreateModelInternal("llama-3.1-8b-instant", "Llama 3.1 8B Instant", "Fast and efficient model for quick responses", 131072, 131072)); 
                models.Add(CreateModelInternal("gemma2-9b-it", "Gemma 2 9B IT", "Google's Gemma model optimized for instruction following", 8192, 8192));
                models.Add(CreateModelInternal("llama3-70b-8192", "Llama 3 70B (Legacy)", "Powerful model with large context window", 8192, 8192)); // Kept for compatibility
                models.Add(CreateModelInternal("mixtral-8x7b-32768", "Mixtral 8x7B", "High performance mixture of experts model", 32768, 32768));
                models.Add(CreateModelInternal("gemma-7b-it", "Gemma 7B IT", "Google's Gemma 7B instruction-tuned model", 8192, 8192));
                
                // Gemma 2 models
                models.Add(CreateModelInternal("gemma-2-27b-it", "Gemma 2 27B IT", "Google's Gemma 2 27B instruction-tuned model", 4096, 8192));
                
                // Preview models
                models.Add(CreateModelInternal("deepseek-r1-distill-llama-70b", "DeepSeek R1 Distilled Llama 70B", "DeepSeek R1 Distilled Llama (70B) - Knowledge-distilled efficient model", 4096, 16384));

                Debug.WriteLine($"GroqAIService: Discovered {models.Count} Groq models.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error discovering Groq models: {ex.Message}");
            }
            return models;
        }

        private static AIModel CreateModelInternal(string modelName, string displayName, string description, int maxTokens, int contextWindow)
        {
            return new AIModel
            {
                ProviderName = "Groq",
                ModelName = modelName,
                DisplayName = displayName,
                Description = description,
                IsAvailable = true,
                MaxTokens = maxTokens,
                MaxContextWindow = contextWindow,
                SupportsStreaming = true,
                SupportsVision = false, // Groq models generally don't support vision directly
                SupportsCodeCompletion = true,
                DefaultTemperature = 0.7f,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Status = ModelStatus.Available,
                ApiKeyVariable = "API_KEY_GROQ"
            };
        }

        /// <summary>
        /// Gets context window size based on model name
        /// </summary>
        private static int GetContextWindowSize(string modelName)
        {
            if (string.IsNullOrEmpty(modelName))
                return 8192;

            // Different context sizes based on model
            if (modelName.Contains("mixtral") && modelName.Contains("32768"))
                return 32768;

            return 8192; // Default for most models
        }

        /// <summary>
        /// Sends a message to Groq API
        /// </summary>
        public override async Task<string> SendMessageAsync(string prompt, CancellationToken cancellationToken)
        {
            return await HandleApiRequestAsync(async () =>
            {
                string apiKey = await ApiKeyManager.GetApiKeyAsync("Groq");
                if (string.IsNullOrEmpty(apiKey))
                {
                    throw new AIServiceException("No API key found for Groq");
                }

                var requestBody = new
                {
                    model = ModelName,
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.7,
                    max_tokens = 1024
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json");

                HttpClient.DefaultRequestHeaders.Clear();
                HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var response = await HttpClient.PostAsync($"{_baseUrl}/chat/completions", content, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var jsonResponse = JsonDocument.Parse(responseContent);
                var choices = jsonResponse.RootElement.GetProperty("choices");
                
                if (choices.GetArrayLength() > 0)
                {
                    var message = choices[0].GetProperty("message");
                    return message.GetProperty("content").GetString() ?? "No response content found.";
                }

                return "No response content found.";
            }, "Groq API request failed");
        }

        /// <summary>
        /// Sends a streamed message to Groq
        /// </summary>
        public override async Task<Stream> SendStreamedMessageAsync(string prompt, CancellationToken cancellationToken, Action<string> onMessageUpdate)
        {
            var responseStream = new MemoryStream();
            var writer = new StreamWriter(responseStream);

            try
            {
                string apiKey = await ApiKeyManager.GetApiKeyAsync("Groq");
                if (string.IsNullOrEmpty(apiKey))
                {
                    await WriteErrorToStreamAsync(writer, "No API key found for Groq");
                    return responseStream;
                }

                var content = new
                {
                    model = ModelName,
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.7,
                    max_tokens = 1024,
                    stream = true
                };

                var httpContent = new StringContent(
                    JsonSerializer.Serialize(content),
                    Encoding.UTF8,
                    "application/json");

                // Use the base HttpClient with proper headers
                HttpClient.DefaultRequestHeaders.Clear();
                HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var response = await HttpClient.PostAsync($"{_baseUrl}/chat/completions", httpContent, cancellationToken);

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
                                        fullResponseBuilder.Append(deltaContent);
                                        onMessageUpdate?.Invoke(fullResponseBuilder.ToString());
                                    }
                                }
                            }
                        }
                        catch (JsonException jsonEx)
                        {
                            Debug.WriteLine($"JSON parsing error: {jsonEx.Message}");
                        }
                    }

                    await writer.WriteAsync(fullResponseBuilder.ToString());
                    await writer.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GroqAIService.SendStreamedMessageAsync: {ex.Message}");
                await WriteErrorToStreamAsync(writer, $"Error communicating with Groq: {ex.Message}");
            }

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
            writer.BaseStream.Position = 0;
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
