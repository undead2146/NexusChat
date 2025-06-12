using System.Windows.Input;
using Microsoft.Maui.Controls;
using NexusChat.Core.Models;

namespace NexusChat.Views.Controls
{
    public partial class AIModelItem : ContentView
    {
        public static readonly BindableProperty ModelProperty =
            BindableProperty.Create(nameof(Model), typeof(AIModel), typeof(AIModelItem));

        public static readonly BindableProperty SelectModelCommandProperty =
            BindableProperty.Create(nameof(SelectModelCommand), typeof(ICommand), typeof(AIModelItem));

        public static readonly BindableProperty ToggleFavoriteCommandProperty =
            BindableProperty.Create(nameof(ToggleFavoriteCommand), typeof(ICommand), typeof(AIModelItem));

        public static readonly BindableProperty SetDefaultModelCommandProperty =
            BindableProperty.Create(nameof(SetDefaultModelCommand), typeof(ICommand), typeof(AIModelItem));

        public static readonly BindableProperty ShowModelInfoCommandProperty =
            BindableProperty.Create(nameof(ShowModelInfoCommand), typeof(ICommand), typeof(AIModelItem));

        public AIModel Model
        {
            get => (AIModel)GetValue(ModelProperty);
            set => SetValue(ModelProperty, value);
        }

        public ICommand SelectModelCommand
        {
            get => (ICommand)GetValue(SelectModelCommandProperty);
            set => SetValue(SelectModelCommandProperty, value);
        }

        public ICommand ToggleFavoriteCommand
        {
            get => (ICommand)GetValue(ToggleFavoriteCommandProperty);
            set => SetValue(ToggleFavoriteCommandProperty, value);
        }

        public ICommand SetDefaultModelCommand
        {
            get => (ICommand)GetValue(SetDefaultModelCommandProperty);
            set => SetValue(SetDefaultModelCommandProperty, value);
        }

        public ICommand ShowModelInfoCommand
        {
            get => (ICommand)GetValue(ShowModelInfoCommandProperty);
            set => SetValue(ShowModelInfoCommandProperty, value);
        }

        public AIModelItem()
        {
            InitializeComponent();
        }
    }
}
