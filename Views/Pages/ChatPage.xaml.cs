using System;
using System.Threading.Tasks;
using NexusChat.Core.ViewModels;
using Microsoft.Maui.Controls;
using System.Diagnostics;
using NexusChat.Views.Pages;
using System.Threading;

namespace NexusChat.Views.Pages;

/// <summary>
/// Page for chat interactions with AI
/// </summary>
public partial class ChatPage : ContentPage
{
    private readonly ChatViewModel _viewModel;
    private CancellationTokenSource _scrollCancellationTokenSource;
    private bool _isScrolling = false;
    
    /// <summary>
    /// Initializes a new instance of ChatPage with injected ViewModel
    /// </summary>
    public ChatPage(ChatViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        _scrollCancellationTokenSource = new CancellationTokenSource();
    }
    
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Subscribe to collection changes to scroll when new messages arrive
        _viewModel.Messages.CollectionChanged += Messages_CollectionChanged;
        
        // Initialize in the background
        await InitializeViewModelAsync();
        
        // Focus on entry field with slight delay to ensure UI is ready
        await Task.Delay(100);
        await MainThread.InvokeOnMainThreadAsync(() => MessageEntry.Focus());
    }
    
    private async Task InitializeViewModelAsync()
    {
        try
        {
            // Create new cancellation token for this initialization
            _scrollCancellationTokenSource?.Cancel();
            _scrollCancellationTokenSource = new CancellationTokenSource();
            
            // Initialize the view model
            await _viewModel.InitializeAsync();
            
            // Clean up the messages
            _viewModel.CleanupEmptyMessages();
            
            // Scroll to bottom after loading messages with small delay to allow layout
            await ScrollToBottomWithDebounceAsync(50, _scrollCancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error initializing chat page: {ex.Message}");
            await DisplayAlert("Error", "Failed to initialize chat. Please try again.", "OK");
        }
    }

    protected override void OnDisappearing()
    {
        // Cancel any pending operations
        _scrollCancellationTokenSource?.Cancel();
        
        // Unsubscribe from events to prevent memory leaks
        _viewModel.Messages.CollectionChanged -= Messages_CollectionChanged;
        
        // Clean up the view model
        _viewModel?.Cleanup();
        
        base.OnDisappearing();
    }
    
    private async void Messages_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        // Only handle additions
        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
        {
            try
            {
                // Cancel any previous scroll operation
                _scrollCancellationTokenSource?.Cancel();
                _scrollCancellationTokenSource = new CancellationTokenSource();
                
                // Debounced scroll to avoid too many scroll operations
                await ScrollToBottomWithDebounceAsync(50, _scrollCancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                // This is expected when cancellation occurs
                Debug.WriteLine("Scroll operation was cancelled");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in collection changed handler: {ex.Message}");
            }
        }
    }
    
    private async Task ScrollToBottomWithDebounceAsync(int delayMs = 50, CancellationToken cancellationToken = default)
    {
        if (_isScrolling)
            return;
        
        _isScrolling = true;
        
        try
        {
            // Wait for a brief moment to allow UI to update
            await Task.Delay(delayMs, cancellationToken);
            
            // Perform scroll on UI thread
            await MainThread.InvokeOnMainThreadAsync(async () => {
                try
                {
                    // Check if the scroll view is available and has content to scroll
                    if (MessageScrollView != null && MessageScrollView.ContentSize.Height > 0)
                    {
                        // Use animation=false for better performance
                        await MessageScrollView.ScrollToAsync(0, MessageScrollView.ContentSize.Height, false);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Scroll error: {ex.Message}");
                }
            });
        }
        catch (OperationCanceledException)
        {
            // Operation was canceled, just ignore
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in ScrollToBottomWithDebounceAsync: {ex.Message}");
        }
        finally
        {
            _isScrolling = false;
        }
    }
    
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        
        // Try to scroll to bottom when size changes (e.g., orientation change)
        if (width > 0 && height > 0)
        {
            _ = ScrollToBottomWithDebounceAsync(100);
        }
    }
}
