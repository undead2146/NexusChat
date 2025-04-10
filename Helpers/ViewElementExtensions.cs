using System;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace NexusChat.Helpers
{
    /// <summary>
    /// Extension methods for working with Visual Elements in MAUI
    /// </summary>
    public static class ViewElementExtensions
    {
        /// <summary>
        /// Animates a color transition for a visual element
        /// </summary>
        public static Task<bool> ColorTo(this VisualElement self, Color fromColor, Color toColor, Action<Color> callback, uint length = 250, Easing easing = null)
        {
            easing = easing ?? Easing.Linear;
            
            Func<double, Color> transform = (t) =>
                Color.FromRgba(
                    fromColor.Red + t * (toColor.Red - fromColor.Red),
                    fromColor.Green + t * (toColor.Green - fromColor.Green),
                    fromColor.Blue + t * (toColor.Blue - fromColor.Blue),
                    fromColor.Alpha + t * (toColor.Alpha - fromColor.Alpha));

            var taskCompletionSource = new TaskCompletionSource<bool>();

            self.Animate<Color>("ColorTo", transform, callback, 16, length, easing, 
                (v, c) => taskCompletionSource.SetResult(c));

            return taskCompletionSource.Task;
        }

        /// <summary>
        /// Get all visual descendants of a visual element - useful for finding elements in templates
        /// </summary>
        public static IEnumerable<Element> GetVisualTreeDescendants(this Element element)
        {
            if (element == null)
                yield break;

            foreach (var child in element.LogicalChildren)
            {
                yield return child;

                foreach (var grandchild in GetVisualTreeDescendants(child))
                {
                    yield return grandchild;
                }
            }
        }

        /// <summary>
        /// Checks if a visual element is visible in the current viewport of a scrollable container
        /// </summary>
        public static bool IsVisibleInViewport(this VisualElement element, CollectionView container)
        {
            try
            {
                if (element == null || container == null)
                    return false;
                
                // Get element bounds
                var elementBounds = element.Bounds;
                var containerBounds = container.Bounds;
                
                // Simple visibility check
                bool isElementInViewport = 
                    elementBounds.Top >= containerBounds.Top - elementBounds.Height &&
                    elementBounds.Bottom <= containerBounds.Bottom + elementBounds.Height;
                
                return isElementInViewport;
            }
            catch (Exception)
            {
                // If anything fails, assume it's not visible
                return false;
            }
        }
    }
}
