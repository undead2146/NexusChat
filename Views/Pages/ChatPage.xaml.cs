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
        // Scroll to bottom when new messages are added
        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
        {
            try
            {
                Debug.WriteLine("Collection changed - attempting to scroll to bottom");
                
                // Give the UI time to update before scrolling
                await Task.Delay(150); // Increase delay slightly
                
                // Ensure the collection view has updated its layout - fix error by using InvalidateMeasure
                MessagesCollection.InvalidateMeasure();
                
                // Try different scroll methods
                try
                {
                    // Method 1: Using ScrollToAsync
                    await MessageScrollView.ScrollToAsync(0, MessageScrollView.ContentSize.Height, true);
                    Debug.WriteLine("Method 1: Scrolled to bottom using ScrollToAsync with ContentSize.Height");
                }
                catch
                {
                    try
                    {
                        // Method 2: Using double.MaxValue
                        await MessageScrollView.ScrollToAsync(0, double.MaxValue, true);
                        Debug.WriteLine("Method 2: Scrolled to bottom using ScrollToAsync with double.MaxValue");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error scrolling using method 2: {ex.Message}");
                        
                        // Method 3: Force layout updates first
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            try
                            {
                                // Force layout update - fix error by using InvalidateMeasure
                                MessageScrollView.InvalidateMeasure();
                                MessagesCollection.InvalidateMeasure();
                                
                                // Wait a bit longer
                                await Task.Delay(100);
                                
                                // Try scrolling again
                                await MessageScrollView.ScrollToAsync(MessageScrollView.ScrollX, MessageScrollView.ContentSize.Height, true);
                                Debug.WriteLine("Method 3: Scrolled to bottom after forcing layout updates");
                            }
                            catch (Exception scrollEx)
                            {
                                Debug.WriteLine($"Failed to scroll using method 3: {scrollEx.Message}");
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error scrolling to bottom: {ex.Message}");
            }
        }
    }
    
    private async Task ScrollToBottomAsync()
    {
        try
        {
            // Give the UI time to update before scrolling
            await Task.Delay(50);
            await MessageScrollView.ScrollToAsync(0, MessageScrollView.ContentSize.Height, true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error scrolling to bottom: {ex.Message}");
        }
    }
}
