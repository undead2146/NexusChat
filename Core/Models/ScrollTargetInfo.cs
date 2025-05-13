using System;

namespace NexusChat.Core.Models
{
    /// <summary>
    /// Class that provides scroll targeting information
    /// </summary>
    public class ScrollTargetInfo
    {
        /// <summary>
        /// Gets or sets whether to scroll to the bottom
        /// </summary>
        public bool ScrollToBottom { get; set; }
        
        /// <summary>
        /// Gets or sets whether to animate the scroll
        /// </summary>
        public bool ShouldAnimate { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the specific index to scroll to (if not scrolling to bottom)
        /// </summary>
        public int? ScrollToIndex { get; set; }

        /// <summary>
        /// Creates a new instance for scrolling to bottom
        /// </summary>
        /// <param name="animate">Whether to animate the scroll</param>
        /// <returns>A ScrollTargetInfo configured for bottom scrolling</returns>
        public static ScrollTargetInfo CreateBottomScroll(bool animate = true)
        {
            return new ScrollTargetInfo
            {
                ScrollToBottom = true,
                ShouldAnimate = animate,
                ScrollToIndex = null
            };
        }

        /// <summary>
        /// Creates a new instance for scrolling to a specific index
        /// </summary>
        /// <param name="index">The index to scroll to</param>
        /// <param name="animate">Whether to animate the scroll</param>
        /// <returns>A ScrollTargetInfo configured for index scrolling</returns>
        public static ScrollTargetInfo CreateIndexScroll(int index, bool animate = true)
        {
            return new ScrollTargetInfo
            {
                ScrollToBottom = false,
                ShouldAnimate = animate,
                ScrollToIndex = index
            };
        }
    }
}
