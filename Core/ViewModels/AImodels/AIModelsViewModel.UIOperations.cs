using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NexusChat.Core.Models;

namespace NexusChat.Core.ViewModels
{
    public partial class AIModelsViewModel
    {
        #region Filter and Search Operations
        /// <summary>
        /// Filters existing models in collection
        /// </summary>
        private async Task FilterExistingModels()
        {
            var currentModels = Models.ToList();
            await ApplyFiltersAndUpdate(currentModels);
        }

        /// <summary>
        /// Applies filters and updates the collection
        /// </summary>
        private async Task ApplyFiltersAndUpdate(List<AIModel> sourceModels)
        {
            try
            {
                var filteredModels = await Task.Run(() =>
                {
                    var filtered = sourceModels.AsEnumerable();
                    
                    if (ShowFavoritesOnly)
                    {
                        filtered = filtered.Where(m => m.IsFavorite);
                    }
                    
                    if (!string.IsNullOrWhiteSpace(SearchText))
                    {
                        string searchLower = SearchText.ToLowerInvariant();
                        filtered = filtered.Where(m => 
                            (m.ModelName?.ToLowerInvariant().Contains(searchLower) == true) ||
                            (m.ProviderName?.ToLowerInvariant().Contains(searchLower) == true) ||
                            (m.Description?.ToLowerInvariant().Contains(searchLower) == true));
                    }
                    
                    return filtered.ToList();
                });
                
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    // Update both Models and FilteredModels
                    await Models.ReplaceAllAsync(sourceModels);
                    
                    FilteredModels.Clear();
                    foreach (var model in filteredModels)
                    {
                        FilteredModels.Add(model);
                    }
                    
                    ShowNoResults = FilteredModels.Count == 0;
                    HasError = false;
                    
                    Debug.WriteLine($"Filter update: {sourceModels.Count} source -> {Models.Count} models -> {FilteredModels.Count} filtered");
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error applying filters: {ex.Message}");
            }
        }
        #endregion

        #region Animation and Scrolling
        /// <summary>
        /// Triggers animation for a specific model
        /// </summary>
        private async Task TriggerModelAnimation(AIModel model)
        {
            if (model == null)
                return;
                
            try
            {
                lock (_animationLock)
                {
                    if (_isAnAnimationInProgress)
                    {
                        Debug.WriteLine("Animation already in progress, skipping");
                        return;
                    }
                        
                    _isAnAnimationInProgress = true;
                }
                
                Debug.WriteLine($"Starting animation for model: {model.ModelName}");
                
                // Since the model now implements INotifyPropertyChanged, 
                // just trigger the scroll and let property changes handle UI updates
                await MainThread.InvokeOnMainThreadAsync(async () => 
                {
                    ScrollToModelRequested?.Invoke(model);
                    
                    // Brief delay for visual feedback
                    await Task.Delay(150);
                });
                
                Debug.WriteLine($"Animation completed for model: {model.ModelName}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error triggering animation: {ex.Message}");
            }
            finally
            {
                _isAnAnimationInProgress = false;
            }
        }

        /// <summary>
        /// Finds and scrolls to the currently selected model
        /// </summary>
        private async Task FindAndScrollToSelected()
        {
            try
            {
                var current = _modelManager.CurrentModel;
                if (current != null)
                {
                    var foundModel = Models.FirstOrDefault(m => 
                        m.ProviderName.Equals(current.ProviderName, StringComparison.OrdinalIgnoreCase) &&
                        m.ModelName.Equals(current.ModelName, StringComparison.OrdinalIgnoreCase));
                        
                    if (foundModel != null)
                    {
                        SelectedModel = foundModel;
                        ScrollToModel = foundModel;
                        
                        ScrollToModelRequested?.Invoke(foundModel);
                        await TriggerModelAnimation(foundModel);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error finding selected model: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates current model selection in UI
        /// </summary>
        private async Task UpdateCurrentSelection()
        {
            try
            {
                var currentModel = _modelManager.CurrentModel;
                if (currentModel != null)
                {
                    var modelInCollection = Models.FirstOrDefault(m => 
                        m.ProviderName.Equals(currentModel.ProviderName, StringComparison.OrdinalIgnoreCase) &&
                        m.ModelName.Equals(currentModel.ModelName, StringComparison.OrdinalIgnoreCase));
                    
                    if (modelInCollection != null)
                    {
                        foreach (var model in Models)
                        {
                            model.IsSelected = model == modelInCollection;
                        }
                        
                        SelectedModel = modelInCollection;
                        await TriggerModelAnimation(modelInCollection);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating selection: {ex.Message}");
            }
        }
        #endregion

        #region Notification System
        /// <summary>
        /// Shows a notification message to the user
        /// </summary>
        private async void ShowNotification(string message)
        {
            try
            {
                LastActionResult = message;
                ShowActionResult = true;
                NotificationOpacity = 1.0;
                
                await Task.Delay(3000);
                
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    NotificationOpacity = 0.0;
                    ShowActionResult = false;
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing notification: {ex.Message}");
            }
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// Normalizes model name by removing common provider prefixes
        /// </summary>
        private string NormalizeModelName(string modelName)
        {
            if (string.IsNullOrEmpty(modelName))
                return modelName;
                
            string normalized = modelName;
            
            if (normalized.StartsWith("openai/", StringComparison.OrdinalIgnoreCase))
                normalized = normalized.Substring(7);
            if (normalized.StartsWith("anthropic/", StringComparison.OrdinalIgnoreCase))
                normalized = normalized.Substring(10);
            if (normalized.StartsWith("google/", StringComparison.OrdinalIgnoreCase))
                normalized = normalized.Substring(7);
                
            return normalized;
        }
        #endregion
    }

    public class AIModelComparer : IEqualityComparer<AIModel>
    {
        public bool Equals(AIModel x, AIModel y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            
            return string.Equals(x.ProviderName, y.ProviderName, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(x.ModelName, y.ModelName, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(AIModel obj)
        {
            if (obj == null) return 0;
            
            return HashCode.Combine(
                obj.ProviderName?.ToLowerInvariant(),
                obj.ModelName?.ToLowerInvariant()
            );
        }
    }
}
