using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using NexusChat.Core.Models;
using NexusChat.Core.ViewModels;
using NexusChat.Helpers;
using NexusChat.Services.Interfaces;

namespace NexusChat.Views.Pages
{
    /// <summary>
    /// Main chat page that shows conversation with AI
    /// </summary>
    public partial class ChatPage : ContentPage
    {
        private readonly ChatViewModel _viewModel;
        private readonly INavigationService _navigationService;
        private bool _isScrolling = false;
        private bool _isAnimating = false;

        /// <summary>
        /// Creates a new instance of ChatPage
        /// </summary>
        /// <param name="viewModel">ViewModel for this page</param>
        public ChatPage(ChatViewModel viewModel, INavigationService navigationService)
        {
            InitializeComponent();
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            BindingContext = _viewModel;
            
            Debug.WriteLine($"ChatPage constructor - BindingContext set to: {BindingContext?.GetType().Name ?? "null"}");
            
            RegisterEventHandlers();
            ConfigureStatusBar();
            
            // Initialize sidebar as closed
            SidebarContainer.TranslationX = -300;
            MainContent.TranslationX = 0;
            SidebarOverlay.IsVisible = false;
            SidebarOverlay.Opacity = 0;
        }

        /// <summary>
        /// Configures status bar and safe area behavior
        /// </summary>
        private void ConfigureStatusBar()
        {
#if ANDROID
            Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.Application.SetWindowSoftInputModeAdjust(
                this, Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.WindowSoftInputModeAdjust.Resize);
#endif
        }

        /// <summary>
        /// Registers all event handlers for UI elements
        /// </summary>
        private void RegisterEventHandlers()
        {
            // Listen for scroll target changes from the ViewModel
            _viewModel.PropertyChanged += (sender, e) => {
                if (e.PropertyName == nameof(_viewModel.ScrollTarget) && _viewModel.ScrollTarget != null)
                {
                    ScrollToTargetAsync(_viewModel.ScrollTarget);
                }
                
                if (e.PropertyName == nameof(_viewModel.IsSidebarOpen))
                {
                    HandleSidebarStateChange();
                }
            };
            
            // Subscribe to sidebar refresh requests
            _viewModel.SidebarRefreshRequested += OnSidebarRefreshRequested;
        }
        
        /// <summary>
        /// Handles requests to refresh the sidebar
        /// </summary>
        private async void OnSidebarRefreshRequested()
        {
            try
            {
                Debug.WriteLine("Sidebar refresh requested from ChatViewModel");
                
                // Get the sidebar view model and refresh it
                if (SidebarContent?.BindingContext is ConversationsSidebarViewModel sidebarViewModel)
                {
                    await sidebarViewModel.LoadConversations(forceRefresh: true);
                    Debug.WriteLine("Sidebar conversations refreshed successfully");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error refreshing sidebar: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Called when the page appears
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            Debug.WriteLine("ChatPage.OnAppearing");
            
            // Configure for immersive experience
#if IOS
            var statusBarManager = Microsoft.Maui.Controls.Application.Current?.Handler?.PlatformView;
            if (statusBarManager != null)
            {
                // Handle iOS status bar
            }
#endif

            _viewModel.OnAppearing();
            
            // Initialize the page
            await InitializePageAsync();
            
            // Load the sidebar content
            LoadSidebarContent();
        }
        
        /// <summary>
        /// Called when the page disappears
        /// </summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            
            // Unsubscribe from events to prevent memory leaks
            _viewModel.SidebarRefreshRequested -= OnSidebarRefreshRequested;
            
            _viewModel.OnDisappearing();
        }
        
        /// <summary>
        /// Loads the conversations sidebar content
        /// </summary>
        private void LoadSidebarContent() 
        {
            try 
            {
                Debug.WriteLine("Loading conversations sidebar content");
                
                // Get the ConversationsSidebarViewModel from DI
                var conversationsSidebarViewModel = Handler?.MauiContext?.Services?.GetService(typeof(ConversationsSidebarViewModel)) as ConversationsSidebarViewModel;
                if (conversationsSidebarViewModel == null) 
                {
                    Debug.WriteLine("Failed to resolve ConversationsSidebarViewModel, using fallback");
                    CreateFallbackSidebar();
                    return;
                }

                // Ensure the sidebar content has the correct binding context
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        // Set the binding context explicitly to prevent inheritance from parent
                        SidebarContent.BindingContext = conversationsSidebarViewModel;
                        
                        // Verify the binding context was set correctly
                        Debug.WriteLine($"Sidebar BindingContext set to: {SidebarContent.BindingContext?.GetType().Name ?? "null"}");
                        
                        // Handle conversation events
                        conversationsSidebarViewModel.ConversationSelected += OnConversationSelectedFromSidebar;
                        conversationsSidebarViewModel.ConversationDeleted += OnConversationDeletedFromSidebar;
                        conversationsSidebarViewModel.ConversationCreated += OnConversationCreatedFromSidebar;
                        
                        Debug.WriteLine("Sidebar content loaded successfully");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error setting sidebar binding context: {ex.Message}");
                        CreateFallbackSidebar();
                    }
                });
                
                // Initialize the sidebar content
                Task.Run(async () => await conversationsSidebarViewModel.InitializeAsync());
            }
            catch (Exception ex) 
            {
                Debug.WriteLine($"Error loading sidebar content: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                CreateFallbackSidebar();
            }
        }
        
        /// <summary>
        /// Creates a fallback sidebar when the main sidebar fails to load
        /// </summary>
        private void CreateFallbackSidebar()
        {
            // Fallback - display simple alternative
            SidebarContent.Content = new VerticalStackLayout 
            {
                Padding = new Thickness(20),
                Children = {
                    new Label { 
                        Text = "Recent Conversations",
                        FontSize = 18,
                        Margin = new Thickness(0, 0, 0, 20)
                    },
                    new Button {
                        Text = "New Chat",
                        Command = new Command(async () => {
                            await _viewModel.InitializeNewConversationAsync();
                            _viewModel.IsSidebarOpen = false;
                        })
                    }
                }
            };
        }
        
        /// <summary>
        /// Handles the sidebar state change
        /// </summary>
        private async void HandleSidebarStateChange()
        {
            if (_isAnimating) return;
            
            bool isSidebarOpen = _viewModel.IsSidebarOpen;
            Debug.WriteLine($"Sidebar state changed: {(isSidebarOpen ? "Open" : "Closed")}");
            
            _isAnimating = true;
            
            try
            {
                if (isSidebarOpen)
                {
                    // Show the sidebar with animation
                    SidebarOverlay.IsVisible = true;
                    SidebarContainer.IsVisible = true;
                    
                    await Task.WhenAll(
                        SidebarContainer.TranslateTo(0, 0, 300, Easing.CubicOut),
                        MainContent.TranslateTo(300, 0, 300, Easing.CubicOut),
                        SidebarOverlay.FadeTo(0.5, 300, Easing.Linear)
                    );
                }
                else
                {
                    // Hide the sidebar with animation
                    await Task.WhenAll(
                        SidebarContainer.TranslateTo(-300, 0, 300, Easing.CubicIn),
                        MainContent.TranslateTo(0, 0, 300, Easing.CubicIn),
                        SidebarOverlay.FadeTo(0, 300, Easing.Linear)
                    );
                    
                    SidebarOverlay.IsVisible = false;
                    SidebarContainer.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during sidebar animation: {ex.Message}");
                
                // Fallback - set positions directly
                if (isSidebarOpen)
                {
                    SidebarContainer.TranslationX = 0;
                    MainContent.TranslationX = 300;
                    SidebarOverlay.Opacity = 0.5;
                    SidebarOverlay.IsVisible = true;
                    SidebarContainer.IsVisible = true;
                }
                else
                {
                    SidebarContainer.TranslationX = -300;
                    MainContent.TranslationX = 0;
                    SidebarOverlay.Opacity = 0;
                    SidebarOverlay.IsVisible = false;
                    SidebarContainer.IsVisible = false;
                }
            }
            finally
            {
                _isAnimating = false;
            }
        }
        
        /// <summary>
        /// Initializes the page
        /// </summary>
        private async Task InitializePageAsync()
        {
            try
            {
                // Ensure conversation exists
                await _viewModel.EnsureConversationExistsAsync();
                
                // Wait for UI to fully render
                await Task.Delay(200);
                await ScrollToBottomIfNeededAsync();
                
                // Log state for debugging
                Debug.WriteLine($"After initialization - Conversation: {_viewModel.CurrentConversation?.Id ?? -1}, " +
                    $"Title: '{_viewModel.CurrentConversation?.Title ?? "null"}', " +
                    $"HasMessages: {_viewModel.HasMessages}, MessagesCount: {_viewModel.Messages?.Count}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in InitializePageAsync: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handles the Editor Completed event (when Enter key is pressed)
        /// </summary>
        private void OnEditorCompleted(object sender, EventArgs e)
        {
            Debug.WriteLine("Editor completed (Enter pressed)");
            
            if (_viewModel.CanSendMessage)
            {
                // Execute on UI thread for immediate response
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        await _viewModel.SendMessage();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in OnEditorCompleted: {ex.Message}");
                    }
                });
            }
        }
        
        /// <summary>
        /// Handles the Send button click
        /// </summary>
        private async void OnSendButtonClicked(object sender, EventArgs e)
        {
            Debug.WriteLine("Send button clicked directly");
            
            if (_viewModel == null)
            {
                Debug.WriteLine("ViewModel is null, cannot send message");
                return;
            }
            
            if (!_viewModel.CanSendMessage)
            {
                Debug.WriteLine("Cannot send message: CanSendMessage is false");
                return;
            }
            
            try
            {
                // Force hide keyboard immediately
                MessageEditor.Unfocus();
                
                // Execute on UI thread for immediate response
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await _viewModel.SendMessage();
                });
                
                Debug.WriteLine("Message sent successfully via OnSendButtonClicked");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error sending message: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// Scrolls to a specific target in the message list
        /// </summary>
        private async Task ScrollToTargetAsync(object target)
        {
            if (target == null || MessageCollectionView == null || _isScrolling)
                return;
                
            try
            {
                _isScrolling = true;
                
                // Reduce delay for faster scrolling
                await Task.Delay(50);
                
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    try
                    {
                        if (target is Message message)
                        {
                            MessageCollectionView.ScrollTo(message, position: ScrollToPosition.MakeVisible, animate: false);
                        }
                        else
                        {
                            MessageCollectionView.ScrollTo(target, position: ScrollToPosition.MakeVisible, animate: false);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error scrolling to target: {ex.Message}");
                    }
                });
            }
            finally
            {
                _isScrolling = false;
            }
        }
        
        /// <summary>
        /// Scrolls to the bottom of the message list if needed
        /// </summary>
        private async Task ScrollToBottomIfNeededAsync()
        {
            if (_viewModel.HasMessages)
            {
                try
                {
                    // Reduce delay for faster scrolling
                    await Task.Delay(50);
                    
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        try
                        {
                            var lastMessage = _viewModel.Messages.LastOrDefault();
                            if (lastMessage != null)
                            {
                                MessageCollectionView.ScrollTo(lastMessage, position: ScrollToPosition.End, animate: false);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error scrolling to bottom: {ex.Message}");
                        }
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in ScrollToBottomIfNeededAsync: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Handles conversation selection from sidebar
        /// </summary>
        private void OnConversationSelectedFromSidebar(Conversation conversation)
        {
            try
            {
                Debug.WriteLine($"Conversation selected from sidebar: {conversation.Title} (ID: {conversation.Id})");
                
                // Load the selected conversation in the chat view
                _viewModel.LoadConversation(conversation);
                
                // Close sidebar when conversation is selected
                _viewModel.IsSidebarOpen = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error handling conversation selection from sidebar: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handles conversation deletion from sidebar
        /// </summary>
        private void OnConversationDeletedFromSidebar(Conversation deletedConversation)
        {
            try
            {
                Debug.WriteLine($"Conversation deleted from sidebar: {deletedConversation.Title} (ID: {deletedConversation.Id})");
                
                // If the deleted conversation is currently loaded, the sidebar will handle selecting a new one
                // We don't need to do anything special here as the ConversationSelected event will be fired
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error handling conversation deletion from sidebar: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handles conversation creation from sidebar
        /// </summary>
        private void OnConversationCreatedFromSidebar(Conversation newConversation)
        {
            try
            {
                Debug.WriteLine($"New conversation created from sidebar: {newConversation.Title} (ID: {newConversation.Id})");
                
                // Load the new conversation
                _viewModel.LoadConversation(newConversation);
                
                // Close sidebar
                _viewModel.IsSidebarOpen = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error handling conversation creation from sidebar: {ex.Message}");
            }
        }
    }
}
