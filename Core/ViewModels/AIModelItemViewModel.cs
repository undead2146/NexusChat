using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using NexusChat.Core.Models;

namespace NexusChat.Core.ViewModels
{
    /// <summary>
    /// View model for individual AI model items in lists
    /// </summary>
    public partial class AIModelItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private AIModel _model;

        [ObservableProperty]
        private bool _isAnimating;

        [ObservableProperty]
        private double _scale = 1.0;

        [ObservableProperty] 
        private double _opacity = 1.0;
        
        [ObservableProperty]
        private double _translateX = 0;

        /// <summary>
        /// Command to toggle favorite status
        /// </summary>
        public ICommand ToggleFavoriteCommand { get; }

        /// <summary>
        /// Command to select this model
        /// </summary>
        public ICommand SelectModelCommand { get; }

        /// <summary>
        /// Command to show model details
        /// </summary>
        public ICommand ShowDetailsCommand { get; }

        /// <summary>
        /// Command to set as default model
        /// </summary>
        public ICommand SetAsDefaultCommand { get; }

        /// <summary>
        /// Creates a new instance of AIModelItemViewModel
        /// </summary>
        public AIModelItemViewModel(
            AIModel model,
            ICommand toggleFavoriteCommand,
            ICommand selectModelCommand,
            ICommand showDetailsCommand,
            ICommand setAsDefaultCommand)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            ToggleFavoriteCommand = toggleFavoriteCommand;
            SelectModelCommand = selectModelCommand; 
            ShowDetailsCommand = showDetailsCommand;
            SetAsDefaultCommand = setAsDefaultCommand;
        }

        /// <summary>
        /// Updates the model without losing animation state
        /// </summary>
        public void UpdateModel(AIModel model)
        {
            if (model == null)
                return;
                
            // Keep animation state but update model properties
            Model.IsFavorite = model.IsFavorite;
            Model.IsSelected = model.IsSelected;
            Model.IsDefault = model.IsDefault;
            Model.IsAvailable = model.IsAvailable;
            
            // Notify UI
            OnPropertyChanged(nameof(Model));
        }

        /// <summary>
        /// Animates the item with a pulse effect
        /// </summary>
        public async void AnimatePulse()
        {
            try
            {
                IsAnimating = true;
                
                // Animation sequence
                Scale = 0.95;
                await Task.Delay(100);
                Scale = 1.05;
                await Task.Delay(100);
                Scale = 1.0;
                
                IsAnimating = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in AnimatePulse: {ex.Message}");
                Scale = 1.0;
                IsAnimating = false;
            }
        }

        /// <summary>
        /// Animates favorite toggle
        /// </summary>
        public async void AnimateFavoriteToggle()
        {
            try
            {
                IsAnimating = true;
                
                // Animation for favorite toggling
                if (Model.IsFavorite)
                {
                    // Pulse with slight rotation
                    Scale = 1.1;
                    await Task.Delay(100);
                    Scale = 0.9;
                    await Task.Delay(50);
                    Scale = 1.05;
                    await Task.Delay(50);
                    Scale = 1.0;
                }
                else
                {
                    // Simple pulse out
                    Scale = 0.9;
                    await Task.Delay(100);
                    Scale = 1.0;
                }
                
                IsAnimating = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in AnimateFavoriteToggle: {ex.Message}");
                Scale = 1.0; 
                IsAnimating = false;
            }
        }

        /// <summary>
        /// Animates selection change
        /// </summary>
        public async void AnimateSelection()
        {
            try
            {
                IsAnimating = true;
                
                if (Model.IsSelected)
                {
                    // More pronounced animation for selection
                    Scale = 1.15;
                    await Task.Delay(100);
                    Scale = 0.95;
                    await Task.Delay(50);
                    Scale = 1.05;
                    await Task.Delay(50);
                    Scale = 1.0;
                }
                else
                {
                    // Simple scale down
                    Scale = 0.95;
                    await Task.Delay(100);
                    Scale = 1.0;
                }
                
                IsAnimating = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in AnimateSelection: {ex.Message}");
                Scale = 1.0;
                IsAnimating = false;
            }
        }
    }
}
