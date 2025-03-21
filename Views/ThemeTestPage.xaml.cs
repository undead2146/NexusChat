using Microsoft.Maui.Controls;
using NexusChat.ViewModels;
using CommunityToolkit.Mvvm.Messaging;

namespace NexusChat.Views
{
    public partial class ThemeTestPage : ContentPage
    {
        public ThemeTestPage()
        {
            InitializeComponent();
            BindingContext = new ThemeTestPageViewModel();
        }
        
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            (BindingContext as IDisposable)?.Dispose();
        }
    }
}
