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
    /// OpenRouter AI provider service implementation
    /// </summary>
    public class OpenRouterAIService : BaseAIService
    {
        private readonly string _baseUrl = "https://openrouter.ai/api/v1";
        
        /// <summary>
        /// Gets the name of the model
        /// </summary>
        public override string ModelName { get; }
        
        /// <summary>
        /// Gets the name of the provider
        /// </summary>
        public override string ProviderName => "OpenRouter";
        
        /// <summary>
        /// Creates a new OpenRouter AI service with the specified model name
        /// </summary>
        public OpenRouterAIService(IApiKeyManager apiKeyManager, string modelName)
            : base(apiKeyManager, CreateOpenRouterModel(modelName))
        {
            if (string.IsNullOrEmpty(modelName))
            {
                throw new ArgumentException("Model name must be provided for OpenRouterAIService.", nameof(modelName));
            }
            ModelName = modelName;
            Debug.WriteLine($"OpenRouterAIService created with model: {ModelName}");
        }

        private static AIModel CreateOpenRouterModel(string modelName)
        {
            if (string.IsNullOrEmpty(modelName))
            {
                throw new ArgumentException("Model name must be provided for creating OpenRouter AIModel.", nameof(modelName));
            }
            // Simplified, as detailed model creation is now in DiscoverModelsAsync
            return new AIModel
            {
                ModelName = modelName,
                ProviderName = "OpenRouter",
                MaxContextWindow = GetContextWindowSize(modelName), // Keep for instance specific details
                SupportsStreaming = true, // Keep for instance specific details
                ApiKeyVariable = "API_KEY_OPENROUTER"
            };
        }

        /// <summary>
        /// Discovers models available from OpenRouter.
        /// </summary>
        /// <param name="apiKeyManager">The API key manager to check for key availability.</param>
        /// <returns>A list of AIModels from OpenRouter.</returns>
        public static async Task<List<AIModel>> DiscoverModelsAsync(IApiKeyManager apiKeyManager)
        {
            var models = new List<AIModel>();
            if (apiKeyManager == null)
            {
                Debug.WriteLine("OpenRouterAIService.DiscoverModelsAsync: ApiKeyManager is null.");
                return models;
            }

            try
            {
                Debug.WriteLine("OpenRouterAIService: Discovering OpenRouter models.");
                bool hasApiKey = await apiKeyManager.HasActiveApiKeyAsync("OpenRouter");

                if (!hasApiKey)
                {
                    Debug.WriteLine("OpenRouterAIService: No active OpenRouter API key found. Not listing OpenRouter models.");
                    return models;
                }

                // Add comprehensive model collection
                models.Add(CreateModelInternal("anthropic/claude-3-opus", "Claude 3 Opus", "Claude 3 Opus - Anthropic's most powerful model", 200000, 4096, true, true));
                models.Add(CreateModelInternal("anthropic/claude-3-sonnet", "Claude 3 Sonnet", "Claude 3 Sonnet - balanced performance & quality", 200000, 4096, true, true));
                models.Add(CreateModelInternal("anthropic/claude-3-haiku", "Claude 3 Haiku", "Claude 3 Haiku - fastest & most compact Claude model", 200000, 4096, true, true));
                models.Add(CreateModelInternal("google/gemini-pro", "Gemini Pro", "Google's Gemini Pro model", 32768, 8192, true, false));
                models.Add(CreateModelInternal("google/gemini-1.5-flash", "Gemini 1.5 Flash", "Gemini 1.5 Flash - Fast response model", 128000, 8192, true, true));
                models.Add(CreateModelInternal("mistralai/mistral-small", "Mistral Small", "Mistral Small - efficient and powerful", 32768, 4096, true, false));
                models.Add(CreateModelInternal("mistralai/mistral-medium", "Mistral Medium", "Mistral Medium - versatile model", 32768, 8192, true, false));
                models.Add(CreateModelInternal("mistralai/mistral-large", "Mistral Large", "Mistral Large - advanced reasoning", 32768, 8192, true, false));
                models.Add(CreateModelInternal("meta-llama/llama-3-70b-instruct", "Llama 3 70B Instruct", "Llama 3 70B - Meta's flagship model", 8192, 8192, true, false));
                models.Add(CreateModelInternal("meta-llama/llama-3-8b-instruct", "Llama 3 8B Instruct", "Llama 3 8B - Compact but capable", 8192, 8192, true, false));
                models.Add(CreateModelInternal("anthropic/claude-2", "Claude 2", "Claude 2 - Anthropic's previous generation model", 100000, 4096, true, false));
                models.Add(CreateModelInternal("openai/gpt-4-turbo", "GPT-4 Turbo", "GPT-4 Turbo - Latest OpenAI model", 128000, 4096, true, false));
                models.Add(CreateModelInternal("openai/gpt-3.5-turbo", "GPT-3.5 Turbo", "GPT-3.5 Turbo - Fast and economical", 16385, 4096, true, false));


                // Model definitions previously in AIModelDiscoveryService.CreateOpenRouterModelsAsync
                // and OpenRouterAIService.GetAvailableModels() 
                models.Add(CreateModelInternal("anthropic/claude-3.5-sonnet", "Claude 3.5 Sonnet (via OpenRouter)", "Anthropic's most advanced model", 200000, 4096, true, true));
                models.Add(CreateModelInternal("openai/gpt-4o", "GPT-4o (via OpenRouter)", "OpenAI's latest multimodal model", 128000, 4096, true, true));
                models.Add(CreateModelInternal("google/gemini-1.5-pro", "Gemini 1.5 Pro (via OpenRouter)", "Google's advanced model with large context", 1048576, 8192, true, true)); // Max context for Gemini 1.5 Pro is 1M or 2M
                models.Add(CreateModelInternal("meta-llama/llama-3.1-405b-instruct", "Llama 3.1 405B Instruct (via OpenRouter)", "Meta's largest instruction-tuned model", 131072, 4096, true, false));
                models.Add(CreateModelInternal("mistralai/mistral-large-latest", "Mistral Large (via OpenRouter)", "Mistral's flagship model", 32768, 4096, true, false));
                models.Add(CreateModelInternal("microsoft/phi-3-medium-128k-instruct", "Phi-3 Medium 128k Instruct (via OpenRouter)", "Microsoft's efficient model with large context", 131072, 4096, true, false));


                Debug.WriteLine($"OpenRouterAIService: Discovered {models.Count} OpenRouter models.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error discovering OpenRouter models: {ex.Message}");
            }
            return models;
        }

        private static AIModel CreateModelInternal(string modelName, string displayName, string description, int contextWindow, int maxOutputTokens, bool supportsStreaming, bool supportsVision)
        {
            return new AIModel
            {
                ProviderName = "OpenRouter", // This is key
                ModelName = modelName, // Full model identifier for OpenRouter
                DisplayName = displayName,
                Description = description,
                IsAvailable = true,
                MaxTokens = maxOutputTokens,
                MaxContextWindow = contextWindow,
                SupportsStreaming = supportsStreaming,
                SupportsVision = supportsVision,
                SupportsCodeCompletion = true, // General assumption for most OpenRouter models
                DefaultTemperature = 0.7f,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Status = ModelStatus.Available,
                ApiKeyVariable = "API_KEY_OPENROUTER"
            };
        }
        
        /// <summary>
        /// Gets context window size based on model name
        /// </summary>
        private static int GetContextWindowSize(string modelName)
        {
            if (string.IsNullOrEmpty(modelName))
                return 16384;
                
            string name = modelName.ToLowerInvariant();
            
            // Anthropic Claude models
            if (name.Contains("claude-3-opus") || name.Contains("claude-3-sonnet") || name.Contains("claude-3-haiku"))
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
                
            return 16384;
        }
        
        /// <summary>
        /// Sends a message to the OpenRouter API
        /// </summary>
        public override async Task<string> SendMessageAsync(string prompt, CancellationToken cancellationToken)
        {
            return await HandleApiRequestAsync(async () =>
            {
                string apiKey = await ApiKeyManager.GetApiKeyAsync("OpenRouter");
                if (string.IsNullOrEmpty(apiKey))
                {
                    throw new AIServiceException("No API key found for OpenRouter");
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

                // Configure headers for OpenRouter
                HttpClient.DefaultRequestHeaders.Clear();
                HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                HttpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://nexuschat.app");
                HttpClient.DefaultRequestHeaders.Add("X-Title", "NexusChat");
                
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
            }, "OpenRouter API request failed");
        }

        /// <summary>
        /// Sends a message with streaming response
        /// </summary>
        public override async Task<Stream> SendStreamedMessageAsync(string prompt, CancellationToken cancellationToken, Action<string> onMessageUpdate)
        {
            var responseStream = new MemoryStream();
            var writer = new StreamWriter(responseStream);
            
            try
            {
                string apiKey = await ApiKeyManager.GetApiKeyAsync("OpenRouter");
                if (string.IsNullOrEmpty(apiKey))
                {
                    await WriteErrorToStreamAsync(writer, "No API key found for OpenRouter");
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

                // Configure headers for OpenRouter
                HttpClient.DefaultRequestHeaders.Clear();
                HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                HttpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://nexuschat.app");
                HttpClient.DefaultRequestHeaders.Add("X-Title", "NexusChat");
                    
                var response = await HttpClient.PostAsync($"{_baseUrl}/chat/completions", httpContent, cancellationToken);
                
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
                Debug.WriteLine($"Error in OpenRouterAIService.SendStreamedMessageAsync: {ex.Message}");
                await WriteErrorToStreamAsync(writer, $"Error communicating with OpenRouter: {ex.Message}");
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
