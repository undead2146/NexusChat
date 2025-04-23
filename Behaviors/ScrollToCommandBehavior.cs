using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NexusChat.Behaviors
{
    public class ScrollToCommandBehavior : Behavior<CollectionView>
    {
        public static readonly BindableProperty ScrollTargetProperty =
            BindableProperty.Create(nameof(ScrollTarget), typeof(object), typeof(ScrollToCommandBehavior), null, 
                propertyChanged: OnScrollTargetChanged);

        public object ScrollTarget
        {
            get => GetValue(ScrollTargetProperty);
            set => SetValue(ScrollTargetProperty, value);
        }

        private static void OnScrollTargetChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var behavior = (ScrollToCommandBehavior)bindable;
            if (behavior._collectionView != null && newValue != null)
            {
                // Use async method to prevent UI thread blocking
                Task.Run(async () => await behavior.ScrollToItemAsync(newValue));
            }
        }

        private CollectionView _collectionView;
        private IDispatcher _dispatcher;

        protected override void OnAttachedTo(CollectionView bindable)
        {
            base.OnAttachedTo(bindable);
            _collectionView = bindable;
            _dispatcher = bindable.Dispatcher;
        }

        protected override void OnDetachingFrom(CollectionView bindable)
        {
            base.OnDetachingFrom(bindable);
            _collectionView = null;
            _dispatcher = null;
        }

        private async Task ScrollToItemAsync(object item)
        {
            try
            {
                if (_collectionView != null && item != null && _dispatcher != null)
                {
                    // Small delay to allow UI to update before scrolling
                    await Task.Delay(150);
                    
                    // Use dispatcher to ensure we're on the UI thread
                    await _dispatcher.DispatchAsync(() => {
                        // Add a try-catch specifically for the ScrollTo operation
                        try
                        {
                            _collectionView.ScrollTo(item, 
                                position: ScrollToPosition.Start,  // Scroll to top of item
                                animate: true);  // Use animation
                            
                            Debug.WriteLine($"Scrolled to item: {item}");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error during ScrollTo operation: {ex.Message}");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error scrolling to item: {ex.Message}");
            }
        }
    }
}
