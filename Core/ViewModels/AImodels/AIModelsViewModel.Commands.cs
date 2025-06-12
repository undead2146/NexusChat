using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
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
                
                foreach (var m in Models)
                {
                    m.IsSelected = m == model;
                }
                
                bool success = await _modelManager.SetCurrentModelAsync(model);
                if (success)
                {
                    ShowNotification($"Now using {NormalizeModelName(model.ModelName)}");
                    SelectedModel = model;
                    
                    _ = _modelManager.RecordModelUsageAsync(model.ProviderName, model.ModelName);
                    
                    ScrollToModelRequested?.Invoke(model);
                }
                else
                {
                    foreach (var m in Models)
                    {
                        m.IsSelected = m == SelectedModel;
                    }
                    model.IsSelected = false;
                    
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
                
                foreach (var m in Models.Where(m => m.ProviderName.Equals(model.ProviderName, StringComparison.OrdinalIgnoreCase)))
                {
                    m.IsDefault = m == model;
                }
                
                bool success = await _modelManager.SetDefaultModelAsync(model.ProviderName, model.ModelName);
                
                if (success)
                {
                    ShowNotification($"{NormalizeModelName(model.ModelName)} set as default for {model.ProviderName}");
                }
                else
                {
                    foreach (var m in Models.Where(m => m.ProviderName.Equals(model.ProviderName, StringComparison.OrdinalIgnoreCase)))
                    {
                        m.IsDefault = false;
                    }
                    model.IsDefault = false;
                    
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
                
                model.IsFavorite = !model.IsFavorite;
                
                string notificationText = model.IsFavorite ? 
                    $"{NormalizeModelName(model.ModelName)} added to favorites" : 
                    $"{NormalizeModelName(model.ModelName)} removed from favorites";
                ShowNotification(notificationText);
                
                bool success = await _modelManager.SetFavoriteStatusAsync(
                    model.ProviderName, model.ModelName, model.IsFavorite);
                
                if (success)
                {
                    Debug.WriteLine($"Successfully updated favorite status to {model.IsFavorite}");
                    
                    if (ShowFavoritesOnly)
                    {
                        await FilterModels();
                    }
                }
                else
                {
                    model.IsFavorite = !model.IsFavorite;
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
        #endregion

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
                        
                        // Refresh models to show new provider's models
                        _lastModelRefresh = DateTime.MinValue;
                        await LoadModelsAsync();
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
                    $"Are you sure you want to remove the {provider} API key?\n\nThis will hide all {provider} models from the list.",
                    "Remove",
                    "Cancel");

                if (confirm)
                {
                    bool success = await _apiKeyManager.DeleteProviderApiKeyAsync(provider);
                    if (success)
                    {
                        ShowNotification($"{provider} API key removed successfully");
                        
                        // Refresh existing keys display
                        await UpdateExistingApiKeys();
                        
                        // Refresh models to hide removed provider's models
                        _lastModelRefresh = DateTime.MinValue;
                        await LoadModelsAsync();
                        
                        // If no API keys remain, show empty state
                        if (ExistingApiKeys.Count == 0)
                        {
                            ShowApiKeyOverlay = false;
                        }
                    }
                    else
                    {
                        ShowNotification($"Failed to remove {provider} API key");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error removing API key: {ex.Message}");
                ShowNotification($"Error removing API key: {ex.Message}");
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
                        ApplyFiltersToCurrentModels();
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
                    });
                    
                    ShowNotification("No models found. Try refreshing to load models.");
                    return;
                }
                
                Debug.WriteLine($"Got {allModels.Count} models from database");
                
                await ApplyFiltersAndUpdate(allModels);
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
            
            await FilterModels();
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
                        await Task.Delay(50);
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
    }
}
