/* 
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using NexusChat.Core.Models;
using NexusChat.Services.Interfaces;

namespace NexusChat.Services.AIProviders.Implementations
{
    /// <summary>
    /// AI Service implementation for Azure AI using GitHub token
    /// </summary>
    public class AzureAIService : BaseAIService
    {
        private readonly string _selectedModel;
        private readonly Dictionary<string, AIModel> _models;
        private readonly Uri _endpoint = new Uri("https://models.inference.ai.azure.com");
        
        /// <summary>
        /// Gets the name of the model being used
        /// </summary>
        public override string ModelName => _selectedModel;
        
        /// <summary>
        /// Gets the provider name
        /// </summary>
        public override string ProviderName => "Azure";
        
        /// <summary>
        /// Gets whether streaming is supported by this model
        /// </summary>
        public override bool SupportsStreaming => 
            _models.TryGetValue(_selectedModel, out var model) 
                ? model.SupportsStreaming 
                : base.SupportsStreaming;
        
        /// <summary>
        /// Gets the maximum context window for this model
        /// </summary>
        public override int MaxContextWindow => 
            _models.TryGetValue(_selectedModel, out var model) 
                ? model.MaxContextWindow 
                : base.MaxContextWindow;

        /// <summary>
        /// Creates a new instance of the Azure AI service
        /// </summary>
        /// <param name="apiKeyManager">The API key manager</param>
        /// <param name="modelName">The model to use</param>
        public AzureAIService(IApiKeyManager apiKeyManager, string modelName = "DeepSeek-R1")
            : base(apiKeyManager)
        {
            _selectedModel = modelName ?? "DeepSeek-R1";
            
            // Initialize model registry with rich AIModel objects
            _models = new Dictionary<string, AIModel>(StringComparer.OrdinalIgnoreCase)
            {
                ["DeepSeek-R1"] = new AIModel
                { 
                    ModelName = "DeepSeek-R1",
                    ProviderName = "Azure",
                    Description = "DeepSeek R1 - Advanced reasoning and language understanding",
                    MaxTokens = 4096, 
                    MaxContextWindow = 16384, 
                    SupportsStreaming = true,
                    DefaultTemperature = 0.7f,
                    SupportsCodeCompletion = true,
                    IsAvailable = true
                },
                ["Phi-3.5-mini-instruct"] = new AIModel
                {
                    ModelName = "Phi-3.5-mini-instruct",
                    ProviderName = "Azure",
                    Description = "Phi 3.5 Mini - Instruction-tuned small model with large context",
                    MaxTokens = 4096,
                    MaxContextWindow = 131072, 
                    SupportsStreaming = true,
                    DefaultTemperature = 0.7f,
                    IsAvailable = true
                },
                ["Phi-3.5-vision-instruct"] = new AIModel
                { 
                    ModelName = "Phi-3.5-vision-instruct",
                    ProviderName = "Azure",
                    Description = "Phi 3.5 Vision - Multimodal model with vision capabilities",
                    MaxTokens = 4096, 
                    MaxContextWindow = 131072, 
                    SupportsStreaming = false,
                    DefaultTemperature = 0.7f,
                    SupportsVision = true,
                    IsAvailable = true
                },
                ["Phi-3-medium-instruct-128k"] = new AIModel
                { 
                    ModelName = "Phi-3-medium-instruct-128k",
                    ProviderName = "Azure",
                    Description = "Phi 3 Medium - Balanced model with large context window",
                    MaxTokens = 4096, 
                    MaxContextWindow = 131072, 
                    SupportsStreaming = true,
                    DefaultTemperature = 0.7f,
                    IsAvailable = true
                }
            };
        }

        /// <summary>
        /// Gets a list of all available models for this provider
        /// </summary>
        public IEnumerable<AIModel> GetAvailableModels()
        {
            return _models.Values.Where(m => m.IsAvailable);
        }

        /// <summary>
        /// Gets model capabilities for a specific model
        /// </summary>
        /// <param name="modelName">Name of the model</param>
        /// <returns>Model capabilities or null if not found</returns>
        public ModelCapabilities GetModelCapabilities(string modelName)
        {
            if (_models.TryGetValue(modelName, out var model))
            {
                return new ModelCapabilities
                {
                    MaxTokens = model.MaxTokens,
                    MaxContextWindow = model.MaxContextWindow,
                    DefaultTemperature = model.DefaultTemperature,
                    SupportsStreaming = model.SupportsStreaming,
                    SupportsVision = model.SupportsVision,
                    SupportsCodeCompletion = model.SupportsCodeCompletion
                };
            }
            
            return null;
        }

        /// <summary>
        /// Gets AIModel information for a specific model
        /// </summary>
        /// <param name="modelName">Name of the model</param>
        /// <returns>AIModel or null if not found</returns>
        public AIModel GetModel(string modelName)
        {
            if (_models.TryGetValue(modelName, out var model))
            {
                // Return a clone to prevent modification of the internal dictionary
                return new AIModel
                {
                    ModelName = model.ModelName,
                    ProviderName = model.ProviderName,
                    Description = model.Description,
                    MaxTokens = model.MaxTokens,
                    MaxContextWindow = model.MaxContextWindow,
                    SupportsStreaming = model.SupportsStreaming,
                    DefaultTemperature = model.DefaultTemperature,
                    SupportsCodeCompletion = model.SupportsCodeCompletion,
                    SupportsVision = model.SupportsVision,
                    IsAvailable = model.IsAvailable
                };
            }
            
            return null;
        }

        /// <summary>
        /// Validates if a model is supported by this provider
        /// </summary>
        /// <param name="modelName">Name of the model to check</param>
        /// <returns>True if the model is supported</returns>
        public bool SupportsModel(string modelName)
        {
            return _models.ContainsKey(modelName);
        }

        /// <summary>
        /// Gets a fallback model if the requested one is not available
        /// </summary>
        /// <returns>Name of a fallback model</returns>
        public string GetFallbackModel()
        {
            // Try first available model in order of preference
            string[] preferredModels = new[]
            {
                "Phi-3.5-mini-instruct",  // Good balance of capabilities and speed
                "DeepSeek-R1",            // Solid alternative
                "Phi-3-medium-instruct-128k"  // Another option
            };
            
            foreach (var modelName in preferredModels)
            {
                if (_models.TryGetValue(modelName, out var model) && model.IsAvailable)
                {
                    return modelName;
                }
            }
            
            // If none of the preferred models are available, return first available
            var firstAvailable = _models.Values.FirstOrDefault(m => m.IsAvailable);
            return firstAvailable?.ModelName ?? "DeepSeek-R1"; // Default fallback
        }

        /// <summary>
        /// Sends a message to the Azure AI service
        /// </summary>
        public override async Task<string> SendMessageAsync(string prompt, CancellationToken cancellationToken)
        {
            return await HandleApiRequestAsync(async () =>
            {
                // Get token from environment variables or API key manager
                string token = GetApiToken();
                var credential = new AzureKeyCredential(token);
                
                // Create Azure AI client
                var client = new ChatCompletionsClient(
                    _endpoint,
                    credential,
                    new AzureAIInferenceClientOptions());
                
                // Get model parameters (use defaults if not found)
                float temperature = 0.7f;
                int maxTokens = 4096;
                
                if (_models.TryGetValue(_selectedModel, out var model))
                {
                    temperature = model.DefaultTemperature;
                    maxTokens = Math.Min(model.MaxTokens, 4096);
                }
                
                // Configure the request
                var requestOptions = new ChatCompletionsOptions()
                {
                    Messages =
                    {
                        new ChatRequestUserMessage(prompt)
                    },
                    MaxTokens = maxTokens,
                    Model = _selectedModel,
                    Temperature = temperature
                };
                
                // Send the request
                Response<ChatCompletions> response = await client.CompleteAsync(requestOptions, cancellationToken);
                
                // Extract the response content
                return response.Value.Content;
                
            }, "Error calling Azure AI API");
        }
        
        /// <summary>
        /// Sends a message to the Azure AI with streaming response
        /// </summary>
        public override async Task<Stream> SendStreamedMessageAsync(string prompt, CancellationToken cancellationToken, Action<string> onMessageUpdate)
        {
            return await HandleApiRequestAsync(async () =>
            {
                // Use a memory stream to collect the entire response
                var responseStream = new MemoryStream();
                
                // Get token from environment variables or API key manager
                string token = GetApiToken();
                var credential = new AzureKeyCredential(token);
                
                // Create Azure AI client
                var client = new ChatCompletionsClient(
                    _endpoint,
                    credential,
                    new AzureAIInferenceClientOptions());
                
                // Get model parameters (use defaults if not found)
                float temperature = 0.7f;
                int maxTokens = 4096;
                
                if (_models.TryGetValue(_selectedModel, out var model))
                {
                    temperature = model.DefaultTemperature;
                    maxTokens = Math.Min(model.MaxTokens, 4096);
                }
                
                // Configure the request
                var requestOptions = new ChatCompletionsOptions()
                {
                    Messages =
                    {
                        new ChatRequestUserMessage(prompt)
                    },
                    MaxTokens = maxTokens,
                    Model = _selectedModel,
                    Temperature = temperature,
                    AdditionalProperties = {
                        { "stream_options", BinaryData.FromObjectAsJson(new { include_usage=true })}
                    }
                };
                
                // Process stream in background
                _ = ProcessStreamingResponseAsync(client, requestOptions, onMessageUpdate, cancellationToken);
                
                // Return a placeholder stream (the actual content is delivered via onMessageUpdate)
                return responseStream;
                
            }, "Error calling Azure AI streaming API");
        }
        
        /// <summary>
        /// Processes streaming response from Azure AI
        /// </summary>
        private async Task ProcessStreamingResponseAsync(
            ChatCompletionsClient client, 
            ChatCompletionsOptions options,
            Action<string> onMessageUpdate,
            CancellationToken cancellationToken)
        {
            var fullResponse = new StringBuilder();
            
            try
            {
                StreamingResponse<StreamingChatCompletionsUpdate> response = 
                    await client.CompleteStreamingAsync(options, cancellationToken);
                
                await foreach (StreamingChatCompletionsUpdate update in response.WithCancellation(cancellationToken))
                {
                    if (!string.IsNullOrEmpty(update.ContentUpdate))
                    {
                        fullResponse.Append(update.ContentUpdate);
                        onMessageUpdate?.Invoke(fullResponse.ToString());
                    }
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                Debug.WriteLine($"Error processing streaming response: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Gets the API token for Azure AI
        /// </summary>
        private string GetApiToken()
        {
            // Try to get the GitHub token which is used for Azure AI authentication
            string apiToken = ApiKeyManager.GetApiKey("GITHUB_TOKEN");
            
            // Try alternative API keys if not found
            if (string.IsNullOrEmpty(apiToken))
            {
                // Use our new model-specific async method but run it synchronously
                Task<string> task = ApiKeyManager.GetProviderApiKeyAsync("AZURE");
                task.Wait();
                apiToken = task.Result;
            }
            
            if (string.IsNullOrEmpty(apiToken))
            {
                throw new InvalidOperationException("No Azure AI authentication token found. Please check API key configuration.");
            }
            
            return apiToken;
        }
        
        /// <summary>
        /// Estimates tokens in input text
        /// </summary>
        public override int EstimateTokens(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;
                
            // Different models have different tokenization ratios
            if (_selectedModel.Contains("phi", StringComparison.OrdinalIgnoreCase))
            {
                // Phi models tend to have different tokenization
                return (int)(text.Length / 3.5f) + 1;
            }
            
            // Default estimation
            return (int)(text.Length / 4f) + 1;
        }
    }
}
*/