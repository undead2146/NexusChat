using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Controls;
using NexusChat.Core.Models;

namespace NexusChat.Core.ViewModels
{
    public partial class AIModelsViewModel
    {
        #region Page Lifecycle Commands
        [RelayCommand]
        private async Task PageAppearing()
        {
            Debug.WriteLine("AIModelsViewModel: PageAppearing called");
            
            if (!_hasBeenInitialized)
            {
                // Initialize services quickly on main thread
                await InitializeAsync();
                
                // Start animation for initial loading
                await AnimateRefreshButton();
                
                // Start incremental loading immediately
                await LoadModelsAsync();
                
                // Start background discovery after incremental loading is done
                _ = Task.Run(() => BackgroundDiscoveryOnly());
                
                _hasBeenInitialized = true;
                _isInitialLoad = false;
            }
            else
            {
                bool shouldRefreshModels = Models.Count == 0 || 
                                           DateTime.Now - _lastModelRefresh > _modelCacheTimeout;
                
                if (shouldRefreshModels)
                {
                    Debug.WriteLine("PageAppearing: Cache is stale, loading models");
                    
                    // Start animation for cache refresh
                    await AnimateRefreshButton();
                    await LoadModelsAsync();
                }
                else
                {
                    Debug.WriteLine("PageAppearing: Using cached models");
                    await UpdateCurrentSelection();
                    if (DateTime.Now - _lastModelRefresh > TimeSpan.FromMinutes(5))
                    {
                        _ = Task.Run(() => BackgroundDiscoveryOnly());
                    }
                }
            }
        }

        [RelayCommand]
        private async Task PageDisappearing()
        {
            Debug.WriteLine("AIModelsViewModel: PageDisappearing called");
            
            // Cancel any ongoing loading when leaving the page
            CancelModelLoading();
        }
        #endregion

        #region Model Selection Commands
        [RelayCommand]
        private async Task SelectModel(AIModel model)
        {
            if (model == null) return;

            try
            {
                if (model.IsSelected)
                {
                    ShowNotification($"{NormalizeModelName(model.ModelName)} is already selected");
                    return;
                }

                string apiKey = await _apiKeyManager.GetApiKeyAsync(model.ProviderName);
                if (string.IsNullOrEmpty(apiKey))
                {
                    ShowNotification($"Missing API key for {model.ProviderName}");
                    return;
                }

                IsLoading = true;
                
                // Update UI with proper property notifications FIRST
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // Clear all selected flags first
                    foreach (var m in Models)
                    {
                        if (m.IsSelected)
                        {
                            m.IsSelected = false;
                        }
                    }
                    foreach (var m in FilteredModels)
                    {
                        if (m.IsSelected)
                        {
                            m.IsSelected = false;
                        }
                    }
                    
                    // Set the new selected model
                    model.IsSelected = true;
                    
                    var filteredModel = FilteredModels.FirstOrDefault(m => 
                        m.ProviderName == model.ProviderName && m.ModelName == model.ModelName);
                    if (filteredModel != null && filteredModel != model)
                    {
                        filteredModel.IsSelected = true;
                    }
                    
                    // Force immediate UI refresh
                    OnPropertyChanged(nameof(Models));
                    OnPropertyChanged(nameof(FilteredModels));
                });
                
                // Start animation
                await TriggerModelSelectionAnimation(model);
                
                bool success = await _modelManager.SetCurrentModelAsync(model);
                if (success)
                {
                    ShowNotification($"Now using {NormalizeModelName(model.ModelName)}");
                    SelectedModel = model;
                    
                    _ = _modelManager.RecordModelUsageAsync(model.ProviderName, model.ModelName);
                    
                    // Re-sort and update collections
                    await ResortAndUpdateModels();
                    
                    ScrollToModelRequested?.Invoke(model);
                }
                else
                {
                    // Revert UI changes if database update failed
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        foreach (var m in Models)
                        {
                            m.IsSelected = m == SelectedModel;
                        }
                        foreach (var m in FilteredModels)
                        {
                            m.IsSelected = m == SelectedModel;
                        }
                        OnPropertyChanged(nameof(Models));
                        OnPropertyChanged(nameof(FilteredModels));
                    });
                    
                    ShowNotification("Failed to select model");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error selecting model: {ex.Message}");
                HasError = true;
                ErrorMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SetDefaultModel(AIModel model)
        {
            if (model == null) return;

            try
            {
                if (model.IsDefault)
                {
                    ShowNotification($"{NormalizeModelName(model.ModelName)} is already the default");
                    return;
                }
                
                // Update UI with proper property notifications FIRST
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // Clear all default flags for this provider first
                    foreach (var m in Models.Where(m => m.ProviderName.Equals(model.ProviderName, StringComparison.OrdinalIgnoreCase)))
                    {
                        if (m.IsDefault)
                        {
                            m.IsDefault = false;
                        }
                    }
                    // Set the new default
                    model.IsDefault = true;
                    
                    // Update FilteredModels collection too
                    foreach (var m in FilteredModels.Where(m => m.ProviderName.Equals(model.ProviderName, StringComparison.OrdinalIgnoreCase)))
                    {
                        m.IsDefault = m == model;
                    }
                    
                    // Force immediate UI refresh
                    OnPropertyChanged(nameof(Models));
                    OnPropertyChanged(nameof(FilteredModels));
                });
                
                // Start animation
                await TriggerModelDefaultAnimation(model);
                
                bool success = await _modelManager.SetDefaultModelAsync(model.ProviderName, model.ModelName);
                
                if (success)
                {
                    ShowNotification($"{NormalizeModelName(model.ModelName)} set as default for {model.ProviderName}");
                    
                    // Re-sort and update collections
                    await ResortAndUpdateModels();
                }
                else
                {
                    // Revert UI changes if database update failed
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        foreach (var m in Models.Where(m => m.ProviderName.Equals(model.ProviderName, StringComparison.OrdinalIgnoreCase)))
                        {
                            m.IsDefault = false;
                        }
                        model.IsDefault = false;
                        
                        foreach (var m in FilteredModels.Where(m => m.ProviderName.Equals(model.ProviderName, StringComparison.OrdinalIgnoreCase)))
                        {
                            m.IsDefault = false;
                        }
                        OnPropertyChanged(nameof(Models));
                        OnPropertyChanged(nameof(FilteredModels));
                    });
                    
                    ShowNotification("Failed to set default model");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting default model: {ex.Message}");
                ShowNotification($"Error: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task ToggleFavorite(AIModel model)
        {
            if (model == null) return;

            try
            {
                Debug.WriteLine($"Toggling favorite for {model.ProviderName}/{model.ModelName}");
                
                bool newFavoriteStatus = !model.IsFavorite;
                
                // Update UI with proper property notifications FIRST
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    model.IsFavorite = newFavoriteStatus;
                    
                    // Update the corresponding model in FilteredModels
                    var filteredModel = FilteredModels.FirstOrDefault(m => 
                        m.ProviderName == model.ProviderName && m.ModelName == model.ModelName);
                    if (filteredModel != null && filteredModel != model)
                    {
                        filteredModel.IsFavorite = newFavoriteStatus;
                    }
                    
                    // Force immediate UI refresh
                    OnPropertyChanged(nameof(Models));
                    OnPropertyChanged(nameof(FilteredModels));
                });
                
                // Start animation
                await TriggerModelFavoriteAnimation(model, newFavoriteStatus);
                
                string notificationText = model.IsFavorite ? 
                    $"{NormalizeModelName(model.ModelName)} added to favorites" : 
                    $"{NormalizeModelName(model.ModelName)} removed from favorites";
                ShowNotification(notificationText);
                
                bool success = await _modelManager.SetFavoriteStatusAsync(
                    model.ProviderName, model.ModelName, model.IsFavorite);
                
                if (success)
                {
                    Debug.WriteLine($"Successfully updated favorite status to {model.IsFavorite}");
                    
                    if (ShowFavoritesOnly && !model.IsFavorite)
                    {
                        // Animate out before removing
                        await AnimateModelOut(model);
                        
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            FilteredModels.Remove(model);
                        });
                    }
                    else
                    {
                        // Re-sort and update collections
                        await ResortAndUpdateModels();
                    }
                }
                else
                {
                    // Revert UI changes if database update failed
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        model.IsFavorite = !newFavoriteStatus;
                        
                        var filteredModel = FilteredModels.FirstOrDefault(m => 
                            m.ProviderName == model.ProviderName && m.ModelName == model.ModelName);
                        if (filteredModel != null)
                        {
                            filteredModel.IsFavorite = !newFavoriteStatus;
                        }
                        OnPropertyChanged(nameof(Models));
                        OnPropertyChanged(nameof(FilteredModels));
                    });
                    
                    ShowNotification("Failed to update favorite status");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error toggling favorite: {ex.Message}");
                HasError = true;
                ErrorMessage = $"Error updating favorite status: {ex.Message}";
            }
        }

        /// <summary>
        /// Triggers selection animation with visual feedback
        /// </summary>
        private async Task TriggerModelSelectionAnimation(AIModel model)
        {
            try
            {
                Debug.WriteLine($"Starting selection animation for {model.ModelName}");
                
                // Force immediate UI update
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // Force collection change notification
                    var index = FilteredModels.IndexOf(model);
                    if (index >= 0)
                    {
                        FilteredModels[index] = model;
                    }
                    
                    // Force property updates
                    OnPropertyChanged(nameof(FilteredModels));
                });
                
                
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in selection animation: {ex.Message}");
            }
        }

        /// <summary>
        /// Triggers default animation with visual feedback
        /// </summary>
        private async Task TriggerModelDefaultAnimation(AIModel model)
        {
            try
            {
                Debug.WriteLine($"Starting default animation for {model.ModelName}");
                
                // Force immediate UI update
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // Force collection change notification
                    var index = FilteredModels.IndexOf(model);
                    if (index >= 0)
                    {
                        FilteredModels[index] = model;
                    }
                    
                    OnPropertyChanged(nameof(FilteredModels));
                });
                
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in default animation: {ex.Message}");
            }
        }

        /// <summary>
        /// Triggers favorite animation with visual feedback
        /// </summary>
        private async Task TriggerModelFavoriteAnimation(AIModel model, bool isFavorite)
        {
            try
            {
                Debug.WriteLine($"Starting favorite animation for {model.ModelName} - {(isFavorite ? "favoriting" : "unfavoriting")}");
                
                // Force immediate UI update
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // Force collection change notification
                    var index = FilteredModels.IndexOf(model);
                    if (index >= 0)
                    {
                        FilteredModels[index] = model;
                    }
                    
                    OnPropertyChanged(nameof(FilteredModels));
                });
                
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in favorite animation: {ex.Message}");
            }
        }

        /// <summary>
        /// Animates a model out before removal
        /// </summary>
        private async Task AnimateModelOut(AIModel model)
        {
            try
            {
                Debug.WriteLine($"Starting removal animation for {model.ModelName}");
                
                // Fade out animation
                _ = Task.Run(async () =>
                {
                    for (int i = 10; i >= 0; i--)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            model.AnimationOpacity = i / 10.0;
                        });
                    }
                });
                
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in removal animation: {ex.Message}");
            }
        }

        #region API Key Management Commands
        [RelayCommand]
        private async Task DisplayApiKeyOverlay()
        {
            // Refresh the existing API keys when showing the overlay
            await UpdateExistingApiKeys();
            ShowApiKeyOverlay = true;
        }

        [RelayCommand]
        private void CloseApiKeyOverlay()
        {
            ShowApiKeyOverlay = false;
        }

        [RelayCommand]
        private async Task SaveApiKey(string provider)
        {
            if (string.IsNullOrEmpty(provider)) return;

            try
            {
                string key = await Shell.Current.DisplayPromptAsync(
                    $"{provider} API Key",
                    $"Enter your {provider} API key:",
                    "Save",
                    "Cancel",
                    "API key",
                    -1,
                    Keyboard.Text);
                
                if (!string.IsNullOrEmpty(key))
                {
                    bool success = await _apiKeyManager.SaveProviderApiKeyAsync(provider, key);
                    if (success)
                    {
                        ShowNotification($"{provider} API key saved successfully");
                        
                        // Refresh existing keys display
                        await UpdateExistingApiKeys();
                        
                        // Clear model cache to force fresh discovery
                        _lastModelRefresh = DateTime.MinValue;
                        
                        // Clear API key cache to ensure fresh validation
                        await _apiKeyManager.ClearCacheAsync();
                        
                        // Clear discovery service cache before triggering discovery
                        await _modelDiscoveryService.ClearProviderCacheAsync(provider);
                        
                        // Force fresh discovery for the new provider
                        await DiscoverAndLoadProviderModelsAsync(provider);
                        
                        // Refresh models to show new provider's models
                        await LoadModelsAsync();
                        
                        Debug.WriteLine($"After API key save: Models.Count = {Models.Count}, FilteredModels.Count = {FilteredModels.Count}");
                    }
                    else
                    {
                        ShowNotification($"Failed to save {provider} API key");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving API key: {ex.Message}");
                ShowNotification($"Error saving API key: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task RemoveApiKey(string provider)
        {
            if (string.IsNullOrEmpty(provider)) return;

            try
            {
                bool confirm = await Shell.Current.DisplayAlert(
                    "Remove API Key",
                    $"Are you sure you want to remove the {provider} API key?\n\nThis will permanently remove all {provider} models and their favorites from your account.",
                    "Remove",
                    "Cancel");

                if (confirm)
                {
                    IsLoading = true;
                    
                    // Step 1: Get models to be removed for comprehensive cleanup
                    var modelsToRemove = Models.Where(m => 
                        m.ProviderName.Equals(provider, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    
                    var favoriteModelsToRemove = modelsToRemove.Where(m => m.IsFavorite).ToList();
                    
                    Debug.WriteLine($"Removing API key for {provider}: {modelsToRemove.Count} models total, {favoriteModelsToRemove.Count} favorites");
                    
                    // Step 2: Remove all favorite status from database BEFORE removing API key
                    foreach (var model in favoriteModelsToRemove)
                    {
                        try
                        {
                            await _modelManager.SetFavoriteStatusAsync(model.ProviderName, model.ModelName, false);
                            Debug.WriteLine($"Removed favorite status for {model.ProviderName}/{model.ModelName}");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error removing favorite status for {model.ModelName}: {ex.Message}");
                        }
                    }
                    
                    // Step 3: Remove current model selection if it belongs to this provider
                    if (_modelManager.CurrentModel != null && 
                        _modelManager.CurrentModel.ProviderName.Equals(provider, StringComparison.OrdinalIgnoreCase))
                    {
                        await _modelManager.ClearCurrentModelAsync();
                    }
                    
                    // Step 4: Remove API key (this will trigger the ApiKeyRemoved event)
                    bool apiKeyRemoved = await _apiKeyManager.DeleteProviderApiKeyAsync(provider);
                    if (!apiKeyRemoved)
                    {
                        ShowNotification($"Failed to remove {provider} API key");
                        return;
                    }
                    
                    // Step 5: Comprehensive model cleanup from database
                    await CleanupProviderModelsComprehensiveAsync(provider);
                    
                    // Step 6: Clear all caches to prevent stale data
                    await _apiKeyManager.ClearCacheAsync();
                    await _modelDiscoveryService.ClearProviderCacheAsync(provider);
                    await _modelManager.RefreshModelCacheAsync();
                    
                    // Step 7: Update existing keys display
                    await UpdateExistingApiKeys();
                    
                    // Step 8: Force complete model refresh
                    _lastModelRefresh = DateTime.MinValue;
                    await LoadModelsAsync();
                    
                    // Step 9: Notify MainPageViewModel to refresh favorites
                    await NotifyMainPageToRefreshFavoritesAsync();
                    
                    // Show completion notification
                    string message = favoriteModelsToRemove.Count > 0
                        ? $"{provider} API key removed. {modelsToRemove.Count} models deleted including {favoriteModelsToRemove.Count} favorites."
                        : $"{provider} API key removed. {modelsToRemove.Count} models deleted.";
                    
                    ShowNotification(message);
                    
                    // If no API keys remain, close overlay
                    if (ExistingApiKeys.Count == 0)
                    {
                        ShowApiKeyOverlay = false;
                        ShowNoResults = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error removing API key: {ex.Message}");
                ShowNotification($"Error removing API key: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Comprehensive cleanup of provider models from database and UI
        /// </summary>
        private async Task CleanupProviderModelsComprehensiveAsync(string provider)
        {
            try
            {
                Debug.WriteLine($"Starting comprehensive cleanup for provider: {provider}");
                
                // Step 1: Remove all models for this provider from database with transaction safety
                int deletedCount = await _modelRepository.DeleteModelsByProviderAsync(provider);
                Debug.WriteLine($"Deleted {deletedCount} models from database for provider: {provider}");
                
                // Step 2: Clear any cached favorites or selections for this provider
                await _modelRepository.ClearProviderCacheAsync(provider);
                
                // Step 3: Remove models from UI collections
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    var modelsToRemove = Models.Where(m => 
                        m.ProviderName.Equals(provider, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    
                    var filteredModelsToRemove = FilteredModels.Where(m => 
                        m.ProviderName.Equals(provider, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    
                    // Remove from both collections
                    foreach (var model in modelsToRemove.ToList())
                    {
                        Models.Remove(model);
                    }
                    
                    foreach (var model in filteredModelsToRemove.ToList())
                    {
                        FilteredModels.Remove(model);
                    }
                    
                    // Update UI state
                    ShowNoResults = FilteredModels.Count == 0 && !IsLoading;
                    OnPropertyChanged(nameof(Models));
                    OnPropertyChanged(nameof(FilteredModels));
                });
                
                Debug.WriteLine($"Successfully completed comprehensive cleanup for provider: {provider}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in comprehensive cleanup for provider {provider}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Notifies MainPageViewModel to refresh its favorite models
        /// </summary>
        private async Task NotifyMainPageToRefreshFavoritesAsync()
        {
            try
            {
                // Use MessagingCenter or WeakReferenceMessenger to notify MainPageViewModel
                WeakReferenceMessenger.Default.Send(new FavoritesChangedMessage());
                Debug.WriteLine("Sent FavoritesChangedMessage to refresh MainPage favorites");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error notifying MainPage to refresh favorites: {ex.Message}");
            }
        }

        /// <summary>
        /// Discovers and loads models for a specific provider
        /// </summary>
        private async Task DiscoverAndLoadProviderModels(string provider)
        {
            try
            {
                await DiscoverAndLoadProviderModelsAsync(provider);
                ShowNotification($"Discovered models for {provider}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error discovering models for {provider}: {ex.Message}");
                ShowNotification($"Error discovering models for {provider}");
            }
        }

        [RelayCommand]
        private async Task AddApiKey()
        {
            try
            {
                var selectedProvider = await Shell.Current.DisplayActionSheet(
                    "Select Provider",
                    "Cancel",
                    null,
                    "Groq", "OpenRouter", "Anthropic", "OpenAI");
                
                if (selectedProvider != "Cancel" && !string.IsNullOrEmpty(selectedProvider))
                {
                    await SaveApiKey(selectedProvider);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in AddApiKey: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task ShowApiKeyCommand()
        {
            await UpdateExistingApiKeys();
            IsApiKeyOverlayVisible = true;
        }

        /// <summary>
        /// Updates the list of existing API keys for display
        /// </summary>
        private async Task UpdateExistingApiKeys()
        {
            try
            {
                var knownProviders = new[] { "Groq", "OpenRouter", "Anthropic", "OpenAI", "Azure" };
                var existingKeys = new List<string>();
                
                foreach (var provider in knownProviders)
                {
                    bool hasKey = await _apiKeyManager.HasActiveApiKeyAsync(provider);
                    if (hasKey)
                    {
                        existingKeys.Add(provider);
                        Debug.WriteLine($"Found API key for provider: {provider}");
                    }
                    else
                    {
                        Debug.WriteLine($"No API key found for provider: {provider}");
                    }
                }
                
                ExistingApiKeys = existingKeys;
                Debug.WriteLine($"Updated existing API keys list: {string.Join(", ", existingKeys)}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating existing API keys: {ex.Message}");
                ExistingApiKeys = new List<string>();
            }
        }
        #endregion

        #region Filter and Search Commands
        [RelayCommand]
        private async Task FilterModels()
        {
            try
            {
                Debug.WriteLine("Filtering models");
                
                if (Models.Count > 0)
                {
                    Debug.WriteLine("Filtering existing models in memory");
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        ApplyFiltersAndUpdateFilteredModels();
                    });
                    return;
                }
                
                List<AIModel> allModels = await _modelManager.GetAllModelsAsync();
                
                if (allModels == null || allModels.Count == 0)
                {
                    Debug.WriteLine("No models found in database or memory");
                    
                    await MainThread.InvokeOnMainThreadAsync(() => 
                    {
                        HasError = false;
                        ShowNoResults = true;
                        FilteredModels.Clear();
                    });
                    
                    ShowNotification("No models found. Try refreshing to load models.");
                    return;
                }
                
                Debug.WriteLine($"Got {allModels.Count} models from database");
                
                // Use incremental loading for better UX
                await LoadModelsIncrementally(allModels, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error filtering models: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task ToggleFavoritesFilter()
        {
            ShowFavoritesOnly = !ShowFavoritesOnly;
            Debug.WriteLine($"Toggled favorites filter: {ShowFavoritesOnly}");
            
            // Re-apply filters to existing models
            if (Models.Count > 0)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ApplyFiltersAndUpdateFilteredModels();
                });
            }
            else
            {
                await FilterModels();
            }
        }
        #endregion

        #region Refresh and Data Commands
        [RelayCommand]
        private async Task RefreshModels()
        {
            try
            {
                Debug.WriteLine("AIModelsViewModel: RefreshModels command executed");
                
                // Start refresh button animation
                await AnimateRefreshButton();
                
                // Use single entry point for loading
                await LoadModelsAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in RefreshModels: {ex.Message}");
            }
        }

        /// <summary>
        /// Animates the refresh button during loading
        /// </summary>
        private async Task AnimateRefreshButton()
        {
            try
            {
                // Only start animation if not already animating
                if (RefreshButtonRotation != 0)
                    return;
                
                // Start rotation animation while loading
                _ = Task.Run(async () =>
                {
                    while (IsLoading)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            RefreshButtonRotation += 30;
                            if (RefreshButtonRotation >= 360)
                                RefreshButtonRotation = 0;
                        });
                    }
                    
                    // Reset rotation when loading is complete
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        RefreshButtonRotation = 0;
                    });
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in AnimateRefreshButton: {ex.Message}");
            }
        }
        #endregion

        #region Information and Navigation Commands
        [RelayCommand]
        private async Task ShowModelInfo(AIModel model)
        {
            if (model == null) return;

            try
            {
                string info = $"Provider: {model.ProviderName}\n" +
                             $"Model: {model.ModelName}\n" +
                             $"Description: {model.Description}\n\n" +
                             $"Max Tokens: {model.MaxTokens}\n" +
                             $"Context Window: {model.MaxContextWindow} tokens\n" +
                             $"Streaming: {(model.SupportsStreaming ? "Yes" : "No")}\n" +
                             $"Vision: {(model.SupportsVision ? "Yes" : "No")}\n" +
                             $"Code Completion: {(model.SupportsCodeCompletion ? "Yes" : "No")}\n\n" +
                             $"Usage Count: {model.UsageCount}";
                
                await Shell.Current.DisplayAlert(
                    NormalizeModelName(model.ModelName),
                    info,
                    "Close");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing model info: {ex.Message}");
                ShowNotification($"Error: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task GoBack()
        {
            try
            {
                await _navigationService.GoBackAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error navigating back: {ex.Message}");
                
                try
                {
                    await Shell.Current.GoToAsync("..");
                }
                catch
                {
                    await Shell.Current.Navigation.PopAsync();
                }
            }
        }

        [RelayCommand]
        private async Task GoToSettings()
        {
            try
            {
                await _navigationService.NavigateToAsync("//Settings");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GoToSettings: {ex.Message}");
            }
        }
        #endregion

        #region Model Management Commands
        [RelayCommand]
        private async Task AddModel()
        {
            // This should show the API key overlay since adding models requires API keys
            await DisplayApiKeyOverlay();
        }
        #endregion

        #endregion

        /// <summary>
        /// Re-sorts models and updates collections after status changes
        /// </summary>
        private async Task ResortAndUpdateModels()
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    Debug.WriteLine("Starting model resort and animation");
                    
                    // Get current models and sort them by priority
                    var sortedModels = Models
                        .OrderByDescending(m => m.IsSelected)
                        .ThenByDescending(m => m.IsFavorite)
                        .ThenByDescending(m => m.IsDefault)
                        .ThenByDescending(m => m.LastUsed ?? DateTime.MinValue)
                        .ThenBy(m => m.ProviderName, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(m => m.ModelName, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    
                    // Animate models out that need to move
                    var modelsToAnimate = new List<AIModel>();
                    for (int i = 0; i < Models.Count; i++)
                    {
                        var currentModel = Models[i];
                        var newPosition = sortedModels.IndexOf(currentModel);
                        
                        // If model needs to move significantly, animate it out
                        if (Math.Abs(i - newPosition) > 2)
                        {
                            modelsToAnimate.Add(currentModel);
                            currentModel.AnimationOpacity = 0.3;
                            currentModel.AnimationScale = 0.9;
                        }
                    }
                    

                    
                    // Clear and re-add in sorted order
                    Models.Clear();
                    foreach (var model in sortedModels)
                    {
                        Models.Add(model);
                        
                        // Reset animation properties
                        model.AnimationOpacity = 1.0;
                        model.AnimationScale = 1.0;
                    }
                    
                    // Apply filters and update FilteredModels with sorting
                    ApplyFiltersAndUpdateFilteredModels();
                    
                    
                    Debug.WriteLine($"Resorted models: {Models.Count} total, {FilteredModels.Count} filtered");
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error resorting models: {ex.Message}");
            }
        }
    }
}
