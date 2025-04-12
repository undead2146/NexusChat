using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexusChat.Core.Models;
using NexusChat.Data.Interfaces;
using NexusChat.Services.Interfaces;

namespace NexusChat.Core.ViewModels
{
    /// <summary>
    /// View model for the AI Models page
    /// </summary>
    public partial class AIModelsViewModel : BaseViewModel
    {
        private readonly IAIModelRepository _modelRepository;
        private readonly IModelManager _modelManager;
        private readonly IApiKeyManager _apiKeyManager;
        
        /// <summary>
        /// Gets or sets the collection of AI models
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<AIModelItemViewModel> _models;
        
        /// <summary>
        /// Gets or sets the filtered collection of AI models
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<AIModelItemViewModel> _filteredModels;
        
        /// <summary>
        /// Gets or sets the search text
        /// </summary>
        [ObservableProperty]
        private string _searchText;
        
        /// <summary>
        /// Gets or sets whether the page is busy
        /// </summary>
        [ObservableProperty]
        private bool _isBusy;
        
        /// <summary>
        /// Gets or sets whether the page is refreshing
        /// </summary>
        [ObservableProperty]
        private bool _isRefreshing;
        
        /// <summary>
        /// Gets or sets whether to show action result message
        /// </summary>
        [ObservableProperty]
        private bool _showActionResult;
        
        /// <summary>
        /// Gets or sets the last action result message
        /// </summary>
        [ObservableProperty]
        private string _lastActionResult;
        
        /// <summary>
        /// Gets or sets whether the view model has been initialized
        /// </summary>
        [ObservableProperty]
        private bool _isInitialized;
        
        /// <summary>
        /// Gets or sets the total number of models before filtering
        /// </summary>
        [ObservableProperty]
        private int _totalModelCount;
        
        /// <summary>
        /// Gets or sets the number of models after filtering
        /// </summary>
        [ObservableProperty]
        private int _filteredModelCount;

        /// <summary>
        /// Gets or sets whether initial loading is in progress (as opposed to model selection)
        /// </summary>
        [ObservableProperty]
        private bool _isInitialLoading;

        /// <summary>
        /// Gets or sets the ID of the model being animated
        /// </summary>
        [ObservableProperty]
        private int _modelBeingAnimated;
        
        /// <summary>
        /// Gets or sets whether a favorite animation is in progress
        /// </summary>
        [ObservableProperty]
        private bool _isAnimatingFavorite;
        
        /// <summary>
        /// Gets or sets whether a selection animation is in progress
        /// </summary>
        [ObservableProperty]
        private bool _isAnimatingSelection;
        
        /// <summary>
        /// Gets or sets the model currently being animated
        /// </summary>
        [ObservableProperty] 
        private AIModelItemViewModel _animatingModel;

        /// <summary>
        /// Signal to scroll to top when a model is selected/favorited
        /// </summary>
        [ObservableProperty]
        private bool _shouldScrollToTop;

        /// <summary>
        /// Gets or sets the default model ID
        /// </summary>
        [ObservableProperty]
        private int _defaultModelId;

        /// <summary>
        /// Gets or sets the grouped models collection
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<GroupedModels> _groupedModels = new();

        /// <summary>
        /// Gets or sets the notification opacity
        /// </summary>
        [ObservableProperty]
        private double _notificationOpacity = 0;

        /// <summary>
        /// Gets or sets the notification scale
        /// </summary>
        [ObservableProperty]
        private double _notificationScale = 0.9;

        /// <summary>
        /// Gets or sets the model to scroll to
        /// </summary>
        [ObservableProperty]
        private AIModelItemViewModel _scrollToModel;
        
        /// <summary>
        /// Gets whether no results are found
        /// </summary>
        public bool ShowNoResults => !IsLoading && (Models == null || Models.Count == 0 || (FilteredModels != null && FilteredModels.Count == 0));
        
        /// <summary>
        /// Gets whether data is being loaded
        /// </summary>
        public bool IsLoading => IsBusy || IsRefreshing;
        
        /// <summary>
        /// Gets whether data is not being loaded
        /// </summary>
        public bool IsNotLoading => !IsLoading;

        /// <summary>
        /// Creates a new instance of AIModelsViewModel
        /// </summary>
        public AIModelsViewModel(IAIModelRepository modelRepository, IModelManager modelManager, IApiKeyManager apiKeyManager = null)
        {
            _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
            _modelManager = modelManager ?? throw new ArgumentNullException(nameof(modelManager));
            _apiKeyManager = apiKeyManager;
            
            Models = new ObservableCollection<AIModelItemViewModel>();
            FilteredModels = new ObservableCollection<AIModelItemViewModel>();
            
            // Initialize animation states
            ModelBeingAnimated = -1;
            IsAnimatingFavorite = false;
            IsAnimatingSelection = false;
        }
        
        /// <summary>
        /// Initializes the view model
        /// </summary>
        public async Task InitializeAsync()
        {
            if (IsBusy || IsInitialized)
                return;
                
            IsBusy = true;
            IsInitialLoading = true;
            
            try
            {
                // Start loading models in background thread to prevent UI blocking
                await Task.Run(async () => 
                {
                    // Fetch models from repository asynchronously
                    var availableModels = await _modelManager.GetAvailableModelsAsync();
                    
                    // Log models found from environment (outside UI thread)
                    Debug.WriteLine($"Loaded {availableModels.Count} models from repository");
                    
                    // Process models without duplicates (outside UI thread)
                    var providers = availableModels
                        .Select(m => m.ProviderName)
                        .Distinct()
                        .ToList();
                        
                    Debug.WriteLine($"Found models from {providers.Count} providers: {string.Join(", ", providers)}");
                    
                    // Process models (outside UI thread)
                    var processedModels = DeduplicateModels(availableModels);
                    
                    // Create view models for each model with correct flags (outside UI thread)
                    var modelViewModels = new List<AIModelItemViewModel>();
                    foreach (var model in processedModels)
                    {
                        var viewModel = new AIModelItemViewModel(model)
                        {
                            IsDefault = model.Id == _modelManager.DefaultModelId,
                            IsSelected = model.Id == _modelManager.CurrentModel?.Id,
                            IsFavourite = model.IsFavourite // Ensure favorite status is correctly initialized
                        };
                        
                        modelViewModels.Add(viewModel);
                    }
                    
                    // Update UI on main thread with all prepared data
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        // Clear and populate in one go
                        Models = new ObservableCollection<AIModelItemViewModel>(modelViewModels);
                        TotalModelCount = processedModels.Count;
                        DefaultModelId = _modelManager.DefaultModelId;
                        FilteredModelCount = Models.Count;
                        
                        // Apply sorting after models are loaded
                        SortModelsWithFavoritesFirst();
                        
                        // Apply any current search filter
                        ApplyFilter();
                        
                        Debug.WriteLine($"Loaded {Models.Count} AI models into view");
                    });
                });
                
                // Mark as initialized
                IsInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing AIModelsViewModel: {ex.Message}");
                await AnimateNotification("Failed to load models");
            }
            finally
            {
                IsBusy = false;
                IsInitialLoading = false;
            }
        }
        
        /// <summary>
        /// Loads AI models from the repository
        /// </summary>
        private async Task LoadModelsAsync()
        {
            try
            {
                // Get available models from model manager
                var availableModels = await _modelManager.GetAvailableModelsAsync();
                
                // Log models found from environment
                Debug.WriteLine($"Loaded {availableModels.Count} models from repository");
                
                // Log providers found
                var providers = availableModels
                    .Select(m => m.ProviderName)
                    .Distinct()
                    .ToList();
                    
                Debug.WriteLine($"Found models from {providers.Count} providers: {string.Join(", ", providers)}");
                
                // Process models without duplicates
                var processedModels = DeduplicateModels(availableModels);
                TotalModelCount = processedModels.Count;
                
                // Clear existing models
                Models.Clear();
                
                // Create view models for each model with correct flags
                foreach (var model in processedModels)
                {
                    var viewModel = new AIModelItemViewModel(model)
                    {
                        IsDefault = model.Id == _modelManager.DefaultModelId,
                        IsSelected = model.Id == _modelManager.CurrentModel?.Id,
                        IsFavourite = model.IsFavourite // Ensure favorite status is loaded from the database model
                    };
                    
                    Models.Add(viewModel);
                }
                
                DefaultModelId = _modelManager.DefaultModelId;
                
                // Sort models with favorites first
                SortModelsWithFavoritesFirst();
                
                // Set filtered model count which should be correct initially
                FilteredModelCount = Models.Count;
                
                // Apply any current search filter
                ApplyFilter();
                
                // Update grouped models
                UpdateGroupedModels();
                
                Debug.WriteLine($"Loaded {Models.Count} AI models into view");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading models: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Sorts the models collection with current model first, favorites next, then others
        /// </summary>
        private void SortModelsWithFavoritesFirst()
        {
            var orderedModels = new List<AIModelItemViewModel>();
            
            // First add selected model (highest priority)
            var selectedModel = Models.FirstOrDefault(m => m.IsSelected);
            if (selectedModel != null)
            {
                orderedModels.Add(selectedModel);
            }
            
            // Then add favorites that aren't already selected
            var favoriteModels = Models.Where(m => m.IsFavourite && !m.IsSelected)
                                     .OrderBy(m => m.ProviderName)
                                     .ThenBy(m => m.ModelName);
            orderedModels.AddRange(favoriteModels);
            
            // Finally add all other models
            var otherModels = Models.Where(m => !m.IsFavourite && !m.IsSelected)
                                  .OrderBy(m => m.ProviderName)
                                  .ThenBy(m => m.ModelName);
            orderedModels.AddRange(otherModels);
            
            // Update collection without clearing to preserve bindings
            // This avoids the UI freeze by using smart updates
            MainThread.BeginInvokeOnMainThread(() => {
                for (int i = 0; i < orderedModels.Count; i++)
                {
                    var model = orderedModels[i];
                    if (i < Models.Count)
                    {
                        if (Models[i] != model)
                        {
                            // Find the model's current position
                            int currentIndex = Models.IndexOf(model);
                            if (currentIndex >= 0 && currentIndex != i)
                            {
                                // Move model to correct position
                                Models.Move(currentIndex, i);
                            }
                        }
                    }
                    else
                    {
                        // Just in case - shouldn't happen normally
                        Models.Add(model);
                    }
                }
            });
            
            // Update grouped models
            UpdateGroupedModels();
        }
        
        /// <summary>
        /// Deduplicates models based on provider and name
        /// </summary>
        private List<AIModel> DeduplicateModels(List<AIModel> models)
        {
            var result = new List<AIModel>();
            var modelKeys = new HashSet<string>();

            // Take top 50 models at most to avoid excessive numbers
            foreach (var model in models.Take(50))
            {
                // Create a unique key by combining provider and name
                string key = $"{model.ProviderName}:{model.ModelName}".ToLowerInvariant();

                // Only add if we haven't seen this model before
                if (!modelKeys.Contains(key))
                {
                    result.Add(model);
                    modelKeys.Add(key);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Applies the search filter to the models
        /// </summary>
        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                // No filter, use all models
                FilteredModels = new ObservableCollection<AIModelItemViewModel>(Models);
            }
            else
            {
                // Filter models by name, provider, or description
                var filtered = Models.Where(m =>
                    m.ModelName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    m.ProviderName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    m.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                ).ToList();
                
                FilteredModels = new ObservableCollection<AIModelItemViewModel>(filtered);
            }
            
            // Update filtered count
            FilteredModelCount = FilteredModels.Count;
            
            // Update grouped models
            UpdateGroupedModels();
            
            // Notify that we need to update the view
            OnPropertyChanged(nameof(ShowNoResults));
        }

        /// <summary>
        /// Updates the grouped models collection
        /// </summary>
        private void UpdateGroupedModels()
        {
            var filteredModels = Models.Where(m => string.IsNullOrEmpty(SearchText) ||
                                                  m.ModelName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                                  m.ProviderName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
                                                  
            var groups = filteredModels
                .GroupBy(m => m.ProviderName)
                .OrderBy(g => g.Key)
                .Select(g => new GroupedModels(g.Key, g.OrderByDescending(m => m.IsSelected)
                                                      .ThenByDescending(m => m.IsDefault)
                                                      .ThenBy(m => m.ModelName)))
                .ToList();
                
            GroupedModels.Clear();
            foreach (var group in groups)
            {
                GroupedModels.Add(group);
            }
        }
        
        /// <summary>
        /// Command to refresh the models
        /// </summary>
        [RelayCommand]
        private async Task RefreshAsync()
        {
            if (IsBusy)
                return;
                
            IsRefreshing = true;
            
            try
            {
                await LoadModelsAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error refreshing models: {ex.Message}");
                await AnimateNotification("Failed to refresh models");
            }
            finally
            {
                IsRefreshing = false;
            }
        }
        
        /// <summary>
        /// Command to filter models based on search text
        /// </summary>
        [RelayCommand]
        private void FilterModels()
        {
            ApplyFilter();
            UpdateGroupedModels();
        }
        
        /// <summary>
        /// Command to set the default model - optimized for UI performance
        /// </summary>
        [RelayCommand]
        private async Task SetDefaultModelAsync(int modelId)
        {
            if (modelId <= 0 || IsAnimatingFavorite || IsAnimatingSelection)
                return;
                
            try
            {
                // Find the model
                var model = Models.FirstOrDefault(m => m.Id == modelId);
                if (model == null || model.IsDefault)
                    return;
                
                // Store previous state for potential rollback
                int previousDefaultId = Models.FirstOrDefault(m => m.IsDefault)?.Id ?? -1;
                IsAnimatingFavorite = true;
                ModelBeingAnimated = modelId;
                AnimatingModel = model;
                
                // STEP 1: Update UI state immediately for responsive feel
                await MainThread.InvokeOnMainThreadAsync(() => {
                    // Update model states
                    foreach (var m in Models)
                    {
                        m.IsDefault = m.Id == modelId;
                    }
                    
                    // Signal to scroll to top
                    ShouldScrollToTop = true;
                });
                
                // STEP 2: Perform database operation in background
                bool success = await Task.Run(async () => {
                    try {
                        return await _modelRepository.SetDefaultModelAsync(modelId);
                    }
                    catch (Exception ex) {
                        Debug.WriteLine($"Background operation failed: {ex.Message}");
                        return false;
                    }
                });
                
                // Small delay for animation to complete
                await Task.Delay(350);
                
                // STEP 3: Update collection order if database operation was successful
                if (success)
                {
                    await MainThread.InvokeOnMainThreadAsync(() => {
                        SortModelsWithFavoritesFirst();
                    });
                    
                    // Signal to scroll to top AFTER reordering
                    await Task.Delay(150);
                    ShouldScrollToTop = true;
                }
                else
                {
                    // Rollback UI state if operation failed
                    await MainThread.InvokeOnMainThreadAsync(() => {
                        foreach (var m in Models)
                        {
                            m.IsDefault = m.Id == previousDefaultId;
                        }
                    });
                    await AnimateNotification("Failed to set default model");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting default model: {ex.Message}");
                await AnimateNotification("Error setting default model");
            }
            finally
            {
                IsAnimatingFavorite = false;
                ModelBeingAnimated = -1;
                AnimatingModel = null;
                await Task.Delay(50);
                ShouldScrollToTop = false; // Reset scroll flag
            }
        }
        
        /// <summary>
        /// Command to toggle the favorite status of a model
        /// </summary>
        [RelayCommand]
        private async Task ToggleFavoriteAsync(int modelId)
        {
            if (modelId <= 0 || IsAnimatingFavorite || IsAnimatingSelection)
                return;
                
            try
            {
                // Set animation state
                IsAnimatingFavorite = true;
                ModelBeingAnimated = modelId;
                
                // Find the model
                var model = Models.FirstOrDefault(m => m.Id == modelId);
                if (model == null)
                    return;
                    
                AnimatingModel = model;
                
                // Update UI immediately for responsiveness
                bool newFavoriteStatus = !model.IsFavourite;
                model.IsFavourite = newFavoriteStatus;
                
                // Important: If we're setting a favorite, scroll to it first BEFORE reordering
                if (newFavoriteStatus)
                {
                    // Scroll to show the star animation (shorter delay)
                    await Task.Delay(75); 
                    ScrollToModel = model;
                }
                
                // Perform database operation in background
                bool success = await Task.Run(async () => {
                    try {
                        return await _modelRepository.ToggleFavoriteAsync(modelId);
                    }
                    catch (Exception ex) {
                        Debug.WriteLine($"Error toggling favorite: {ex.Message}");
                        return false;
                    }
                });
                
                if (!success)
                {
                    // Rollback UI if operation failed
                    model.IsFavourite = !newFavoriteStatus;
                    await AnimateNotification("Failed to update favorites");
                    
                    // Clear animation state early to allow other interactions
                    IsAnimatingFavorite = false;
                    ModelBeingAnimated = -1;
                    AnimatingModel = null;
                    ScrollToModel = null;
                    return;
                }
                
                // Reorder collection with a shorter delay
                await Task.Delay(100); 
                SortModelsWithFavoritesFirst();
                
                // Scroll to model in its new position (shorter delay)
                await Task.Delay(75);
                ScrollToModel = model;
                
                // Show notification
                string message = newFavoriteStatus ? 
                    $"{model.ModelName} added to favorites" : 
                    $"{model.ModelName} removed from favorites";
                    
                // Show notification but don't wait for it to complete
                _ = AnimateNotification(message);
                
                // Clear animation state early to allow other interactions
                IsAnimatingFavorite = false;
                ModelBeingAnimated = -1;
                AnimatingModel = null;
                
                // Reset scroll target after a shorter delay
                await Task.Delay(100);
                ScrollToModel = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error toggling favorite: {ex.Message}");
                await AnimateNotification("Error updating favorites");
                
                // Always reset animation state in case of error
                IsAnimatingFavorite = false;
                ModelBeingAnimated = -1;
                AnimatingModel = null;
                ScrollToModel = null;
            }
        }
        
        /// <summary>
        /// Command to select a model - optimized for UI performance with smooth animations
        /// </summary>
        [RelayCommand]
        private async Task SelectModelAsync(AIModelItemViewModel model)
        {
            if (model == null || model.IsSelected || IsAnimatingSelection || IsAnimatingFavorite)
                return;
            
            try
            {
                // Set animation state
                IsAnimatingSelection = true;
                ModelBeingAnimated = model.Id;
                AnimatingModel = model;
                
                // Store the previous selected model's ID if any
                var previousSelectedModel = Models.FirstOrDefault(m => m.IsSelected);
                int previousSelectedId = previousSelectedModel?.Id ?? -1;
                
                // STEP 1: Update model selection state immediately
                foreach (var m in Models)
                {
                    m.IsSelected = m.Id == model.Id;
                }
                
                // Scroll to the newly selected model before reordering (shorter delay)
                await Task.Delay(75);
                ScrollToModel = model;
                
                // STEP 2: Update the model service in background without blocking the UI
                _ = Task.Run(async () => {
                    try {
                        await _modelManager.GetServiceForModelAsync(model.Id);
                    }
                    catch (Exception ex) {
                        Debug.WriteLine($"Background service update failed: {ex.Message}");
                    }
                });
                
                // STEP 3: Move item to top and maintain proper order (shorter delay)
                await Task.Delay(100);
                SortModelsWithFavoritesFirst();
                
                // Allow interactions again after the resorting
                IsAnimatingSelection = false;
                
                // Scroll to the model in its new position (shorter delay)
                await Task.Delay(75);
                ScrollToModel = model;
                
                // Show notification but don't wait for completion
                _ = AnimateNotification($"{model.ModelName} selected as current model");
                
                // Reset scroll target after a short delay
                await Task.Delay(75);
                ScrollToModel = null;
                ModelBeingAnimated = -1;
                AnimatingModel = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error selecting model: {ex.Message}");
                await AnimateNotification("Failed to select model");
                
                // Always reset animation state
                IsAnimatingSelection = false;
                ModelBeingAnimated = -1;
                AnimatingModel = null;
                ScrollToModel = null;
            }
        }
        
        /// <summary>
        /// Command to show model info
        /// </summary>
        [RelayCommand]
        private async Task ShowModelInfoAsync(AIModelItemViewModel model)
        {
            if (model == null)
                return;
                
            // For now, just show a simple alert - could be expanded to a detailed view
            string modelInfo = $"Model: {model.ModelName}\n" +
                              $"Provider: {model.ProviderName}\n" +
                              $"Max Tokens: {model.MaxTokens}\n" +
                              $"Max Context: {model.MaxContextWindow}\n" +
                              $"Default Temperature: {model.DefaultTemperature}\n" +
                              $"ID: {model.Id}";
                              
            await Application.Current.MainPage.DisplayAlert("Model Info", modelInfo, "Close");
        }

        /// <summary>
        /// Handles notification animations
        /// </summary>
        private async Task ShowNotification(string message)
        {
            LastActionResult = message;
            ShowActionResult = true;
            
            // Set animation starting state
            NotificationOpacity = 0;
            NotificationScale = 0.9;
            
            // Animate in
            await Task.WhenAll(
                Task.Run(async () => {
                    for (double i = 0; i <= 1; i += 0.1)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() => {
                            NotificationOpacity = i;
                        });
                        await Task.Delay(20);
                    }
                }),
                Task.Run(async () => {
                    for (double i = 0.9; i <= 1; i += 0.02)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() => {
                            NotificationScale = i;
                        });
                        await Task.Delay(20);
                    }
                })
            );
            
            // Wait for display duration
            await Task.Delay(2000);
            
            // Animate out
            await Task.WhenAll(
                Task.Run(async () => {
                    for (double i = 1; i >= 0; i -= 0.1)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() => {
                            NotificationOpacity = i;
                        });
                        await Task.Delay(15);
                    }
                }),
                Task.Run(async () => {
                    for (double i = 1; i >= 0.9; i -= 0.02)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() => {
                            NotificationScale = i;
                        });
                        await Task.Delay(15);
                    }
                })
            );
            
            ShowActionResult = false;
        }

        /// <summary>
        /// Makes the notification logic reusable and MVVM-compliant
        /// </summary>
        private async Task AnimateNotification(string message)
        {
            await ShowNotification(message);
        }

        /// <summary>
        /// Tries to scroll to the default model
        /// </summary>
        private void TryScrollToDefaultModel()
        {
            try
            {
                // Find the default model in the models list
                var defaultModel = Models.FirstOrDefault(m => m.IsDefault);
                if (defaultModel != null)
                {
                    ScrollToModel = defaultModel;
                    // Reset after a delay to allow the scroll behavior to trigger
                    Task.Delay(100).ContinueWith(_ => ScrollToModel = null);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting scroll to model: {ex.Message}");
            }
        }

        /// <summary>
        /// Persists the current state of models (selected and favorites) to the database
        /// This is called when leaving the page to ensure states are saved
        /// </summary>
        public async Task PersistModelStatesAsync()
        {
            if (!IsInitialized || Models == null || Models.Count == 0)
                return;
                
            try
            {
                Debug.WriteLine("Persisting model states to database...");
                
                // Get current selected model
                var selectedModel = Models.FirstOrDefault(m => m.IsSelected);
                if (selectedModel != null)
                {
                    // Ensure the model manager has the same selected model
                    var modelEntity = await _modelRepository.GetModelByIdAsync(selectedModel.Id);
                    if (modelEntity != null)
                    {
                        await _modelManager.SetCurrentModelAsync(modelEntity);
                        Debug.WriteLine($"Current model set to {modelEntity.ModelName}");
                    }
                }
                
                // Get current default model
                var defaultModel = Models.FirstOrDefault(m => m.IsDefault);
                if (defaultModel != null)
                {
                    // Save default model ID to preferences
                    await _modelManager.SetDefaultModelAsync(defaultModel.Id);
                    Debug.WriteLine($"Default model set to {defaultModel.ModelName}");
                }
                
                // Persist all favorite models to database
                var favorites = Models.Where(m => m.IsFavourite).ToList();
                foreach (var favorite in favorites)
                {
                    // Only update if needed (to avoid unnecessary DB operations)
                    var dbModel = await _modelRepository.GetModelByIdAsync(favorite.Id);
                    if (dbModel != null && !dbModel.IsFavourite)
                    {
                        dbModel.IsFavourite = true;
                        await _modelRepository.UpdateModelAsync(dbModel);
                    }
                }
                
                // Update non-favorites as well
                var nonFavorites = Models.Where(m => !m.IsFavourite).ToList();
                foreach (var nonFavorite in nonFavorites)
                {
                    // Only update if needed (to avoid unnecessary DB operations)
                    var dbModel = await _modelRepository.GetModelByIdAsync(nonFavorite.Id);
                    if (dbModel != null && dbModel.IsFavourite)
                    {
                        dbModel.IsFavourite = false;
                        await _modelRepository.UpdateModelAsync(dbModel);
                    }
                }
                
                Debug.WriteLine($"Successfully persisted states for {favorites.Count} favorite models");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error persisting model states: {ex.Message}");
            }
        }
    }
}
