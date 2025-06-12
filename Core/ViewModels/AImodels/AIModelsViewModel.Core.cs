using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using NexusChat.Core.Models;
using NexusChat.Services.Interfaces;
using NexusChat.Services.AIProviders;
using NexusChat.Data.Interfaces;
using System.Collections.Specialized;

namespace NexusChat.Core.ViewModels
{
    public partial class AIModelsViewModel : BaseViewModel
    {
        #region Private Fields
        private readonly IAIModelManager _modelManager;
        private readonly IApiKeyManager _apiKeyManager;
        private readonly INavigationService _navigationService;
        private readonly IAIProviderFactory _providerFactory;
        private readonly IAIModelRepository _modelRepository;

        private bool _hasBeenInitialized = false;
        private bool _isAnAnimationInProgress = false;
        private readonly object _animationLock = new object();
        private DateTime _lastModelRefresh = DateTime.MinValue;
        private readonly TimeSpan _modelCacheTimeout = TimeSpan.FromMinutes(15);
        private bool _isInitialLoad = true;

        private Dictionary<string, bool> _animationStates = new Dictionary<string, bool>();
        #endregion

        #region Observable Properties
        [ObservableProperty]
        private ObservableRangeCollection<AIModel> _models = new();

        [ObservableProperty]
        private ObservableRangeCollection<ModelGroup> _groupedModels = new();

        [ObservableProperty]
        private AIModel? _selectedModel;

        [ObservableProperty]
        private ModelGroup? _selectedModelGroup;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isNotLoading = true;

        [ObservableProperty]
        private bool _hasError;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _showNoResults;

        [ObservableProperty]
        private bool _showFavoritesOnly;

        [ObservableProperty]
        private bool _showActionResult;

        [ObservableProperty]
        private string _lastActionResult = string.Empty;

        [ObservableProperty]
        private double _notificationOpacity = 0;
        
        [ObservableProperty]
        private AIModel? _scrollToModel;
        
        [ObservableProperty]
        private bool _isRefreshing;

        [ObservableProperty]
        private bool _showApiKeyOverlay;

        [ObservableProperty]
        private double _refreshButtonRotation = 0;

        [ObservableProperty]
        private List<string> _existingApiKeys = new List<string>();

        [ObservableProperty]
        private ObservableCollection<AIModel> _filteredModels = new();

        [ObservableProperty]
        private bool _showAddButton = true;

        [ObservableProperty]
        private bool _isApiKeyOverlayVisible = false;
        #endregion

        #region Events
        public event Action<AIModel>? ScrollToModelRequested;
        #endregion

        #region Constructor
        public AIModelsViewModel(
            IAIModelManager modelManager,
            IApiKeyManager apiKeyManager,
            INavigationService navigationService,
            IAIProviderFactory providerFactory,
            IAIModelRepository modelRepository)
        {
            Title = "AI Models";
            _modelManager = modelManager ?? throw new ArgumentNullException(nameof(modelManager));
            _apiKeyManager = apiKeyManager ?? throw new ArgumentNullException(nameof(apiKeyManager));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
            _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));

            Models = new ObservableRangeCollection<AIModel>();
            GroupedModels = new ObservableRangeCollection<ModelGroup>();
            
            IsLoading = false;
            
            _modelManager.CurrentModelChanged += OnCurrentModelChanged;
        }
        #endregion

        #region Service Initialization
        public override async Task InitializeAsync()
        {
            Debug.WriteLine("AIModelsViewModel: Initializing services only");
            
            try
            {
                if (_apiKeyManager == null || _modelManager == null || _providerFactory == null)
                {
                    Debug.WriteLine("AIModelsViewModel: Critical services not available");
                    throw new InvalidOperationException("Required services not available");
                }
                
                await InitializeServicesOnly();
                
                Debug.WriteLine("AIModelsViewModel: Services initialization completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AIModelsViewModel: Error in InitializeAsync: {ex.Message}");
                HasError = true;
                ErrorMessage = $"Error: {ex.Message}";
                IsLoading = false;
            }
        }

        private async Task InitializeServicesOnly()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                Debug.WriteLine("AIModelsViewModel: Starting services initialization");
                
                // Initialize services without waiting for discovery
                await _apiKeyManager.InitializeAsync();
                await _modelManager.InitializeAsync();
                
                Debug.WriteLine($"Core services initialized in {stopwatch.ElapsedMilliseconds}ms");
                Debug.WriteLine($"Services initialization finished in {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in InitializeServicesOnly: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    HasError = true;
                    ErrorMessage = $"Error during initialization: {ex.Message}";
                    IsLoading = false;
                });
            }
            finally
            {
                stopwatch.Stop();
                Debug.WriteLine($"InitializeServicesOnly total time: {stopwatch.ElapsedMilliseconds}ms");
            }
        }

        private async Task BackgroundDiscoveryOnly()
        {
            try
            {
                Debug.WriteLine("Starting background discovery");
                
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                
                await Task.Run(async () =>
                {
                    try
                    {
                        var result = await _modelManager.DiscoverAndLoadModelsAsync();
                        Debug.WriteLine($"Background discovery completed: {result}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Background discovery error: {ex.Message}");
                    }
                }, cts.Token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"BackgroundDiscoveryOnly error: {ex.Message}");
            }
        }
        #endregion

        #region Model Changed Event Handler
        private void OnCurrentModelChanged(object? sender, AIModel e)
        {
            Debug.WriteLine("AIModelsViewModel: Current model changed event received");
            SelectedModel = e;
            
            foreach (var item in Models)
            {
                item.IsSelected = string.Equals(item.ProviderName, e.ProviderName, StringComparison.OrdinalIgnoreCase) &&
                                  string.Equals(item.ModelName, e.ModelName, StringComparison.OrdinalIgnoreCase);
            }
            
            var selectedModel = Models.FirstOrDefault(m => m.IsSelected);
            if (selectedModel != null)
            {
                ScrollToModel = selectedModel;
                TriggerModelAnimation(selectedModel).ConfigureAwait(false);
                ScrollToModelRequested?.Invoke(selectedModel);
            }
            
            Debug.WriteLine("AIModelsViewModel: Model selection updated");
        }
        #endregion
    }

    public class ModelGroup : ObservableRangeCollection<AIModel>
    {
        public string Name { get; }

        public ModelGroup(string name, List<AIModel> models) : base()
        {
            Name = name;
            if (models != null && models.Any())
            {
                Task.Run(async () => await AddRangeAsync(models, useIncrementalLoading: false));
            }
        }
    }

    public class ObservableRangeCollection<T> : ObservableCollection<T>
    {
        private bool _suppressNotification = false;

        public ObservableRangeCollection() : base() { }

        public ObservableRangeCollection(IEnumerable<T> collection) : base(collection) { }

        public async Task AddRangeAsync(IEnumerable<T> items, bool useIncrementalLoading = true)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            var itemList = items.ToList();
            if (!itemList.Any())
                return;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                foreach (var item in itemList)
                {
                    Add(item);
                }
            });
        }

        public async Task ClearAsync()
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Clear();
            });
        }

        public async Task ReplaceAllAsync(IEnumerable<T> newItems)
        {
            if (newItems == null)
                throw new ArgumentNullException(nameof(newItems));

            var newItemsList = newItems.ToList();
            
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Clear();
                foreach (var item in newItemsList)
                {
                    Add(item);
                }
            });
        }

        public new void Clear()
        {
            if (!Items.Any())
                return;
                
            _suppressNotification = true;
            
            try
            {
                Items.Clear();
            }
            finally
            {
                _suppressNotification = false;
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(Count)));
                OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Item[]"));
            }
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!_suppressNotification)
            {
                base.OnCollectionChanged(e);
            }
        }
    }
}
