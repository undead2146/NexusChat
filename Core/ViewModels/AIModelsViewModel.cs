using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using NexusChat.Core.Models;
using NexusChat.Services.Interfaces;
using NexusChat.Services.AIProviders;
using NexusChat.Data.Interfaces;

namespace NexusChat.Core.ViewModels
{
    /// <summary>
    /// View model for AI models page
    /// </summary>
    public partial class AIModelsViewModel : BaseViewModel
    {
        private readonly IAIModelRepository _modelRepository;
        private readonly IAIProviderFactory _providerFactory;
        private readonly IAIModelManager _modelManager;
        private readonly IApiKeyManager _apiKeyManager;
        private readonly INavigationService _navigationService;

        [ObservableProperty]
        private ObservableCollection<AIModelGroupViewModel> _modelGroups;

        [ObservableProperty]
        private AIModelItemViewModel _selectedModel;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isRefreshing;

        [ObservableProperty]
        private bool _hasError;

        [ObservableProperty]
        private string _errorMessage;

        [ObservableProperty]
        private string _apiKey;

        [ObservableProperty]
        private string _selectedProvider;

        [ObservableProperty]
        private bool _isEditingApiKey;

        [ObservableProperty]
        private bool _showFavoritesOnly;

        /// <summary>
        /// Creates a new instance of AIModelsViewModel
        /// </summary>
        public AIModelsViewModel(
            IAIModelRepository modelRepository,
            IAIProviderFactory providerFactory,
            IAIModelManager modelManager,
            IApiKeyManager apiKeyManager,
            INavigationService navigationService)
        {
            _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
            _providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
            _modelManager = modelManager ?? throw new ArgumentNullException(nameof(modelManager));
            _apiKeyManager = apiKeyManager ?? throw new ArgumentNullException(nameof(apiKeyManager));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

            ModelGroups = new ObservableCollection<AIModelGroupViewModel>();
            Title = "AI Models";
        }

        /// <summary>
        /// Initializes the view model
        /// </summary>
        public async Task InitializeAsync()
        {
            await LoadModelsAsync();
        }

        /// <summary>
        /// Called when the page appears
        /// </summary>
        [RelayCommand]
        private async Task PageAppearing()
        {
            await InitializeAsync();
        }

        /// <summary>
        /// Loads all models from the repository
        /// </summary>
        private async Task LoadModelsAsync()
        {
            try
            {
                IsLoading = true;

                // Get only models from providers with API keys
                var models = await _modelRepository.GetActiveModelsAsync();

                await UpdateModelItemsAsync(models);

                // Ensure current model is selected in UI
                await UpdateCurrentModelSelectionAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading models: {ex.Message}");
                HasError = true;
                ErrorMessage = $"Error loading models: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Updates the model items in the UI
        /// </summary>
        private async Task UpdateModelItemsAsync(List<AIModel> models)
        {
            try
            {
                // Group models by provider
                var groupedModels = models
                    .GroupBy(m => m.ProviderName)
                    .OrderBy(g => g.Key != "OpenRouter") // OpenRouter first, then others
                    .ThenBy(g => g.Key);

                var modelGroups = new ObservableCollection<AIModelGroupViewModel>();

                foreach (var group in groupedModels)
                {
                    var groupVm = new AIModelGroupViewModel
                    {
                        ProviderName = group.Key,
                        Items = new ObservableCollection<AIModelItemViewModel>()
                    };

                    foreach (var model in group.OrderBy(m => !m.IsFavorite).ThenBy(m => m.ModelName))
                    {
                        if (ShowFavoritesOnly && !model.IsFavorite)
                            continue;

                        groupVm.Items.Add(new AIModelItemViewModel
                        {
                            Id = model.Id,
                            ModelName = model.ModelName,
                            ProviderName = model.ProviderName,
                            Description = model.Description,
                            IsFavorite = model.IsFavorite,
                            IsSelected = model.IsSelected,
                            IsDefault = model.IsDefault,
                            SupportsStreaming = model.SupportsStreaming,
                            ToggleFavoriteCommand = new AsyncRelayCommand<AIModelItemViewModel>(ToggleFavorite),
                            SelectModelCommand = new AsyncRelayCommand<AIModelItemViewModel>(SelectModel)
                        });
                    }

                    if (groupVm.Items.Any())
                        modelGroups.Add(groupVm);
                }

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ModelGroups.Clear();
                    foreach (var group in modelGroups)
                    {
                        ModelGroups.Add(group);
                    }
                });

                Debug.WriteLine($"Updated model items: {models.Count} models in {ModelGroups.Count} groups");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating model items: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the selection state to match current model
        /// </summary>
        private async Task UpdateCurrentModelSelectionAsync()
        {
            try
            {
                var currentModel = _modelManager.CurrentModel;
                if (currentModel == null)
                    return;

                Debug.WriteLine($"Updating selection for current model: {currentModel.ProviderName}/{currentModel.ModelName}");

                foreach (var group in ModelGroups)
                {
                    foreach (var item in group.Items)
                    {
                        bool isSelected = string.Equals(item.ProviderName, currentModel.ProviderName, StringComparison.OrdinalIgnoreCase) &&
                                         string.Equals(item.ModelName, currentModel.ModelName, StringComparison.OrdinalIgnoreCase);

                        item.IsSelected = isSelected;

                        if (isSelected)
                        {
                            SelectedModel = item;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating current model selection: {ex.Message}");
            }
        }

        /// <summary>
        /// Refreshes the models from providers
        /// </summary>
        [RelayCommand]
        public async Task RefreshModelsFromProvidersAsync()
        {
            if (IsRefreshing)
                return;

            try
            {
                IsRefreshing = true;
                IsLoading = true;

                Debug.WriteLine("Refreshing models from providers");

                // Clear the model cache to force fresh retrieval
                _providerFactory.ClearModelCache();

                // Get all models from providers with valid API keys
                var models = await _providerFactory.GetAllModelsAsync();
                Debug.WriteLine($"Got {models.Count} models from providers with valid API keys");

                // Update the view models
                await UpdateModelItemsAsync(models);

                Debug.WriteLine("Models refreshed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error refreshing models: {ex.Message}");
            }
            finally
            {
                IsRefreshing = false;
                IsLoading = false;
            }
        }

        /// <summary>
        /// Toggles the favorite status of a model
        /// </summary>
        private async Task ToggleFavorite(AIModelItemViewModel model)
        {
            if (model == null)
                return;

            try
            {
                bool newStatus = !model.IsFavorite;
                bool success = await _modelManager.SetFavoriteStatusAsync(model.ProviderName, model.ModelName, newStatus);

                if (success)
                {
                    model.IsFavorite = newStatus;

                    if (ShowFavoritesOnly && !newStatus)
                    {
                        // If showing favorites only and model is no longer favorite, remove it
                        await RefreshModelsFromProvidersAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error toggling favorite status: {ex.Message}");
            }
        }

        /// <summary>
        /// Selects a model as the current model
        /// </summary>
        private async Task SelectModel(AIModelItemViewModel model)
        {
            if (model == null)
                return;

            try
            {
                IsLoading = true;

                Debug.WriteLine($"Selecting model: {model.ProviderName}/{model.ModelName}");

                // Find the actual repository model
                var repoModel = await _modelRepository.GetModelByNameAsync(
                    model.ProviderName, model.ModelName);

                if (repoModel != null)
                {
                    bool success = await _modelManager.SetCurrentModelAsync(repoModel);

                    if (success)
                    {
                        // Update selection status in UI
                        foreach (var group in ModelGroups)
                        {
                            foreach (var item in group.Items)
                            {
                                item.IsSelected = (item == model);
                            }
                        }

                        SelectedModel = model;

                        // Navigate back
                        await Shell.Current.GoToAsync("..");
                    }
                }
                else
                {
                    Debug.WriteLine($"Model not found in repository: {model.ProviderName}/{model.ModelName}");

                    // Create a minimum model object
                    var minModel = new AIModel
                    {
                        ProviderName = model.ProviderName,
                        ModelName = model.ModelName,
                        DisplayName = model.ModelName,
                        Description = model.Description,
                        IsSelected = true
                    };

                    bool success = await _modelManager.SetCurrentModelAsync(minModel);

                    if (success)
                    {
                        // Navigate back
                        await Shell.Current.GoToAsync("..");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error selecting model: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", $"Could not select model: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Shows the API key management section
        /// </summary>
        [RelayCommand]
        private void ShowApiKeySection()
        {
            // Get the available providers for the dropdown
            SelectedProvider = _providerFactory.GetAvailableProviders().FirstOrDefault();
            IsEditingApiKey = true;
        }

        /// <summary>
        /// Hides the API key management section
        /// </summary>
        [RelayCommand]
        private void HideApiKeySection()
        {
            IsEditingApiKey = false;
            ApiKey = string.Empty;
        }

        /// <summary>
        /// Saves an API key
        /// </summary>
        [RelayCommand]
        private async Task SaveApiKey()
        {
            if (string.IsNullOrEmpty(SelectedProvider) || string.IsNullOrEmpty(ApiKey))
            {
                await Shell.Current.DisplayAlert("Error", "Please enter an API key and select a provider", "OK");
                return;
            }

            try
            {
                IsLoading = true;

                bool success = await _apiKeyManager.SaveApiKeyAsync(SelectedProvider, ApiKey);

                if (success)
                {
                    await Shell.Current.DisplayAlert("Success", $"API key for {SelectedProvider} saved successfully", "OK");
                    ApiKey = string.Empty;
                    IsEditingApiKey = false;

                    // Refresh models to include the new provider's models
                    await RefreshModelsFromProvidersAsync();
                }
                else
                {
                    await Shell.Current.DisplayAlert("Error", $"Could not save API key for {SelectedProvider}", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving API key: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", $"Could not save API key: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Toggles showing favorites only
        /// </summary>
        [RelayCommand]
        private async Task ToggleFavoritesOnly()
        {
            ShowFavoritesOnly = !ShowFavoritesOnly;
            await RefreshModelsFromProvidersAsync();
        }
    }
}
