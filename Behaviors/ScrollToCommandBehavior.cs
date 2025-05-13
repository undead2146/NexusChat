using System;
using System.Windows.Input;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using System.Diagnostics; // Add this using directive
using System.Linq; // Add this using directive
using NexusChat.Core.Models; // Add the using statement to reference the new class

namespace NexusChat.Behaviors
{
    public class ScrollToCommandBehavior : Behavior<CollectionView>
    {
        public static readonly BindableProperty ScrollTargetProperty =
            BindableProperty.Create(
                nameof(ScrollTarget),
                typeof(object),
                typeof(ScrollToCommandBehavior),
                null,
                propertyChanged: OnScrollTargetChanged);

        public object ScrollTarget
        {
            get => GetValue(ScrollTargetProperty);
            set => SetValue(ScrollTargetProperty, value);
        }

        private static void OnScrollTargetChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var behavior = (ScrollToCommandBehavior)bindable;

            if (behavior.AssociatedObject != null && newValue != null)
            {
                behavior.ScrollToTarget(newValue);
            }
        }

        protected override void OnAttachedTo(CollectionView collectionView)
        {
            base.OnAttachedTo(collectionView);
            AssociatedObject = collectionView;
        }

        protected override void OnDetachingFrom(CollectionView collectionView)
        {
            base.OnDetachingFrom(collectionView);
            AssociatedObject = null;
        }

        private void ScrollToTarget(object target)
        {
            if (AssociatedObject == null || target == null)
                return;

            try
            {
                // Handle ScrollTargetInfo type
                if (target is ScrollTargetInfo info)
                {
                    if (info.ScrollToBottom)
                    {
                        // For CollectionView, scroll to last item
                        if (AssociatedObject.ItemsSource is IEnumerable<object> items)
                        {
                            var itemsList = items.ToList();
                            if (itemsList.Count > 0)
                            {
                                AssociatedObject.ScrollTo(itemsList.Count - 1, position: ScrollToPosition.End, animate: info.ShouldAnimate);
                            }
                        }
                    }
                    else if (info.ScrollToIndex.HasValue && info.ScrollToIndex.Value >= 0)
                    {
                        AssociatedObject.ScrollTo(info.ScrollToIndex.Value, position: ScrollToPosition.Center, animate: info.ShouldAnimate);
                    }
                    return;
                }
                
                // Can scroll to either an item or an index
                if (target is int index && index >= 0)
                {
                    if (AssociatedObject.ItemsSource is IList<object> items && 
                        index < items.Count)
                    {
                        AssociatedObject.ScrollTo(index, position: ScrollToPosition.Center, animate: true);
                    }
                }
                else
                {
                    // Try to scroll to the target object
                    AssociatedObject.ScrollTo(target, position: ScrollToPosition.Center, animate: true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error scrolling to target: {ex.Message}");
            }
        }

        public CollectionView AssociatedObject { get; private set; }
    }
}
