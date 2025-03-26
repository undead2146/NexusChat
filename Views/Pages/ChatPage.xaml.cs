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
                }
                
                if (_isFirstAppearance)
                {
                    // Initial scroll to bottom
                    await Task.Delay(300);
                    await ScrollToBottomAsync(false);
                    _isFirstAppearance = false;
                    
                    // Focus on entry field
                    await Task.Delay(100);
                    await MainThread.InvokeOnMainThreadAsync(() => MessageEntry?.Focus());
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
                // Only care about additions
                if (e.Action != NotifyCollectionChangedAction.Add) return;
                
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
    }
}
