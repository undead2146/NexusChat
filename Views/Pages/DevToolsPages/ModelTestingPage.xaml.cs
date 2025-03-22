using Microsoft.Maui.Controls;
using NexusChat.ViewModels;
using NexusChat.Data;

namespace NexusChat.Views
{
    /// <summary>
    /// Page for testing models and generating random test data
    /// </summary>
    public partial class ModelTestingPage : ContentPage
    {
        /// <summary>
        /// Initializes a new instance of the ModelTestingPage
        /// </summary>
        /// <param name="databaseService">Database service for accessing data</param>
        public ModelTestingPage(DatabaseService databaseService)
        {
            InitializeComponent();
            BindingContext = new ModelTestingViewModel(databaseService);
        }
        
        /// <summary>
        /// Default constructor for design-time support
        /// </summary>
        public ModelTestingPage()
        {
            InitializeComponent();
            // Design-time constructor
        }
    }
}
