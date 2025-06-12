using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NexusChat.Core.Models;

namespace NexusChat.Core.ViewModels
{
    /// <summary>
    /// Data operations and processing methods for AI Models ViewModel
    /// </summary>
    public partial class AIModelsViewModel
    {
        #region Model Loading - Single Entry Point
        private CancellationTokenSource? _loadingCancellationToken;

        /// <summary>
        /// Main model loading method - single entry point for all model loading
        /// </summary>
        public async Task LoadModelsAsync()
        {
            try
            {
                // Cancel any existing loading operation
                _loadingCancellationToken?.Cancel();
                _loadingCancellationToken = new CancellationTokenSource();
                
                IsLoading = true;
                HasError = false;
                ErrorMessage = string.Empty;
                
                // Clear existing models immediately
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Models.Clear();
                    ShowNoResults = false;
                });
                
                // Get models from service - service handles API key filtering
                var models = await _modelManager.GetAllModelsAsync().ConfigureAwait(false);
                
                if (models?.Count > 0)
                {
                    Debug.WriteLine($"Loading {models.Count} models with valid API keys");
                    
                    // Load models incrementally
                    await LoadModelsIncrementally(models, _loadingCancellationToken.Token);
                    
                    // Update refresh timestamp after successful loading
                    _lastModelRefresh = DateTime.Now;
                }
                else
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        ShowNoResults = true;
                        ErrorMessage = "No API keys configured. Add an API key to see available models.";
                    });
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Model loading was cancelled");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in LoadModelsAsync: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    HasError = true;
                    ErrorMessage = ex.Message;
                });
            }
            finally
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    IsLoading = false;
                });
            }
        }

        /// <summary>
        /// Loads models incrementally with reduced delay
        /// </summary>
        private async Task LoadModelsIncrementally(List<AIModel> models, CancellationToken cancellationToken)
        {
            // Process models for display
            var processedModels = ProcessModels(models);
            
            // Sort models to show important ones first
            var sortedModels = processedModels
                .OrderByDescending(m => m.IsSelected)
                .ThenByDescending(m => m.IsFavorite) 
                .ThenByDescending(m => m.IsDefault)
                .ThenByDescending(m => m.LastUsed ?? DateTime.MinValue)
                .ThenBy(m => m.ProviderName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(m => m.ModelName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            Debug.WriteLine($"Loading {sortedModels.Count} models incrementally");
            
            for (int i = 0; i < sortedModels.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                    
                var model = sortedModels[i];
                
                try
                {
                    // Add model to UI on main thread
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        Models.Add(model);
                        Debug.WriteLine($"Added model {i + 1}/{sortedModels.Count}: {model.ModelName}");
                    });
                    
                    // Apply filters less frequently for better performance
                    if (i % 10 == 0 || i == sortedModels.Count - 1)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            ApplyFiltersToCurrentModels();
                        });
                    }
                    
                    // Very small delay to allow UI updates
                    await Task.Delay(10, cancellationToken);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error adding model {model.ModelName}: {ex.Message}");
                }
            }
            
            // Final filter application
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ApplyFiltersToCurrentModels();
            });
            
            Debug.WriteLine($"Incremental loading completed. {Models.Count} models loaded.");
        }

        /// <summary>
        /// Cancels any ongoing model loading
        /// </summary>
        public void CancelModelLoading()
        {
            _loadingCancellationToken?.Cancel();
        }
        #endregion

        #region Model Processing
        /// <summary>
        /// Processes models for display
        /// </summary>
        private List<AIModel> ProcessModels(List<AIModel> rawModels)
        {
            try
            {
                Debug.WriteLine($"Processing {rawModels.Count} models");
                
                if (rawModels.Count == 0)
                {
                    Debug.WriteLine("No models to process, creating fallback");
                    return CreateFallbackModels();
                }
                
                var processedModels = new List<AIModel>(rawModels.Count);
                const int chunkSize = 50;
                
                for (int i = 0; i < rawModels.Count; i += chunkSize)
                {
                    var chunk = rawModels.Skip(i).Take(chunkSize);
                    
                    foreach (var model in chunk)
                    {
                        try
                        {
                            model.DisplayName = NormalizeModelName(model.ModelName ?? string.Empty);
                            model.Description = ProcessDescription(model.Description ?? string.Empty);
                            processedModels.Add(model);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error processing model {model?.ModelName}: {ex.Message}");
                        }
                    }
                    
                    if (i + chunkSize < rawModels.Count)
                    {
                        Thread.Yield();
                    }
                }
                
                Debug.WriteLine($"Successfully processed {processedModels.Count} models");
                return processedModels;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ProcessModels: {ex.Message}");
                return CreateFallbackModels();
            }
        }

        /// <summary>
        /// Processes model description for UI display
        /// </summary>
        private string ProcessDescription(string description)
        {
            if (string.IsNullOrEmpty(description))
                return string.Empty;
                
            try
            {
                string processed = description.Trim();
                
                if (processed.Length > 200)
                {
                    processed = processed.Substring(0, 197) + "...";
                }
                
                return processed;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing description: {ex.Message}");
                return description ?? string.Empty;
            }
        }

        /// <summary>
        /// Creates fallback models when needed
        /// </summary>
        private List<AIModel> CreateFallbackModels()
        {
            try
            {
                return new List<AIModel>
                {
                    new AIModel
                    {
                        ProviderName = "Dummy",
                        ModelName = "dummy-gpt",
                        DisplayName = "Dummy GPT",
                        Description = "Fallback model for testing",
                        IsAvailable = true,
                        MaxTokens = 4096,
                        MaxContextWindow = 8192,
                        SupportsStreaming = true,
                        SupportsVision = false,
                        SupportsCodeCompletion = true
                    }
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating fallback models: {ex.Message}");
                return new List<AIModel>();
            }
        }
        #endregion

        #region Filter Operations
        /// <summary>
        /// Apply filters to current models in collection
        /// </summary>
        private void ApplyFiltersToCurrentModels()
        {
            try
            {
                var filteredModels = Models.AsEnumerable();
                
                if (ShowFavoritesOnly)
                {
                    filteredModels = filteredModels.Where(m => m.IsFavorite);
                }
                
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    string searchLower = SearchText.ToLowerInvariant();
                    filteredModels = filteredModels.Where(m => 
                        (m.ModelName?.ToLowerInvariant().Contains(searchLower) == true) ||
                        (m.ProviderName?.ToLowerInvariant().Contains(searchLower) == true) ||
                        (m.Description?.ToLowerInvariant().Contains(searchLower) == true));
                }

                var filteredList = filteredModels.ToList();
                
                // Update collection if filtering changed the results
                if (filteredList.Count != Models.Count)
                {
                    Models.Clear();
                    foreach (var model in filteredList)
                    {
                        Models.Add(model);
                    }
                }
                
                ShowNoResults = Models.Count == 0 && !IsLoading;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error applying filters: {ex.Message}");
            }
        }
        #endregion

        #region Background Operations
        /// <summary>
        /// Refreshes models from providers
        /// </summary>
        public async Task RefreshFromProviders()
        {
            if (IsRefreshing)
                return;
                
            try
            {
                IsRefreshing = true;
                
                Debug.WriteLine("Refreshing models from providers");
                await LoadModelsAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error refreshing models: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    HasError = true;
                    ErrorMessage = $"Error refreshing models: {ex.Message}";
                    IsLoading = false;
                });
            }
            finally
            {
                IsRefreshing = false; 
            }
        }
        #endregion
    }
}
