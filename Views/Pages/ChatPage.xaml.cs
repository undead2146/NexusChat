using System;
using System.Threading.Tasks;
using NexusChat.Core.ViewModels;
using Microsoft.Maui.Controls;
using System.Diagnostics;
using NexusChat.Views.Pages;

namespace NexusChat.Views.Pages;

/// <summary>
/// Page for chat interactions with AI
/// </summary>
public partial class ChatPage : ContentPage
{
    private readonly ChatViewModel _viewModel;
    
    /// <summary>
    /// Initializes a new instance of ChatPage with injected ViewModel
    /// </summary>
    public ChatPage(ChatViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        
        // Subscribe to collection changes to scroll when new messages arrive
        _viewModel.Messages.CollectionChanged += Messages_CollectionChanged;
    }
    
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await InitializeViewModelAsync();
        
        // Focus on entry field
        await Task.Delay(100);
        MessageEntry.Focus();
    }
    
    private async Task InitializeViewModelAsync()
    {
        try
        {
            await _viewModel.InitializeAsync();
            
            // Clean up the messages
            _viewModel.CleanupEmptyMessages();
            
            // Scroll to bottom after loading messages
            await ScrollToBottomAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing chat page: {ex.Message}");
            await DisplayAlert("Error", "Failed to initialize chat. Please try again.", "OK");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        // Unsubscribe from events to prevent memory leaks
        _viewModel.Messages.CollectionChanged -= Messages_CollectionChanged;
        _viewModel?.Cleanup();
    }
    
    private async void Messages_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        // Only handle additions
        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
        {
            try
            {
                // EMERGENCY FIX: Ultra-simple scroll with minimal overhead
                // Use a very short delay then scroll directly
                await Task.Delay(50);
                
                MainThread.BeginInvokeOnMainThread(async () => {
                    try
                    {
                        // Direct scroll to bottom - no layout invalidation or other operations
                        await MessageScrollView.ScrollToAsync(0, MessageScrollView.ContentSize.Height, false);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Scroll error: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in collection handler: {ex.Message}");
            }
        }
    }
    
    private async Task ScrollToBottomAsync()
    {
        // EMERGENCY FIX: Ultra-simple scroll with minimal overhead
        await Task.Delay(50);
        
        MainThread.BeginInvokeOnMainThread(async () => {
            try
            {
                await MessageScrollView.ScrollToAsync(0, MessageScrollView.ContentSize.Height, false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Scroll error: {ex.Message}");
            }
        });
    }
}
