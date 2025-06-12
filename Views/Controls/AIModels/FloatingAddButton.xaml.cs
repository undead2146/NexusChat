using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace NexusChat.Views.Controls
{
    public partial class FloatingAddButton : ContentView
    {
        public static readonly BindableProperty IsVisibleProperty =
            BindableProperty.Create(nameof(IsVisible), typeof(bool), typeof(FloatingAddButton), true);

        public static readonly BindableProperty CommandProperty =
            BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(FloatingAddButton));

        public bool IsVisible
        {
            get => (bool)GetValue(IsVisibleProperty);
            set => SetValue(IsVisibleProperty, value);
        }

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public FloatingAddButton()
        {
            InitializeComponent();
        }
    }
}
