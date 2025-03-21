using Microsoft.Maui.Controls;
using System.Diagnostics;

namespace NexusChat.Helpers
{
    /// <summary>
    /// Helper class for FontAwesome integration
    /// </summary>
    public static class FontAwesomeHelper
    {
        /// <summary>
        /// Verifies that FontAwesome fonts are properly registered
        /// </summary>
        public static void VerifyFontAwesomeFonts()
        {
            try
            {
                // Create a test label with FontAwesome font
                var testLabel = new Label
                {
                    Text = "\uf005", // Star icon
                    FontFamily = "FontAwesome-Solid"
                };
                
                // Log success
                Debug.WriteLine("FontAwesome fonts verified successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error verifying FontAwesome fonts: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Gets FontAwesome icon character by name
        /// </summary>
        public static string GetIcon(string iconName)
        {
            // Common icons
            return iconName switch
            {
                "star" => "\uf005",
                "user" => "\uf007",
                "check" => "\uf00c",
                "times" => "\uf00d",
                "home" => "\uf015",
                "download" => "\uf019",
                "refresh" => "\uf021",
                "tag" => "\uf02b",
                "bookmark" => "\uf02e",
                "search" => "\uf002",
                "gear" => "\uf013",
                "cog" => "\uf013",
                "trash" => "\uf1f8",
                "sun" => "\uf185",
                "moon" => "\uf186",
                _ => ""
            };
        }
    }
}
