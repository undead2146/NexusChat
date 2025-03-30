using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using System.Diagnostics;
using System.Threading;
using NexusChat.Core.Models;
using NexusChat.Core.ViewModels;
using System.Linq;
using System.Collections.Specialized;

namespace NexusChat.Views.Pages
{
    /// <summary>
    /// Page for chat interactions with AI
    /// </summary>
    public partial class ChatPage : ContentPage
    {
        private readonly ChatViewModel _viewModel;
        private bool _isFirstAppearance = true;
        private bool _isScrolling = false;
        private const double SCROLL_THRESHOLD = 50; // Pixels from top to trigger loading more
        private bool _scrollToNewlyLoadedMessages = false;
        private int _previousItemCount = 0;
        private bool _userHasScrolled = false; // Flag to track if user has scrolled
        private Timer _scrollTriggerTimer; // Add a timer field for scroll debouncing
        
        /// <summary>
        /// Initializes a new instance of ChatPage with injected ViewModel
        /// </summary>
        public ChatPage(ChatViewModel viewModel)
        {
            try
            {
                InitializeComponent();
                _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
                BindingContext = _viewModel;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ChatPage constructor: {ex}");
            }
        }
        
        protected override async void OnAppearing()
        {
            try
            {
                base.OnAppearing();
                
                // Check if we already have data
                if (_viewModel?.CurrentConversation == null)
                {
                    // Initialize ViewModel
                    await _viewModel.InitializeAsync();
                }
                
                // Subscribe to collection changes
                if (_viewModel?.Messages != null)
                {
                    _viewModel.Messages.CollectionChanged -= Messages_CollectionChanged;
                    _viewModel.Messages.CollectionChanged += Messages_CollectionChanged;
                    Debug.WriteLine("Subscribed to Messages collection changes");
                    _previousItemCount = _viewModel.Messages.Count;
                }
                
                if (_isFirstAppearance)
                {
                    // Initial scroll to bottom
                    await Task.Delay(300);
                    await ScrollToBottomAsync(false);
                    _isFirstAppearance = false;
                    
                    // Reset user scroll flag
                    _userHasScrolled = false;
                    
                    // Remove auto-focus on entry field
                    // await MainThread.InvokeOnMainThreadAsync(() => MessageEntry?.Focus());
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ChatPage OnAppearing: {ex}");
            }
        }
        
        protected override void OnDisappearing()
        {
            try
            {
                _scrollTriggerTimer?.Dispose();
                _scrollTriggerTimer = null;
                
                // Unsubscribe from events first
                if (_viewModel?.Messages != null)
                {
                    _viewModel.Messages.CollectionChanged -= Messages_CollectionChanged;
                    Debug.WriteLine("Unsubscribed from Messages collection changes");
                }
                
                // Clean up viewmodel resources
                _viewModel?.Cleanup();
                
                // Call base implementation
                base.OnDisappearing();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ChatPage OnDisappearing: {ex}");
                base.OnDisappearing();
            }
        }
        
        private async void Messages_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            try
            {
                // Handle newly loaded older messages (inserted at the beginning)
                if (e.Action == NotifyCollectionChangedAction.Add && e.NewStartingIndex == 0 && _scrollToNewlyLoadedMessages)
                {
                    _scrollToNewlyLoadedMessages = false;
                    int newItemsCount = _viewModel.Messages.Count - _previousItemCount;
                    
                    // Give the UI time to update
                    await Task.Delay(150);
                    
                    // Scroll to the first item that was previously at the top
                    if (newItemsCount > 0 && MessageScrollView != null)
                    {
                        // Scroll to the message that was previously at index 0
                        await ScrollToPositionAsync(newItemsCount, animate: false);
                    }
                    
                    _previousItemCount = _viewModel.Messages.Count;
                    return;
                }
                
                // Handle new messages added at the end (normal case)
                if (e.Action != NotifyCollectionChangedAction.Add || e.NewStartingIndex == 0) return;
                
                // Get the added items
                var newItems = e.NewItems;
                if (newItems == null || newItems.Count == 0) return;

                // If we're already scrolling, we'll wait longer to avoid overlapping scrolls
                if (_isScrolling)
                {
                    Debug.WriteLine("Already scrolling - delaying this scroll operation");
                    await Task.Delay(300);
                }
                else
                {
                    // Short delay to let UI update
                    await Task.Delay(100);
                }
                
                // Update the previous count
                _previousItemCount = _viewModel.Messages.Count;
                
                // Single scroll after batch of operations
                await ScrollToBottomAsync(animate: true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error handling collection change: {ex}");
            }
        }

        /// <summary>
        /// Scrolls to the bottom of the chat
        /// </summary>
        private async Task ScrollToBottomAsync(bool animate = true)
        {
            if (_isScrolling)
            {
                Debug.WriteLine("Scroll operation already in progress - skipping");
                return;
            }
            
            _isScrolling = true;
            
            try
            {
                if (MessageScrollView == null)
                {
                    Debug.WriteLine("ScrollView is null - can't scroll");
                    return;
                }
                
                // Single smooth scrolling approach with better timing
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    try
                    {
                        // Give layout time to settle before scrolling
                        await Task.Delay(100);
                        
                        // Force layout to ensure accurate content size
                        MessageScrollView.ForceLayout();
                        
                        // Calculate exact scroll position
                        double y = Math.Max(0, MessageScrollView.ContentSize.Height - MessageScrollView.Height);
                        
                        // Single smooth scroll operation
                        await MessageScrollView.ScrollToAsync(0, y, animate);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error during scroll operation: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ScrollToBottomAsync: {ex}");
            }
            finally
            {
                // Longer delay before allowing more scrolling to prevent bouncing
                await Task.Delay(200);
                _isScrolling = false;
            }
        }
        
        /// <summary>
        /// Scrolls to a specific position in the chat
        /// </summary>
        private async Task ScrollToPositionAsync(int index, bool animate = true)
        {
            if (_isScrolling) return;
            
            _isScrolling = true;
            
            try
            {
                // Ensure we have a valid index
                if (index < 0 || _viewModel.Messages.Count <= index)
                {
                    Debug.WriteLine($"Invalid scroll index: {index}, message count: {_viewModel.Messages.Count}");
                    return;
                }
                
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    try
                    {
                        // Get the position of the item
                        var message = _viewModel.Messages[index];
                        
                        // Use CollectionView to scroll to the item
                        MessageList.ScrollTo(message, position: ScrollToPosition.Start, animate: animate);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error scrolling to position: {ex.Message}");
                    }
                });
                
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ScrollToPositionAsync: {ex}");
            }
            finally
            {
                await Task.Delay(200);
                _isScrolling = false;
            }
        }
        
        /// <summary>
        /// Handles scroll events to detect when to load more messages
        /// </summary>
        private void MessageScrollView_Scrolled(object sender, ScrolledEventArgs e)
        {
            try
            {
                // Skip immediately if loading is in progress
                if (_viewModel.IsLoadingMore)
                    return;
                
                // Mark that user has scrolled at least once, but don't count the very first scroll event
                if (!_userHasScrolled)
                {
                    _userHasScrolled = true;
                    Debug.WriteLine("First scroll detected - ignoring as automatic");
                    return; // Skip the initial scroll which is caused by layout
                }
                
                // Detect if we're near the top
                bool isNearTop = e.ScrollY < SCROLL_THRESHOLD;
                
                // Only attempt auto-loading if we're not in initial load and user has intentionally scrolled
                if (isNearTop && !_viewModel.IsLoadingMore && _viewModel.CanLoadMore && _userHasScrolled)
                {
                    // Don't trigger immediately to avoid unintended loads
                    if (_scrollTriggerTimer != null)
                    {
                        _scrollTriggerTimer.Dispose();
                    }
                    
                    _scrollTriggerTimer = new Timer(_ => 
                    {
                        if (isNearTop && !_viewModel.IsLoadingMore && _viewModel.CanLoadMore)
                        {
                            MainThread.BeginInvokeOnMainThread(() => 
                            {
                                Debug.WriteLine($"Auto-loading more messages: ScrollY={e.ScrollY}");
                                _scrollToNewlyLoadedMessages = true;
                                _previousItemCount = _viewModel.Messages.Count;
                                _viewModel.LoadMoreMessagesCommand.Execute(null);
                            });
                        }
                    }, null, 300, Timeout.Infinite); // 300ms debounce
                }
                
                // Show/hide scroll to bottom button based on scroll position
                double distanceFromBottom = MessageScrollView.ContentSize.Height - (e.ScrollY + MessageScrollView.Height);
                _viewModel.ShowScrollToBottom = distanceFromBottom > 200;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in MessageScrollView_Scrolled: {ex}");
            }
        }
    }
}
