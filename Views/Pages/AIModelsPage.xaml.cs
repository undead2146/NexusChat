using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using NexusChat.Core.Models;
using NexusChat.Core.ViewModels;
using NexusChat.Services.Interfaces;

namespace NexusChat.Views.Pages
{
    /// <summary>
    /// Page for browsing and selecting AI models
    /// </summary>
    public partial class AIModelsPage : ContentPage
    {
        private readonly AIModelsViewModel _viewModel;
        private readonly IApiKeyManager _apiKeyManager;

        /// <summary>
        /// Creates a new AIModelsPage
        /// </summary>
        public AIModelsPage(AIModelsViewModel viewModel, IApiKeyManager apiKeyManager)
        {
            InitializeComponent();
            
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _apiKeyManager = apiKeyManager ?? throw new ArgumentNullException(nameof(apiKeyManager));
            
            BindingContext = _viewModel;
            
            // Register for property changes to handle animations
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            
            // Register for scroll requests
            _viewModel.ScrollToModelRequested += OnScrollToModelRequested;
            
            // Subscribe to API key changes
            _apiKeyManager.ApiKeyChanged += OnApiKeyChanged;
        }

        /// <summary>
        /// Called when the page appears
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            // Trigger the page appearing command
            _viewModel.OnAppearing();
        }
        
        /// <summary>
        /// Called when the page disappears
        /// </summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            
            // Unsubscribe from events
            if (_apiKeyManager != null)
            {
                _apiKeyManager.ApiKeyChanged -= OnApiKeyChanged;
            }
        }
        
        /// <summary>
        /// Handles changes to ViewModel properties
        /// </summary>
        private async void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Look for animation requests
            if (e.PropertyName?.StartsWith("Animate_") == true)
            {
                string[] parts = e.PropertyName.Substring(8).Split('_');
                if (parts.Length == 2)
                {
                    string providerName = parts[0];
                    string modelName = parts[1];
                    
                    await FindAndAnimateModelCard(providerName, modelName);
                }
            }
        }
        
        /// <summary>
        /// Finds and animates a model card based on provider and model name
        /// </summary>
        private async Task FindAndAnimateModelCard(string providerName, string modelName)
        {
            try
            {
                Debug.WriteLine($"Animating model: {providerName}/{modelName}");
                
                // Find the specific model in the collection view
                var model = _viewModel.Models.FirstOrDefault(m => 
                    m.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase) && 
                    m.ModelName.Equals(modelName, StringComparison.OrdinalIgnoreCase));

                if (model == null)
                {
                    Debug.WriteLine($"Model not found for animation: {providerName}/{modelName}");
                    return;
                }
                
                // Find the visual element for this model
                foreach (var item in ModelList.GetVisualTreeDescendants())
                {
                    if (item is Border card && card.BindingContext is AIModel cardModel && 
                        cardModel.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase) && 
                        cardModel.ModelName.Equals(modelName, StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.WriteLine($"Found card for model: {providerName}/{modelName}");
                        
                        // Animate the card
                        await AnimateModelCard(card);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error animating model card: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Animates a model card with highlight effect
        /// </summary>
        private async Task AnimateModelCard(Border card)
        {
            try
            {
                // Save original scale and opacity
                double originalScale = card.Scale;
                double originalOpacity = card.Opacity;
                
                // Highlight animation sequence
                await card.ScaleTo(1.03, 150, Easing.SpringOut);
                await Task.WhenAll(
                    card.ScaleTo(originalScale, 150, Easing.SpringIn),
                    card.FadeTo(0.7, 100),
                    card.FadeTo(originalOpacity, 200)
                );
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during card animation: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Scrolls to the specified model
        /// </summary>
        private void OnScrollToModelRequested(AIModel model)
        {
            try
            {
                ModelList.ScrollTo(model, position: ScrollToPosition.MakeVisible, animate: true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error scrolling to model: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Opens the API key dialog
        /// </summary>
        private async void OnAddApiKeyClicked(object sender, EventArgs e)
        {
            try
            {
                // Show action sheet to select provider
                string result = await DisplayActionSheet(
                    "Select Provider", 
                    "Cancel", 
                    null, 
                    "Groq",
                    "OpenRouter",
                    "Azure OpenAI");
                
                if (result == "Cancel" || string.IsNullOrEmpty(result))
                    return;
                    
                // Use the command
                await _viewModel.SaveApiKeyCommand.ExecuteAsync(result);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error adding API key: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handles API key changes
        /// </summary>
        private async void OnApiKeyChanged(object sender, string providerName)
        {
            Debug.WriteLine($"API key changed for provider: {providerName}");
            
            // Wait briefly to ensure key is saved
            await Task.Delay(500);
            
            // Refresh models when an API key is added or changed
            await _viewModel.RefreshModelsFromProvidersAsync();
        }
    }
}



