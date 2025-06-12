using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexusChat.Services.Interfaces;

namespace NexusChat.Core.ViewModels
{
    /// <summary>
    /// View model for the ChatHeader control
    /// </summary>
    public partial class ChatHeaderViewModel : ObservableObject
    {
        private readonly IAIModelManager _modelManager;
        private readonly INavigationService _navigationService;

        [ObservableProperty]
        private string _title;

        [ObservableProperty]
        private string _currentModelName;

        [ObservableProperty]
        private bool _showModelSwitcher = true;

        [ObservableProperty]
        private bool _showOptionsButton = true;

        [ObservableProperty]
        private bool _isMenuOpen;

        /// <summary>
        /// Creates a new instance of ChatHeaderViewModel
        /// </summary>
        /// <param name="modelManager">Model preference manager for AI model information</param>
        /// <param name="navigationService">Navigation service for page navigation</param>
        public ChatHeaderViewModel(
            IAIModelManager modelManager,
            INavigationService navigationService)
        {
            _modelManager = modelManager ?? throw new ArgumentNullException(nameof(modelManager));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

            Title = "NexusChat";
            UpdateCurrentModelName();
        }

        /// <summary>
        /// Updates the current model name
        /// </summary>
        public void UpdateCurrentModelName()
        {
            var model = _modelManager.CurrentModel;
            if (model != null)
            {
                CurrentModelName = $"{model.ProviderName} {model.ModelName}";
            }
            else
            {
                CurrentModelName = "Default AI Model";
            }
        }

        /// <summary>
        /// Navigates to the models selection page
        /// </summary>
        [RelayCommand]
        private async Task SwitchModel()
        {
            Debug.WriteLine("SwitchModel command executing");
            try
            {
                // Use the navigation service instead of Shell absolute routing
                await _navigationService.NavigateToAsync("models");
                Debug.WriteLine("Navigation to AI models page completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SwitchModel: {ex.Message}");
                
                // Try alternative navigation as a fallback
                try
                {
                    // Use a relative route as a second fallback
                    await Shell.Current.GoToAsync("AIModelsPage");
                }
                catch (Exception navEx)
                {
                    Debug.WriteLine($"Alternative navigation also failed: {navEx.Message}");
                }
            }
        }

        /// <summary>
        /// Executes when the menu button is clicked
        /// </summary>
        [RelayCommand]
        private void ToggleMenu()
        {
            IsMenuOpen = !IsMenuOpen;
        }

        /// <summary>
        /// Shows options menu
        /// </summary>
        [RelayCommand]
        private async Task ShowOptions()
        {
            string action = await Shell.Current.DisplayActionSheet(
                "Options",
                "Cancel",
                null,
                "Switch Model",
                "Settings",
                "About");

            switch (action)
            {
                case "Switch Model":
                    await SwitchModel();
                    break;
                case "Settings":
                    await _navigationService.NavigateToAsync("settings");
                    break;
                case "About":
                    await Shell.Current.DisplayAlert(
                        "About NexusChat",
                        "NexusChat v0.5.0\nA multi-model AI chat application\n© 2024",
                        "OK");
                    break;
            }
        }
    }
}
