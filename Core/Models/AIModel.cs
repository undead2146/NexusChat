using System;
using SQLite;
using CommunityToolkit.Mvvm.ComponentModel;
using SQLiteNetExtensions.Attributes;

namespace NexusChat.Core.Models
{
    /// <summary>
    /// Represents an AI model with its metadata and capabilities
    /// </summary>
    [Table("AIModels")]
    public partial class AIModel : ObservableObject
    {
        /// <summary>
        /// Gets or sets the ID of the model
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        
        /// <summary>
        /// Gets or sets the input cost per 1K tokens
        /// </summary>
        public decimal? InputCostPer1K { get; set; }
        
        /// <summary>
        /// Gets or sets the output cost per 1K tokens
        /// </summary>
        public decimal? OutputCostPer1K { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the model
        /// </summary>
        [ObservableProperty]
        [property: MaxLength(100)]
        private string _modelName = string.Empty;
        
        /// <summary>
        /// Gets or sets the provider name
        /// </summary>
        [ObservableProperty]
        [property: MaxLength(50)]
        private string _providerName = string.Empty;
        
        /// <summary>
        /// Gets or sets the model description
        /// </summary>
        [ObservableProperty]
        [property: MaxLength(500)]
        private string _description = string.Empty;
        
        /// <summary>
        /// Gets or sets the maximum tokens
        /// </summary>
        [ObservableProperty]
        private int _maxTokens;
        
        /// <summary>
        /// Gets or sets the maximum context window
        /// </summary>
        [ObservableProperty]
        private int _maxContextWindow;
        
        /// <summary>
        /// Gets or sets whether streaming is supported
        /// </summary>
        [ObservableProperty]
        private bool _supportsStreaming;
        
        /// <summary>
        /// Gets or sets whether vision capabilities are supported
        /// </summary>
        [ObservableProperty]
        private bool _supportsVision;
        
        /// <summary>
        /// Gets or sets whether code completion is supported
        /// </summary>
        [ObservableProperty]
        private bool _supportsCodeCompletion;
        
        /// <summary>
        /// Gets or sets whether the model is available
        /// </summary>
        [ObservableProperty]
        private bool _isAvailable;
        
        /// <summary>
        /// Gets or sets the API key variable name
        /// </summary>
        [MaxLength(100)]
        public string ApiKeyVariable { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the display name
        /// </summary>
        [ObservableProperty]
        [property: MaxLength(100)]
        private string _displayName = string.Empty;
        
        /// <summary>
        /// Gets or sets the model version
        /// </summary>
        [MaxLength(50)]
        public string Version { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the default temperature
        /// </summary>
        [ObservableProperty]
        private float _defaultTemperature = 0.7f;
        
        /// <summary>
        /// Gets or sets whether the model is currently selected
        /// </summary>
        [ObservableProperty]
        private bool _isSelected;

        /// <summary>
        /// Gets or sets whether the model is a favorite
        /// </summary>
        [ObservableProperty]
        private bool _isFavorite;
        
        /// <summary>
        /// Gets or sets whether the model is the default for its provider
        /// </summary>
        [ObservableProperty]
        private bool _isDefault;
        
        /// <summary>
        /// Gets or sets the usage count
        /// </summary>
        [ObservableProperty]
        private int _usageCount;
        
        /// <summary>
        /// Gets or sets when the model was last used
        /// </summary>
        [ObservableProperty]
        private DateTime? _lastUsed;
        
        /// <summary>
        /// Gets or sets the model status
        /// </summary>
        [ObservableProperty]
        [property: Ignore]
        private ModelStatus _status = ModelStatus.Unknown;
        
        /// <summary>
        /// Database-persistent status field
        /// </summary>
        public int StatusValue 
        { 
            get => (int)Status;
            set => Status = (ModelStatus)value;
        }
        
        /// <summary>
        /// Gets or sets when the model was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Gets or sets when the model was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Gets or sets the display status (legacy field)
        /// </summary>
        [Obsolete("Use Status property instead")]
        public string DisplayStatus { get; set; } = "normal";
        
        /// <summary>
        /// Creates a copy of this model
        /// </summary>
        public AIModel Clone()
        {
            return new AIModel
            {
                Id = this.Id,
                InputCostPer1K = this.InputCostPer1K,
                OutputCostPer1K = this.OutputCostPer1K,
                ModelName = this.ModelName,
                ProviderName = this.ProviderName,
                Description = this.Description,
                MaxTokens = this.MaxTokens,
                MaxContextWindow = this.MaxContextWindow,
                SupportsStreaming = this.SupportsStreaming,
                SupportsVision = this.SupportsVision,
                SupportsCodeCompletion = this.SupportsCodeCompletion,
                IsAvailable = this.IsAvailable,
                ApiKeyVariable = this.ApiKeyVariable,
                DisplayName = this.DisplayName,
                Version = this.Version,
                DefaultTemperature = this.DefaultTemperature,
                IsSelected = this.IsSelected,
                IsFavorite = this.IsFavorite,
                IsDefault = this.IsDefault,
                UsageCount = this.UsageCount,
                LastUsed = this.LastUsed,
                Status = this.Status,
                CreatedAt = this.CreatedAt,
                UpdatedAt = this.UpdatedAt
            };
        }
    }

}
