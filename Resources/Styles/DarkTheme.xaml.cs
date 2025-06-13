using System;
using Microsoft.Maui.Controls;

using Microsoft.Maui.Controls.Xaml;

namespace NexusChat.Resources.Styles
{
    public partial class DarkTheme : ResourceDictionary
    {
        public DarkTheme()
        {
            try
            {
                InitializeComponent();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing DarkTheme: {ex.Message}");
                // Add fallback resources programmatically
                this["Primary"] = Color.FromArgb("#7B68EE");
                this["Background"] = Color.FromArgb("#121212");
                this["CardBackground"] = Color.FromArgb("#252525");
                this["PrimaryTextColor"] = Colors.White;
                this["SecondaryTextColor"] = Color.FromArgb("#B0B0B0");
            }
        }
    }
}
