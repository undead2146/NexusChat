using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace NexusChat.Core.ViewModels
{
    /// <summary>
    /// Base class for all view models
    /// </summary>
    public abstract partial class BaseViewModel : ObservableObject
    {
        private string _title;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _statusMessage;

        /// <summary>
        /// Gets or sets the page title
        /// </summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        /// <summary>
        /// Gets whether the ViewModel is not busy
        /// </summary>
        public bool IsNotBusy => !IsBusy;

        /// <summary>
        /// Command to navigate back to previous page
        /// </summary>
        public IAsyncRelayCommand GoBackCommand { get; }

        /// <summary>
        /// Initializes a new instance of the BaseViewModel class
        /// </summary>
        protected BaseViewModel()
        {
            GoBackCommand = new AsyncRelayCommand(GoBackAsync);
        }

        /// <summary>
        /// Navigate back to the previous page
        /// </summary>
        protected virtual async Task GoBackAsync()
        {
            try
            {
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Navigation error: {ex.Message}");
            }
        }

        /// <summary>
        /// Initialize the ViewModel
        /// </summary>
        public virtual Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Clean up resources used by the ViewModel
        /// </summary>
        public virtual void Cleanup()
        {
            // Override in derived classes if needed
        }

        /// <summary>
        /// Safely execute an operation with busy state handling and error catching
        /// </summary>
        protected async Task ExecuteWithBusyStatusAsync(Func<Task> operation, string initialStatus = null, string errorPrefix = "Error")
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;

                if (!string.IsNullOrEmpty(initialStatus))
                    StatusMessage = initialStatus;

                await operation();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{errorPrefix}: {ex.Message}");
                StatusMessage = $"{errorPrefix}: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Update IsNotBusy whenever IsBusy changes
        partial void OnIsBusyChanged(bool value)
        {
            OnPropertyChanged(nameof(IsNotBusy));
        }

        /// <summary>
        /// Easy way to raise property changed with CallerMemberName attribute
        /// </summary>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Dispose of resources used by the ViewModel
        /// </summary>
        public virtual void Dispose()
        {
            Cleanup();
        }


    }
}
