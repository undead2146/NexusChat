using System;
using Microsoft.Maui.Controls;
using NexusChat.Core.ViewModels.DevTools;

namespace NexusChat.Views.Controls.Theme
{
    /// <summary>
    /// Component that displays Font Awesome icons
    /// </summary>
    public partial class Icons : ContentView, IDisposable
    {
        public Icons()
        {
            InitializeComponent();
        }
        
        /// <summary>
        /// Cleans up resources
        /// </summary>
        public void Dispose()
        {
            // Release any resources if needed
        }
    }
}
