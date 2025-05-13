using System;
using System.Diagnostics;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace NexusChat.Views.Controls
{
    /// <summary>
    /// Reusable header component for chat interfaces with consistent styling
    /// </summary>
    public partial class ChatHeader : ContentView
    {
        /// <summary>
        /// Title text bindable property
        /// </summary>
        public static readonly BindableProperty TitleProperty =
            BindableProperty.Create(nameof(Title), typeof(string), typeof(ChatHeader), string.Empty,
                propertyChanged: OnTitlePropertyChanged);

        /// <summary>
        /// Subtitle text (model name) bindable property
        /// </summary>
        public static readonly BindableProperty CurrentModelNameProperty =
            BindableProperty.Create(nameof(CurrentModelName), typeof(string), typeof(ChatHeader), string.Empty,
                propertyChanged: OnCurrentModelNamePropertyChanged);

        /// <summary>
        /// Menu command bindable property
        /// </summary>
        public static readonly BindableProperty MenuCommandProperty =
            BindableProperty.Create(nameof(MenuCommand), typeof(ICommand), typeof(ChatHeader), null,
                propertyChanged: OnMenuCommandPropertyChanged);

        /// <summary>
        /// Menu command parameter bindable property
        /// </summary>
        public static readonly BindableProperty MenuCommandParameterProperty =
            BindableProperty.Create(nameof(MenuCommandParameter), typeof(object), typeof(ChatHeader), null);

        /// <summary>
        /// Switch model command bindable property
        /// </summary>
        public static readonly BindableProperty SwitchModelCommandProperty =
            BindableProperty.Create(nameof(SwitchModelCommand), typeof(ICommand), typeof(ChatHeader), null,
                propertyChanged: OnSwitchModelCommandPropertyChanged);

        /// <summary>
        /// Switch model command parameter bindable property
        /// </summary>
        public static readonly BindableProperty SwitchModelCommandParameterProperty =
            BindableProperty.Create(nameof(SwitchModelCommandParameter), typeof(object), typeof(ChatHeader), null);

        /// <summary>
        /// Options command bindable property
        /// </summary>
        public static readonly BindableProperty OptionsCommandProperty =
            BindableProperty.Create(nameof(OptionsCommand), typeof(ICommand), typeof(ChatHeader), null,
                propertyChanged: OnOptionsCommandPropertyChanged);

        /// <summary>
        /// Options command parameter bindable property
        /// </summary>
        public static readonly BindableProperty OptionsCommandParameterProperty =
            BindableProperty.Create(nameof(OptionsCommandParameter), typeof(object), typeof(ChatHeader), null);

        /// <summary>
        /// Shows/hides the model switcher button
        /// </summary>
        public static readonly BindableProperty ShowModelSwitcherProperty =
            BindableProperty.Create(nameof(ShowModelSwitcher), typeof(bool), typeof(ChatHeader), true,
                propertyChanged: OnShowModelSwitcherPropertyChanged);

        /// <summary>
        /// Shows/hides the options button
        /// </summary>
        public static readonly BindableProperty ShowOptionsButtonProperty =
            BindableProperty.Create(nameof(ShowOptionsButton), typeof(bool), typeof(ChatHeader), true,
                propertyChanged: OnShowOptionsButtonPropertyChanged);

        /// <summary>
        /// Gets or sets the title text
        /// </summary>
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        /// <summary>
        /// Gets or sets the current model name for subtitle
        /// </summary>
        public string CurrentModelName
        {
            get => (string)GetValue(CurrentModelNameProperty);
            set => SetValue(CurrentModelNameProperty, value);
        }

        /// <summary>
        /// Gets or sets the menu command
        /// </summary>
        public ICommand MenuCommand
        {
            get => (ICommand)GetValue(MenuCommandProperty);
            set => SetValue(MenuCommandProperty, value);
        }

        /// <summary>
        /// Gets or sets the menu command parameter
        /// </summary>
        public object MenuCommandParameter
        {
            get => GetValue(MenuCommandParameterProperty);
            set => SetValue(MenuCommandParameterProperty, value);
        }

        /// <summary>
        /// Gets or sets the switch model command
        /// </summary>
        public ICommand SwitchModelCommand
        {
            get => (ICommand)GetValue(SwitchModelCommandProperty);
            set => SetValue(SwitchModelCommandProperty, value);
        }

        /// <summary>
        /// Gets or sets the switch model command parameter
        /// </summary>
        public object SwitchModelCommandParameter
        {
            get => GetValue(SwitchModelCommandParameterProperty);
            set => SetValue(SwitchModelCommandParameterProperty, value);
        }

        /// <summary>
        /// Gets or sets the options command
        /// </summary>
        public ICommand OptionsCommand
        {
            get => (ICommand)GetValue(OptionsCommandProperty);
            set => SetValue(OptionsCommandProperty, value);
        }

        /// <summary>
        /// Gets or sets the options command parameter
        /// </summary>
        public object OptionsCommandParameter
        {
            get => GetValue(OptionsCommandParameterProperty);
            set => SetValue(OptionsCommandParameterProperty, value);
        }

        /// <summary>
        /// Gets or sets whether to show the model switcher button
        /// </summary>
        public bool ShowModelSwitcher
        {
            get => (bool)GetValue(ShowModelSwitcherProperty);
            set => SetValue(ShowModelSwitcherProperty, value);
        }

        /// <summary>
        /// Gets or sets whether to show the options button
        /// </summary>
        public bool ShowOptionsButton
        {
            get => (bool)GetValue(ShowOptionsButtonProperty);
            set => SetValue(ShowOptionsButtonProperty, value);
        }

        /// <summary>
        /// Initialize component
        /// </summary>
        public ChatHeader()
        {
            InitializeComponent();

            // Set up click events for buttons
            MenuButton.Clicked += OnMenuButtonClicked;
            SwitchModelButton.Clicked += OnSwitchModelButtonClicked;
            OptionsButton.Clicked += OnOptionsButtonClicked;
            
            // Add tap gesture handler for model badge
            ModelBadgeTapGesture.Tapped += OnModelBadgeTapped;
        }

        private static void OnTitlePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ChatHeader header)
            {
                header.HeaderLabel.Text = newValue as string;
            }
        }

        private static void OnCurrentModelNamePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ChatHeader header)
            {
                string modelName = newValue as string;
                bool hasModel = !string.IsNullOrEmpty(modelName);
                
                // Update model badge
                header.ModelLabel.Text = modelName;
                header.ModelBadge.IsVisible = hasModel;
            }
        }

        private static void OnMenuCommandPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ChatHeader header)
            {
                header.MenuButton.IsEnabled = newValue != null;
            }
        }

        private static void OnSwitchModelCommandPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ChatHeader header)
            {
                header.SwitchModelButton.IsEnabled = newValue != null;
            }
        }

        private static void OnOptionsCommandPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ChatHeader header)
            {
                header.OptionsButton.IsEnabled = newValue != null;
            }
        }

        private static void OnShowModelSwitcherPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ChatHeader header && newValue is bool showSwitcher)
            {
                header.SwitchModelButton.IsVisible = showSwitcher;
            }
        }

        private static void OnShowOptionsButtonPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ChatHeader header && newValue is bool showOptions)
            {
                header.OptionsButton.IsVisible = showOptions;
            }
        }

        private void OnMenuButtonClicked(object sender, EventArgs e)
        {
            if (MenuCommand?.CanExecute(MenuCommandParameter) == true)
            {
                MenuCommand.Execute(MenuCommandParameter);
            }
        }

        private void OnSwitchModelButtonClicked(object sender, EventArgs e)
        {
            if (SwitchModelCommand?.CanExecute(SwitchModelCommandParameter) == true)
            {
                SwitchModelCommand.Execute(SwitchModelCommandParameter);
            }
        }

        private void OnOptionsButtonClicked(object sender, EventArgs e)
        {
            if (OptionsCommand?.CanExecute(OptionsCommandParameter) == true)
            {
                OptionsCommand.Execute(OptionsCommandParameter);
            }
        }

        private void OnModelBadgeTapped(object sender, EventArgs e)
        {
            Debug.WriteLine("Model badge tapped");
            if (SwitchModelCommand?.CanExecute(SwitchModelCommandParameter) == true)
            {
                SwitchModelCommand.Execute(SwitchModelCommandParameter);
            }
        }
    }
}
