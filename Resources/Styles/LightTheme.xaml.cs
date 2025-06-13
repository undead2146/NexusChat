using Microsoft.Maui.Controls;

namespace NexusChat.Resources.Styles
{
    public partial class LightTheme : ResourceDictionary
    {
        public LightTheme()
        {
            try
            {
                InitializeComponent();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing LightTheme: {ex.Message}");
                // Add fallback resources programmatically
                this["Primary"] = Color.FromArgb("#512BD4");
                this["Background"] = Colors.White;
                this["CardBackground"] = Colors.White;
                this["PrimaryTextColor"] = Colors.Black;
                this["SecondaryTextColor"] = Color.FromArgb("#616161");
            }
        }
    }
}
