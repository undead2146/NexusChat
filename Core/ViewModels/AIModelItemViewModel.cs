using System;
using CommunityToolkit.Mvvm.ComponentModel;
using NexusChat.Core.Models;
using NexusChat.Helpers;

namespace NexusChat.Core.ViewModels
{
    /// <summary>
    /// View model for displaying an AI model item in lists and selectors
    /// </summary>
    public partial class AIModelItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private int _id;
        
        [ObservableProperty]
        private string _modelName;
        
        [ObservableProperty]
        private string _providerName;
        
        [ObservableProperty]
        private string _description;
        
        [ObservableProperty]
        private bool _isDefault;
        
        [ObservableProperty]
        private bool _isFavourite;
        
        [ObservableProperty]
        private bool _isSelected;
        
        [ObservableProperty]
        private bool _isAvailable;
        
        [ObservableProperty]
        private int _maxTokens;
        
        [ObservableProperty]
        private int _maxContextWindow;
        
        [ObservableProperty]
        private bool _supportsStreaming;
        
        [ObservableProperty]
        private float _defaultTemperature;
        
        /// <summary>
        /// Gets the display name for the model (combines provider and model name)
        /// </summary>
        public string DisplayName => $"{ProviderName}: {ModelName}";
        
        /// <summary>
        /// Gets a shorter display name for limited space
        /// </summary>
        public string ShortDisplayName 
        { 
            get 
            {
                // Some model names are very long, so we trim them for display
                if (ModelName.Length > 15)
                {
                    return $"{ProviderName}: {ModelName.Substring(0, 12)}...";
                }
                return DisplayName;
            }
        }
        
        /// <summary>
        /// Gets a subtitle for additional display information
        /// </summary>
        public string Subtitle => $"{MaxTokens} tokens | Temp: {DefaultTemperature:F1}";
        
        /// <summary>
        /// Gets the validation state (true if model is valid for use)
        /// </summary>
        public bool IsValid => IsAvailable && !string.IsNullOrEmpty(ModelName) && !string.IsNullOrEmpty(ProviderName);
        
        /// <summary>
        /// Default constructor for XAML
        /// </summary>
        public AIModelItemViewModel()
        {
            IsAvailable = true;
            DefaultTemperature = 0.7f;
            MaxTokens = 4096;
            MaxContextWindow = 8192;
            SupportsStreaming = true;
        }
        
        /// <summary>
        /// Creates a new view model from an AIModel
        /// </summary>
        /// <param name="model">The model to create the view model from</param>
        public AIModelItemViewModel(AIModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
                
            Id = model.Id;
            ModelName = model.ModelName;
            ProviderName = model.ProviderName;
            Description = model.Description;
            IsAvailable = model.IsAvailable;
            MaxTokens = model.MaxTokens;
            MaxContextWindow = model.MaxContextWindow;
            SupportsStreaming = model.SupportsStreaming;
            DefaultTemperature = model.DefaultTemperature;
            IsDefault = false;
            IsFavourite = model.IsFavourite; // Initialize from model's favorite status
            IsSelected = false;
        }
        
        /// <summary>
        /// Updates this view model from an AIModel
        /// </summary>
        /// <param name="model">The model to update from</param>
        public void UpdateFromModel(AIModel model)
        {
            if (model == null)
                return;
                
            Id = model.Id;
            ModelName = model.ModelName;
            ProviderName = model.ProviderName;
            Description = model.Description;
            IsAvailable = model.IsAvailable;
            MaxTokens = model.MaxTokens;
            MaxContextWindow = model.MaxContextWindow;
            SupportsStreaming = model.SupportsStreaming;
            DefaultTemperature = model.DefaultTemperature;
            IsFavourite = model.IsFavourite; // Update favorite status from model
            // Don't update IsDefault and IsSelected - these are UI state
        }
        
        /// <summary>
        /// Converts this view model back to an AIModel
        /// </summary>
        public AIModel ToModel()
        {
            return new AIModel
            {
                Id = Id,
                ModelName = ModelName,
                ProviderName = ProviderName,
                Description = Description,
                IsAvailable = IsAvailable,
                MaxTokens = MaxTokens,
                MaxContextWindow = MaxContextWindow,
                SupportsStreaming = SupportsStreaming,
                DefaultTemperature = DefaultTemperature,
                IsFavourite = IsFavourite // Include favorite status when converting back to model
            };
        }
        
        /// <summary>
        /// Updates common notification properties when any property changes
        /// </summary>
        partial void OnPropertyChanged(string propertyName);
        
        partial void OnPropertyChanged(string propertyName)
        {
            // Update dependent properties when base properties change
            switch (propertyName)
            {
                case nameof(ModelName):
                case nameof(ProviderName):
                    OnPropertyChanged(nameof(DisplayName));
                    OnPropertyChanged(nameof(ShortDisplayName));
                    OnPropertyChanged(nameof(IsValid));
                    break;
                    
                case nameof(MaxTokens):
                case nameof(DefaultTemperature):
                    OnPropertyChanged(nameof(Subtitle));
                    break;
                    
                case nameof(IsAvailable):
                    OnPropertyChanged(nameof(IsValid));
                    break;

                case nameof(IsFavourite):
                    RequestAnimateFavoriteStar();
                    break;
            }
        }
        
        /// <summary>
        /// Sends a request to animate the default star for this model
        /// </summary>
        public void RequestAnimateDefaultStar()
        {
            if (IsDefault && Id > 0)
            {
                MessagingHelper.RequestAnimateDefaultStar(this, Id);
            }
        }

        /// <summary>
        /// Sends a request to animate the favorite star for this model
        /// </summary>
        public void RequestAnimateFavoriteStar()
        {
            if (IsFavourite && Id > 0)
            {
                MessagingHelper.RequestAnimateFavouriteStar(Id);
            }
        }
    }
}
