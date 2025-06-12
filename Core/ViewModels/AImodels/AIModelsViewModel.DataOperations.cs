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
                
                Debug.WriteLine("AIModelsViewModel: Starting LoadModelsAsync");
                
                // Clear existing models immediately
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Models.Clear();
                    ShowNoResults = false;
                });
                
                // Get models from service - service handles API key filtering
                var models = await _modelManager.GetAllModelsAsync().ConfigureAwait(false);
                
                Debug.WriteLine($"AIModelsViewModel: Retrieved {models?.Count ?? 0} models from model manager");
                
                if (models?.Count > 0)
                {
                    Debug.WriteLine($"Loading {models.Count} models with valid API keys");
                    
                    // Load models incrementally
                    await LoadModelsIncrementally(models, _loadingCancellationToken.Token);
                    
                    // Update refresh timestamp after successful loading
                    _lastModelRefresh = DateTime.Now;
                    
                    Debug.WriteLine($"AIModelsViewModel: Successfully loaded {Models.Count} models into UI");
                }
                else
                {
                    Debug.WriteLine("AIModelsViewModel: No models found or models list is empty");
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        ShowNoResults = true;
                        ErrorMessage = "No models found. Make sure you have API keys configured and try refreshing.";
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
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
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

            Debug.WriteLine($"Loading {sortedModels.Count} models incrementally with animations");
            
            // Clear collections at start
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Models.Clear();
                FilteredModels.Clear();
            });
            
            for (int i = 0; i < sortedModels.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                    
                var model = sortedModels[i];
                
                try
                {
                    // Add model to both collections immediately on main thread
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        Models.Add(model);
                        
                        // Apply filters to this single model and add to FilteredModels if it passes
                        bool passesFilter = true;
                        
                        if (ShowFavoritesOnly && !model.IsFavorite)
                        {
                            passesFilter = false;
                        }
                        
                        if (passesFilter && !string.IsNullOrWhiteSpace(SearchText))
                        {
                            string searchLower = SearchText.ToLowerInvariant();
                            passesFilter = (model.ModelName?.ToLowerInvariant().Contains(searchLower) == true) ||
                                          (model.ProviderName?.ToLowerInvariant().Contains(searchLower) == true) ||
                                          (model.Description?.ToLowerInvariant().Contains(searchLower) == true);
                        }
                        
                        if (passesFilter)
                        {
                            FilteredModels.Add(model);
                            
                            // Trigger fade-in animation for this model
                            _ = Task.Run(async () =>
                            {
                                await Task.Delay(100);
                                await MainThread.InvokeOnMainThreadAsync(() =>
                                {
                                    OnPropertyChanged(nameof(FilteredModels));
                                });
                            });
                        }
                        
                        Debug.WriteLine($"Added model {i + 1}/{sortedModels.Count}: {model.ModelName} (Filtered: {FilteredModels.Count})");
                    });
                    
                    // Visible delay to make the incremental loading animated
                    await Task.Delay(80, cancellationToken);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error adding model {model.ModelName}: {ex.Message}");
                }
            }
            
            // Final update of ShowNoResults and force UI refresh
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ShowNoResults = FilteredModels.Count == 0 && !IsLoading;
                OnPropertyChanged(nameof(FilteredModels));
                OnPropertyChanged(nameof(Models));
            });
            
            Debug.WriteLine($"Incremental loading completed with animations. {Models.Count} models loaded, {FilteredModels.Count} filtered models shown.");
        }

        /// <summary>
        /// Apply filters to current models and update FilteredModels collection
        /// </summary>
        private void ApplyFiltersAndUpdateFilteredModels()
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

                // Always sort filtered models by priority
                var filteredList = filteredModels
                    .OrderByDescending(m => m.IsSelected)
                    .ThenByDescending(m => m.IsFavorite)
                    .ThenByDescending(m => m.IsDefault)
                    .ThenByDescending(m => m.LastUsed ?? DateTime.MinValue)
                    .ThenBy(m => m.ProviderName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(m => m.ModelName, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                
                // Update FilteredModels collection
                FilteredModels.Clear();
                foreach (var model in filteredList)
                {
                    FilteredModels.Add(model);
                }
                
                ShowNoResults = FilteredModels.Count == 0 && !IsLoading;
                
                // Force UI update
                OnPropertyChanged(nameof(FilteredModels));
                
                Debug.WriteLine($"Applied filters with sorting: {Models.Count} total models -> {FilteredModels.Count} filtered models");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error applying filters: {ex.Message}");
            }
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
            ApplyFiltersAndUpdateFilteredModels();
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

        #region Provider Model Management
        /// <summary>
        /// Cleans up models from database and UI for a specific provider
        /// </summary>
        public async Task CleanupProviderModelsAsync(string provider)
        {
            try
            {
                Debug.WriteLine($"Cleaning up models for provider: {provider}");
                
                // Remove models from database first
                int deletedCount = await _modelRepository.DeleteModelsByProviderAsync(provider);
                Debug.WriteLine($"Deleted {deletedCount} models from database for provider: {provider}");
                
                // Remove models from UI collections
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    var modelsToRemove = Models.Where(m => 
                        m.ProviderName.Equals(provider, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    
                    var filteredModelsToRemove = FilteredModels.Where(m => 
                        m.ProviderName.Equals(provider, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    
                    // Animate out models before removing
                    foreach (var model in modelsToRemove)
                    {
                        model.AnimationOpacity = 0.3;
                        model.AnimationScale = 0.9;
                    }
                });
                
                // Wait for animation to complete then remove
                await Task.Delay(300);
                
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // Remove from Models collection
                    var modelsToRemove = Models.Where(m => 
                        m.ProviderName.Equals(provider, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    
                    foreach (var model in modelsToRemove)
                    {
                        Models.Remove(model);
                    }
                    
                    // Remove from FilteredModels collection
                    var filteredModelsToRemove = FilteredModels.Where(m => 
                        m.ProviderName.Equals(provider, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    
                    foreach (var model in filteredModelsToRemove)
                    {
                        FilteredModels.Remove(model);
                    }
                    
                    // Update UI state
                    ShowNoResults = FilteredModels.Count == 0 && !IsLoading;
                    OnPropertyChanged(nameof(Models));
                    OnPropertyChanged(nameof(FilteredModels));
                });
                
                Debug.WriteLine($"Cleaned up models for provider: {provider}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error cleaning up models for provider {provider}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Discovers and loads models for a specific provider
        /// </summary>
        public async Task DiscoverAndLoadProviderModelsAsync(string provider)
        {
            try
            {
                Debug.WriteLine($"AIModelsViewModel: Discovering models for {provider} after API key save");
                
                // Use the model manager to discover models for this provider
                bool discoverySuccess = await _modelManager.DiscoverAndLoadProviderModelsAsync(provider);
                
                if (discoverySuccess)
                {
                    Debug.WriteLine($"AIModelsViewModel: Successfully discovered models for {provider}");
                }
                else
                {
                    Debug.WriteLine($"AIModelsViewModel: No new models found for {provider}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error discovering models for {provider}: {ex.Message}");
                throw;
            }
        }
        #endregion
    }
}
