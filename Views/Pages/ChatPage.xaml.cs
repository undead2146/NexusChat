using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using NexusChat.Core.Models;
using NexusChat.Core.ViewModels;
using NexusChat.Helpers;
using NexusChat.Views.Pages;
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
            
            // Initialize sidebar as closed
            SidebarContainer.TranslationX = -300;
            MainContent.TranslationX = 0;
            SidebarOverlay.IsVisible = false;
            SidebarOverlay.Opacity = 0;
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
        }

        /// <summary>
        /// Called when the page appears
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            Debug.WriteLine("ChatPage.OnAppearing");
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
            _viewModel.OnDisappearing();
        }
        
        /// <summary>
        /// Loads the conversations page into the sidebar
        /// </summary>
        private void LoadSidebarContent() {
            try {
                Debug.WriteLine("Loading conversations page into sidebar");
                
                // Get the ConversationsPageViewModel from DI
                var conversationsViewModel = Handler?.MauiContext?.Services?.GetService(typeof(ConversationsPageViewModel)) as ConversationsPageViewModel;
                if (conversationsViewModel == null) {
                    Debug.WriteLine("Failed to resolve ConversationsPageViewModel");
                    throw new InvalidOperationException("ConversationsPageViewModel could not be resolved.");
                }

                // Create the conversations page
                var conversationsPage = new ConversationsPage(conversationsViewModel);
                
                // Important: Need to set the proper parent to initialize the page properly
                conversationsPage.Parent = this;
                
                // Set the content of the sidebar to the conversations page content
                SidebarContent.Content = conversationsPage.Content;
                
                // Ensure the ViewModel is initialized
                conversationsViewModel.OnAppearing();
                
                // Handle conversation selection to close the sidebar and navigate
                conversationsViewModel.PropertyChanged += (sender, e) => {
                    if (e.PropertyName == nameof(conversationsViewModel.SelectedConversation) && 
                        conversationsViewModel.SelectedConversation != null) {
                        Debug.WriteLine("Conversation selected from sidebar");
                        
                        // Close sidebar when conversation is selected
                        _viewModel.IsSidebarOpen = false;
                    }
                };
                
                Debug.WriteLine("Sidebar content loaded successfully");
            }
            catch (Exception ex) {
                Debug.WriteLine($"Error loading sidebar content: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Fallback - display simple alternative
                SidebarContent.Content = new VerticalStackLayout {
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
                                await _navigationService.NavigateToAsync("chat");
                                _viewModel.IsSidebarOpen = false;
                            })
                        }
                    }
                };
            }
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
                    await AnimationHelpers.ShowSidebarAsync(SidebarContainer, MainContent, SidebarOverlay);
                }
                else
                {
                    // Hide the sidebar with animation
                    await AnimationHelpers.HideSidebarAsync(SidebarContainer, MainContent, SidebarOverlay);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during sidebar animation: {ex.Message}");
                
                // Fallback to using visual states directly in case of animation errors
                VisualStateManager.GoToState(this, isSidebarOpen ? "SidebarOpen" : "SidebarClosed");
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
                // Execute the SendMessage command directly from the viewmodel
                _viewModel.SendMessageCommand.Execute(null);
            }
        }
        
        /// <summary>
        /// Handles the Send button click - Properly implemented
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
                // Force hide keyboard
                MessageEditor.Unfocus();
                
                // Execute the SendMessageCommand directly
                await _viewModel.SendMessage();
                
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
                await Task.Delay(100);
                
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    try
                    {
                        if (target is Message message)
                        {
                            MessageCollectionView.ScrollTo(message, position: ScrollToPosition.MakeVisible, animate: true);
                        }
                        else
                        {
                            MessageCollectionView.ScrollTo(target, position: ScrollToPosition.MakeVisible, animate: true);
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
                    await Task.Delay(100);
                    
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        try
                        {
                            var lastMessage = _viewModel.Messages.LastOrDefault();
                            if (lastMessage != null)
                            {
                                MessageCollectionView.ScrollTo(lastMessage, position: ScrollToPosition.End, animate: true);
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
    }
}
