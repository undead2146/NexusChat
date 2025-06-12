using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Core.Models;
using NexusChat.Services.Interfaces;

namespace NexusChat.Services.AIProviders.Implementations
{
    /// <summary>
    /// Base class for all AI service implementations
    /// </summary>
    public abstract class BaseAIService : IAIProviderService
    {
        protected readonly IApiKeyManager ApiKeyManager;
        protected readonly HttpClient HttpClient;
        protected readonly AIModel Model;
        
        /// <summary>
        /// Gets the name of the model
        /// </summary>
        public abstract string ModelName { get; }
        
        /// <summary>
        /// Gets the name of the provider
        /// </summary>
        public abstract string ProviderName { get; }
        
        /// <summary>
        /// Gets whether streaming is supported
        /// </summary>
        public virtual bool SupportsStreaming => Model?.SupportsStreaming ?? true;
        
        /// <summary>
        /// Gets the maximum context window size
        /// </summary>
        public virtual int MaxContextWindow => Model?.MaxContextWindow ?? 8192;

        /// <summary>
        /// Creates a new instance of the base AI service
        /// </summary>
        /// <param name="apiKeyManager">The API key manager</param>
        protected BaseAIService(IApiKeyManager apiKeyManager, AIModel model = null)
        {
            ApiKeyManager = apiKeyManager ?? throw new ArgumentNullException(nameof(apiKeyManager));
            HttpClient = new HttpClient();
            Model = model ?? new AIModel();
            
            // Configure common HTTP client settings
            ConfigureHttpClient();
        }

        /// <summary>
        /// Configures the HTTP client with common settings
        /// </summary>
        protected virtual void ConfigureHttpClient()
        {
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            HttpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("NexusChat", "0.5.0"));
            HttpClient.Timeout = TimeSpan.FromSeconds(120); // 2 minute timeout for AI requests
        }

        /// <summary>
        /// Sends a message to the AI service
        /// </summary>
        public abstract Task<string> SendMessageAsync(string prompt, CancellationToken cancellationToken);

        /// <summary>
        /// Sends a message to the AI service with streaming response
        /// </summary>
        public abstract Task<Stream> SendStreamedMessageAsync(string prompt, CancellationToken cancellationToken, Action<string> onMessageUpdate);

        /// <summary>
        /// Gets the capabilities of the model
        /// </summary>
        public virtual Task<AIModel> GetCapabilitiesAsync()
        {
            return Task.FromResult(Model);
        }

        /// <summary>
        /// Estimates token count for text
        /// </summary>
        public virtual int EstimateTokens(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;
                
            // Default estimation: ~4 characters per token
            return text.Length / 4 + 1;
        }

        /// <summary>
        /// Helper method to handle API request errors
        /// </summary>
        protected virtual async Task<T> HandleApiRequestAsync<T>(Func<Task<T>> apiCall, string errorContext)
        {
            try
            {
                return await apiCall();
            }
            catch (HttpRequestException ex)
            {
                throw new AIServiceException($"{errorContext}: {ex.Message}", ex) 
                {
                    ProviderName = ProviderName,
                    ModelName = ModelName,
                    StatusCode = ex.StatusCode
                };
            }
            catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
            {
                throw new AIServiceException($"{errorContext}: Request timed out", ex)
                {
                    ProviderName = ProviderName,
                    ModelName = ModelName,
                    IsTimeout = true
                };
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                throw new AIServiceException($"{errorContext}: {ex.Message}", ex)
                {
                    ProviderName = ProviderName,
                    ModelName = ModelName
                };
            }
        }

        /// <summary>
        /// Dispose method to clean up HttpClient
        /// </summary>
        public virtual void Dispose()
        {
            HttpClient?.Dispose();
        }
    }

    /// <summary>
    /// Exception for AI service errors
    /// </summary>
    public class AIServiceException : Exception
    {
        public string ProviderName { get; set; }
        public string ModelName { get; set; }
        public System.Net.HttpStatusCode? StatusCode { get; set; }
        public bool IsTimeout { get; set; }
        
        public AIServiceException(string message) : base(message) { }
        
        public AIServiceException(string message, Exception innerException) 
            : base(message, innerException) { }
    }
}
