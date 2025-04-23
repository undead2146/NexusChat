using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Services.ApiKeyManagement;
using NexusChat.Services.Interfaces;

namespace NexusChat.Services
{
    /// <summary>
    /// Service for managing API keys for AI providers
    /// </summary>
    public class ApiKeyManager : IApiKeyManager
    {
        private readonly Dictionary<string, string> _apiKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Regex> _keyValidationPatterns = new Dictionary<string, Regex>();
        private readonly Dictionary<string, KeyMetadata> _keyMetadata = new Dictionary<string, KeyMetadata>(StringComparer.OrdinalIgnoreCase);
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly IEnvironmentService _environmentService;
        
        private const string SECURE_STORAGE_PREFIX = "nexuschat_apikey_";
        
        // Environment variable patterns that define our convention
        private const string MODEL_KEY_PATTERN = @"AI_KEY_([A-Z0-9_]+)(?:_([A-Z0-9_]+))?";
        private const string MODEL_DEFINITION_PATTERN = @"AI_MODEL_([A-Z0-9_]+)_([A-Z0-9_]+)";
        
        /// <summary>
        /// Creates a new instance of ApiKeyManager
        /// </summary>
        public ApiKeyManager(IEnvironmentService environmentService)
        {
            _environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
            InitializeValidationPatterns();
            LoadEnvironmentKeys();
        }

        /// <summary>
        /// Sets a custom API key for a specific service
        /// </summary>
        public async Task<bool> SetCustomApiKeyAsync(string keyName, string apiKey)
        {
            if (string.IsNullOrWhiteSpace(keyName) || string.IsNullOrEmpty(apiKey))
            {
                return false;
            }

            // Check if the key has a valid format if we have a pattern for it
            string providerName = GetProviderFromKeyName(keyName);
            if (!string.IsNullOrEmpty(providerName) && 
                _keyValidationPatterns.TryGetValue(providerName, out var pattern) && 
                !pattern.IsMatch(apiKey))
            {
                return false;
            }
            
            await _lock.WaitAsync();
            try
            {
                // Store key metadata
                _keyMetadata[keyName] = new KeyMetadata
                {
                    Source = KeySource.UserDefined,
                    CreatedTime = DateTime.UtcNow,
                    LastUsedTime = DateTime.UtcNow
                };
                
                // Store the key in memory
                _apiKeys[keyName] = apiKey;
                
                // Store the key securely
                try
                {
                    await SecureStorage.SetAsync(GetSecureStorageKey(keyName), apiKey);
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error storing API key in secure storage: {ex.Message}");
                    // If secure storage fails, just use in-memory storage
                    return true;
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Gets an API key by name
        /// </summary>
        public string GetApiKey(string keyName)
        {
            if (string.IsNullOrEmpty(keyName))
                return null;
                
            // First try in-memory cache
            if (_apiKeys.TryGetValue(keyName, out string key) && !string.IsNullOrEmpty(key))
            {
                // Update metadata
                if (_keyMetadata.TryGetValue(keyName, out var metadata))
                {
                    metadata.LastUsedTime = DateTime.UtcNow;
                    metadata.UseCount++;
                }
                return key;
            }
            
            // Try secure storage
            try
            {
                key = SecureStorage.GetAsync(GetSecureStorageKey(keyName)).Result;
                if (!string.IsNullOrEmpty(key))
                {
                    // Cache it
                    _apiKeys[keyName] = key;
                    return key;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error retrieving key from secure storage: {ex.Message}");
            }
            
            // Try environment service as last resort
            key = _environmentService.GetValue(keyName);
            
            // If direct key didn't work, try with AI_KEY_ prefix
            if (string.IsNullOrEmpty(key) && !keyName.StartsWith(_environmentService.API_KEY_PREFIX, StringComparison.OrdinalIgnoreCase))
            {
                key = _environmentService.GetValue($"{_environmentService.API_KEY_PREFIX}{keyName.ToUpperInvariant()}");
            }
            
            // Cache if found
            if (!string.IsNullOrEmpty(key))
            {
                _apiKeys[keyName] = key;
            }
            
            return key;
        }

        /// <summary>
        /// Resolves an API key using hierarchical resolution
        /// </summary>
        public async Task<string> ResolveApiKeyAsync(string providerName, string modelName = null, string strategyName = "hierarchical")
        {
            if (string.IsNullOrEmpty(providerName))
                return null;
                
            // Try model-specific key first if model name is provided
            if (!string.IsNullOrEmpty(modelName))
            {
                var modelKeyName = GetModelKeyFromNames(providerName, modelName);
                var modelKey = GetApiKey(modelKeyName);
                
                if (!string.IsNullOrEmpty(modelKey))
                {
                    Debug.WriteLine($"Using model-specific key for {providerName}/{modelName}");
                    return modelKey;
                }
                
                // Try environment service directly
                modelKey = _environmentService.GetModelApiKey(providerName, modelName);
                if (!string.IsNullOrEmpty(modelKey))
                {
                    Debug.WriteLine($"Using environment key for {providerName}/{modelName}");
                    return modelKey;
                }
                
                // Try normalized model name (convert slashes to underscores)
                string normalizedModelName = modelName.Replace('/', '_');
                string normalizedKeyName = $"{providerName.ToLowerInvariant()}_{normalizedModelName}";
                string normalizedKey = GetApiKey(normalizedKeyName);
                
                if (!string.IsNullOrEmpty(normalizedKey))
                {
                    Debug.WriteLine($"Using normalized key for {providerName}/{modelName}");
                    return normalizedKey;
                }
            }
            
            // Fall back to provider-level key
            var providerKey = GetApiKey(providerName.ToLowerInvariant());
            if (!string.IsNullOrEmpty(providerKey))
            {
                Debug.WriteLine($"Using provider-level key for {providerName}");
                return providerKey;
            }
            
            // Try provider with _api_key suffix
            var providerApiKeyName = $"{providerName.ToLowerInvariant()}_api_key";
            var providerApiKey = GetApiKey(providerApiKeyName);
            if (!string.IsNullOrEmpty(providerApiKey))
            {
                Debug.WriteLine($"Using {providerApiKeyName} key");
                return providerApiKey;
            }
            
            // Try environment service for provider key
            var envKey = _environmentService.GetApiKey(providerName);
            if (!string.IsNullOrEmpty(envKey))
            {
                Debug.WriteLine($"Using environment provider key for {providerName}");
                return envKey;
            }
            
            return null;
        }

        /// <summary>
        /// Resolves an API key for a specific provider and model
        /// </summary>
        /// <param name="providerId">The provider ID</param>
        /// <param name="modelId">The model ID</param>
        /// <returns>The resolved API key</returns>
        public async Task<string> ResolveApiKeyAsync(string providerId, string modelId)
        {
            // First check for model-specific key
            var modelSpecificKey = await GetApiKeyAsync($"{providerId}_{modelId}");
            if (!string.IsNullOrEmpty(modelSpecificKey))
                return modelSpecificKey;
                
            // Fall back to provider-level key
            return await GetApiKeyAsync(providerId);
        }

        /// <summary>
        /// Validates the format of an API key
        /// </summary>
        public bool ValidateApiKeyFormat(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                return false;
                
            // Generic validation - at least 10 chars, no whitespace
            if (apiKey.Length < 10 || apiKey.Any(char.IsWhiteSpace))
                return false;
                
            // All keys passed basic validation
            return true;
        }

        /// <summary>
        /// Gets the model key from provider and model names
        /// </summary>
        public string GetModelKeyFromNames(string providerName, string modelName)
        {
            if (string.IsNullOrEmpty(providerName) || string.IsNullOrEmpty(modelName))
                return null;
                
            string normalizedProviderName = NormalizeProviderName(providerName);
            string normalizedModelName = NormalizeModelName(modelName);
            
            // Different formatting options depending on provider
            switch (normalizedProviderName.ToLowerInvariant())
            {
                case "openrouter":
                    // Format: AI_MODEL_OPENROUTER_MODELNAME
                    string openRouterModelName = normalizedModelName.Replace('/', '_').Replace('-', '_').ToUpperInvariant();
                    return $"{_environmentService.MODEL_KEY_PREFIX}{normalizedProviderName.ToUpperInvariant()}_{openRouterModelName}";
                    
                default:
                    // For other providers, use the standardized format: provider_model
                    string standardModelName = normalizedModelName.Replace('/', '_');
                    return $"{normalizedProviderName.ToLowerInvariant()}_{standardModelName}";
            }
        }
        
        /// <summary>
        /// Normalizes a provider name for consistency
        /// </summary>
        public string NormalizeProviderName(string providerName)
        {
            if (string.IsNullOrEmpty(providerName))
                return "Unknown";
                
            providerName = providerName.Trim();
            
            // Handle common provider name variations
            switch (providerName.ToLowerInvariant())
            {
                case "groq":
                case "groqi":
                case "groqapi":
                    return "Groq";
                    
                case "openrouter":
                case "or":
                case "openr":
                    return "OpenRouter";
                    
                case "openai":
                case "oai":
                    return "OpenAI";
                    
                case "anthropic":
                    return "Anthropic";
                    
                case "google":
                case "gemini":
                    return "Google";
                    
                case "dummy":
                case "test":
                case "testing":
                    return "Dummy";
                    
                default:
                    // Return capitalized version
                    return char.ToUpper(providerName[0]) + providerName.Substring(1).ToLowerInvariant();
            }
        }
        
        /// <summary>
        /// Normalizes a model name for consistency
        /// </summary>
        public string NormalizeModelName(string modelName)
        {
            if (string.IsNullOrEmpty(modelName))
                return string.Empty;
                
            return modelName.Replace(' ', '-').ToLowerInvariant();
        }
        
        /// <summary>
        /// Gets all available API keys for a provider
        /// </summary>
        public List<string> GetAvailableKeysForProvider(string providerName)
        {
            if (string.IsNullOrEmpty(providerName))
                return new List<string>();
                
            var keys = new List<string>();
            string normalizedProvider = providerName.ToLowerInvariant();
            
            // Find keys that match the provider name
            foreach (var key in _apiKeys.Keys)
            {
                if (key.StartsWith(normalizedProvider + "_", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals(normalizedProvider, StringComparison.OrdinalIgnoreCase))
                {
                    keys.Add(key);
                }
            }
            
            // Also check environment variables
            try
            {
                foreach (string key in _environmentService.GetAllEnvironmentVariables().Keys)
                {
                    // Check for API_KEY_PROVIDERNAME format
                    if (key.StartsWith(_environmentService.API_KEY_PREFIX + providerName.ToUpperInvariant(), StringComparison.OrdinalIgnoreCase))
                    {
                        var normalizedKey = key.Substring(_environmentService.API_KEY_PREFIX.Length).ToLowerInvariant();
                        if (!_apiKeys.ContainsKey(normalizedKey))
                        {
                            keys.Add(normalizedKey);
                        }
                    }
                    
                    // Check for AI_MODEL_PROVIDERNAME_ format (for model-specific keys)
                    if (key.StartsWith(_environmentService.MODEL_KEY_PREFIX + providerName.ToUpperInvariant() + "_", StringComparison.OrdinalIgnoreCase))
                    {
                        string[] parts = key.Split('_');
                        if (parts.Length >= 3)
                        {
                            // Format as provider_model
                            var modelName = string.Join("_", parts.Skip(2));
                            var normalizedKey = $"{normalizedProvider}_{modelName.ToLowerInvariant()}";
                            if (!_apiKeys.ContainsKey(normalizedKey))
                            {
                                keys.Add(normalizedKey);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking environment variables: {ex.Message}");
            }
            
            return keys;
        }
        
        /// <summary>
        /// Gets a masked version of an API key for display
        /// </summary>
        public string GetMaskedKey(string keyName)
        {
            var key = GetApiKey(keyName);
            if (string.IsNullOrEmpty(key))
                return null;
                
            // Mask the key - show first 4 and last 4 characters
            if (key.Length > 8)
            {
                return key.Substring(0, 4) + "****" + key.Substring(key.Length - 4);
            }
            else
            {
                return "********";
            }
        }

        /// <summary>
        /// Extracts the provider name from a key name
        /// </summary>
        private string GetProviderFromKeyName(string keyName)
        {
            if (string.IsNullOrEmpty(keyName))
                return string.Empty;
                
            // Check if it's in provider_model format
            int underscoreIndex = keyName.IndexOf('_');
            if (underscoreIndex > 0)
            {
                return keyName.Substring(0, underscoreIndex);
            }
            
            // Check if it's in API_KEY_PROVIDER format
            if (keyName.StartsWith(_environmentService.API_KEY_PREFIX, StringComparison.OrdinalIgnoreCase))
            {
                return keyName.Substring(_environmentService.API_KEY_PREFIX.Length);
            }
            
            // Otherwise, the whole key is the provider name
            return keyName;
        }
        
        /// <summary>
        /// Gets the secure storage key for a given key name
        /// </summary>
        private string GetSecureStorageKey(string keyName)
        {
            return SECURE_STORAGE_PREFIX + keyName.ToLowerInvariant();
        }
        
        /// <summary>
        /// Initializes the validation patterns for different providers
        /// </summary>
        private void InitializeValidationPatterns()
        {
            // Add validation patterns for additional providers
            _keyValidationPatterns["groq"] = new Regex(@"^gsk_[A-Za-z0-9]{32,}$");
            _keyValidationPatterns["openrouter"] = new Regex(@"^sk-or-v1-[0-9a-f]{64}$");
            _keyValidationPatterns["openai"] = new Regex(@"^sk-[A-Za-z0-9]{32,}$");
            _keyValidationPatterns["anthropic"] = new Regex(@"^sk-ant-[A-Za-z0-9]{32,}$");
            _keyValidationPatterns["google"] = new Regex(@"^[A-Za-z0-9_\-]{30,}$");
        }
        
        /// <summary>
        /// Loads API keys from environment variables
        /// </summary>
        private void LoadEnvironmentKeys()
        {
            try
            {
                // Initialize environment service
                _environmentService.InitializeAsync().Wait();
                
                // Get API keys with AI_KEY_ prefix
                var apiKeys = _environmentService.GetAllApiKeys();
                foreach (var kv in apiKeys)
                {
                    string keyName = kv.Key;
                    
                    // Normalize names - remove AI_KEY_ prefix if present
                    if (keyName.StartsWith(_environmentService.API_KEY_PREFIX, StringComparison.OrdinalIgnoreCase))
                    {
                        keyName = keyName.Substring(_environmentService.API_KEY_PREFIX.Length).ToLowerInvariant();
                    }
                    
                    _apiKeys[keyName] = kv.Value;
                    _keyMetadata[keyName] = new KeyMetadata
                    {
                        Source = KeySource.Environment,
                        CreatedTime = DateTime.UtcNow,
                        LastUsedTime = DateTime.UtcNow
                    };
                }
                
                // Parse AI_KEY_ pattern keys directly
                ParseModelSpecificKeys();
                
                Debug.WriteLine($"Loaded {_apiKeys.Count} API keys from environment");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading API keys from environment: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Parses model-specific keys from environment variables
        /// </summary>
        private void ParseModelSpecificKeys()
        {
            // Get all environment variables
            var allVars = _environmentService.GetAllEnvironmentVariables();
            
            // Look for AI_KEY pattern keys
            foreach (string key in allVars.Keys)
            {
                var keyMatch = Regex.Match(key, MODEL_KEY_PATTERN);
                if (keyMatch.Success && keyMatch.Groups.Count >= 3)
                {
                    string provider = keyMatch.Groups[1].Value;
                    string model = keyMatch.Groups.Count > 2 && keyMatch.Groups[2].Success 
                        ? keyMatch.Groups[2].Value 
                        : null;
                    
                    string value = _environmentService.GetValue(key);
                    
                    if (!string.IsNullOrEmpty(value))
                    {
                        if (string.IsNullOrEmpty(model))
                        {
                            // This is a provider-level key (AI_KEY_PROVIDER)
                            string normalizedKey = provider.ToLowerInvariant();
                            _apiKeys[normalizedKey] = value;
                            _keyMetadata[normalizedKey] = new KeyMetadata
                            {
                                Source = KeySource.Environment,
                                CreatedTime = DateTime.UtcNow,
                                LastUsedTime = DateTime.UtcNow
                            };
                        }
                        else
                        {
                            // This is a model-specific key (AI_KEY_PROVIDER_MODEL)
                            string normalizedKey = $"{provider.ToLowerInvariant()}_{model.ToLowerInvariant()}";
                            _apiKeys[normalizedKey] = value;
                            _keyMetadata[normalizedKey] = new KeyMetadata
                            {
                                Source = KeySource.Environment,
                                CreatedTime = DateTime.UtcNow,
                                LastUsedTime = DateTime.UtcNow
                            };
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Verifies a provider-specific API key format
        /// </summary>
        public bool ValidateProviderSpecificKey(string providerName, string apiKey)
        {
            if (string.IsNullOrEmpty(providerName) || string.IsNullOrEmpty(apiKey))
                return false;
                
            // Check if we have a specific pattern for this provider
            if (_keyValidationPatterns.TryGetValue(providerName.ToLowerInvariant(), out var pattern))
            {
                return pattern.IsMatch(apiKey);
            }
            
            // Fall back to generic validation
            return ValidateApiKeyFormat(apiKey);
        }
        
        /// <summary>
        /// Gets key statistics for diagnostics
        /// </summary>
        public Dictionary<string, string> GetKeyStatistics()
        {
            var stats = new Dictionary<string, string>
            {
                ["TotalKeys"] = _apiKeys.Count.ToString(),
                ["EnvironmentKeys"] = _keyMetadata.Count(kv => kv.Value.Source == KeySource.Environment).ToString(),
                ["SecureStorageKeys"] = _keyMetadata.Count(kv => kv.Value.Source == KeySource.SecureStorage).ToString(),
                ["UserDefinedKeys"] = _keyMetadata.Count(kv => kv.Value.Source == KeySource.UserDefined).ToString()
            };
            
            return stats;
        }
        
        /// <summary>
        /// Gets all stored API key names
        /// </summary>
        private async Task<IEnumerable<string>> GetAllKeysAsync()
        {
            try
            {
                var result = new List<string>();
                
                // Get keys from secure storage
                // This is a simplified implementation as SecureStorage doesn't provide a way to list all keys
                // For a real implementation, you might need to maintain a separate list of keys
                
                // Try common key patterns
                foreach (var providerName in new[] { "openai", "groq", "anthropic", "openrouter", "google" })
                {
                    string keyName = $"{providerName.ToLowerInvariant()}_api_key";
                    try
                    {
                        var key = await SecureStorage.GetAsync(keyName);
                        if (!string.IsNullOrEmpty(key))
                        {
                            result.Add(keyName);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error retrieving key {keyName}: {ex.Message}");
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting all keys: {ex.Message}");
                return Enumerable.Empty<string>();
            }
        }

        /// <summary>
        /// Gets all environment variables
        /// </summary>
        public Dictionary<string, string> GetAllEnvironmentVariables()
        {
            var result = new Dictionary<string, string>();
            
            foreach (var key in Environment.GetEnvironmentVariables().Keys)
            {
                string keyStr = key.ToString();
                string value = Environment.GetEnvironmentVariable(keyStr);
                if (!string.IsNullOrEmpty(value))
                {
                    result[keyStr] = value;
                }
            }
            
            return result;
        }

        /// <summary>
        /// Gets an API key asynchronously
        /// </summary>
        public async Task<string> GetApiKeyAsync(string keyName)
        {
            return await Task.FromResult(GetApiKey(keyName));
        }
    }
}
