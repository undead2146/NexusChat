using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace NexusChat.Helpers
{
    /// <summary>
    /// Helper class for common animations
    /// </summary>
    public static class AnimationHelpers
    {
        /// <summary>
        /// Shows the sidebar with animation
        /// </summary>
        public static async Task ShowSidebarAsync(View sidebar, View mainContent, View overlay, uint duration = 250)
        {
            // Make overlay visible first
            overlay.IsVisible = true;
            
            // Create and run parallel animations
            var tasks = new[]
            {
                sidebar.TranslateTo(0, 0, duration, Easing.CubicOut),
                mainContent.TranslateTo(300, 0, duration, Easing.CubicOut),
                overlay.FadeTo(0.4, duration, Easing.CubicOut)
            };
            
            await Task.WhenAll(tasks);
        }
        
        /// <summary>
        /// Hides the sidebar with animation
        /// </summary>
        public static async Task HideSidebarAsync(View sidebar, View mainContent, View overlay, uint duration = 250)
        {
            // Create and run parallel animations
            var tasks = new[]
            {
                sidebar.TranslateTo(-300, 0, duration, Easing.CubicIn),
                mainContent.TranslateTo(0, 0, duration, Easing.CubicIn),
                overlay.FadeTo(0, duration, Easing.CubicIn)
            };
            
            await Task.WhenAll(tasks);
            
            // Hide overlay after animation completes
            overlay.IsVisible = false;
        }
    }
}
