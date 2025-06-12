using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace NexusChat.Views.Controls
{
    public partial class AIModelsHeader : ContentView
    {
        public static readonly BindableProperty SearchTextProperty =
            BindableProperty.Create(nameof(SearchText), typeof(string), typeof(AIModelsHeader), string.Empty, BindingMode.TwoWay);

        public static readonly BindableProperty ShowFavoritesOnlyProperty =
            BindableProperty.Create(nameof(ShowFavoritesOnly), typeof(bool), typeof(AIModelsHeader), false);

        public static readonly BindableProperty RefreshButtonRotationProperty =
            BindableProperty.Create(nameof(RefreshButtonRotation), typeof(double), typeof(AIModelsHeader), 0.0);

        public static readonly BindableProperty GoBackCommandProperty =
            BindableProperty.Create(nameof(GoBackCommand), typeof(ICommand), typeof(AIModelsHeader));

        public static readonly BindableProperty FilterModelsCommandProperty =
            BindableProperty.Create(nameof(FilterModelsCommand), typeof(ICommand), typeof(AIModelsHeader));

        public static readonly BindableProperty ToggleFavoritesFilterCommandProperty =
            BindableProperty.Create(nameof(ToggleFavoritesFilterCommand), typeof(ICommand), typeof(AIModelsHeader));

        public static readonly BindableProperty RefreshModelsCommandProperty =
            BindableProperty.Create(nameof(RefreshModelsCommand), typeof(ICommand), typeof(AIModelsHeader));

        public string SearchText
        {
            get => (string)GetValue(SearchTextProperty);
            set => SetValue(SearchTextProperty, value);
        }

        public bool ShowFavoritesOnly
        {
            get => (bool)GetValue(ShowFavoritesOnlyProperty);
            set => SetValue(ShowFavoritesOnlyProperty, value);
        }

        public double RefreshButtonRotation
        {
            get => (double)GetValue(RefreshButtonRotationProperty);
            set => SetValue(RefreshButtonRotationProperty, value);
        }

        public ICommand GoBackCommand
        {
            get => (ICommand)GetValue(GoBackCommandProperty);
            set => SetValue(GoBackCommandProperty, value);
        }

        public ICommand FilterModelsCommand
        {
            get => (ICommand)GetValue(FilterModelsCommandProperty);
            set => SetValue(FilterModelsCommandProperty, value);
        }

        public ICommand ToggleFavoritesFilterCommand
        {
            get => (ICommand)GetValue(ToggleFavoritesFilterCommandProperty);
            set => SetValue(ToggleFavoritesFilterCommandProperty, value);
        }

        public ICommand RefreshModelsCommand
        {
            get => (ICommand)GetValue(RefreshModelsCommandProperty);
            set => SetValue(RefreshModelsCommandProperty, value);
        }

        public AIModelsHeader()
        {
            InitializeComponent();
        }
    }
}
