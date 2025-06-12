using System.Collections.Generic;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace NexusChat.Views.Controls
{
    public partial class ApiKeyOverlay : ContentView
    {
        public static readonly BindableProperty IsVisibleProperty =
            BindableProperty.Create(nameof(IsVisible), typeof(bool), typeof(ApiKeyOverlay), false);

        public static readonly BindableProperty ExistingApiKeysProperty =
            BindableProperty.Create(nameof(ExistingApiKeys), typeof(List<string>), typeof(ApiKeyOverlay), 
                new List<string>(), propertyChanged: OnExistingApiKeysChanged);

        public static readonly BindableProperty SaveApiKeyCommandProperty =
            BindableProperty.Create(nameof(SaveApiKeyCommand), typeof(ICommand), typeof(ApiKeyOverlay));

        public static readonly BindableProperty RemoveApiKeyCommandProperty =
            BindableProperty.Create(nameof(RemoveApiKeyCommand), typeof(ICommand), typeof(ApiKeyOverlay));

        public static readonly BindableProperty CloseOverlayCommandProperty =
            BindableProperty.Create(nameof(CloseOverlayCommand), typeof(ICommand), typeof(ApiKeyOverlay));

        public bool IsVisible
        {
            get => (bool)GetValue(IsVisibleProperty);
            set => SetValue(IsVisibleProperty, value);
        }

        public List<string> ExistingApiKeys
        {
            get => (List<string>)GetValue(ExistingApiKeysProperty);
            set => SetValue(ExistingApiKeysProperty, value);
        }

        public ICommand SaveApiKeyCommand
        {
            get => (ICommand)GetValue(SaveApiKeyCommandProperty);
            set => SetValue(SaveApiKeyCommandProperty, value);
        }

        public ICommand RemoveApiKeyCommand
        {
            get => (ICommand)GetValue(RemoveApiKeyCommandProperty);
            set => SetValue(RemoveApiKeyCommandProperty, value);
        }

        public ICommand CloseOverlayCommand
        {
            get => (ICommand)GetValue(CloseOverlayCommandProperty);
            set => SetValue(CloseOverlayCommandProperty, value);
        }

        public ApiKeyOverlay()
        {
            InitializeComponent();
        }

        private static void OnExistingApiKeysChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ApiKeyOverlay overlay && newValue is List<string> newKeys)
            {
                overlay.OnPropertyChanged(nameof(ExistingApiKeys));
            }
        }
    }
}
