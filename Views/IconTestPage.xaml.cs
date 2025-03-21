using System.Text;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace NexusChat.Views {
    public partial class IconTestPage : ContentPage {
        public IconTestPage() {
            InitializeComponent();
            DisplayFontInfo();
        }

        private async void OnBackClicked(object sender, EventArgs e) {
            await Shell.Current.GoToAsync("..");
        }

        private void DisplayFontInfo() {
            try {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Available fonts:");

                // Safely get default font info
                string defaultFont = "Default font unknown";
                try {
                    defaultFont = Microsoft.Maui.Font.Default.ToString();
                } catch {
                    defaultFont = "(Error getting default font)";
                }
                sb.AppendLine($"Default font: {defaultFont}");

                // Get registered fonts for testing
                sb.AppendLine("\nRegistered fonts for testing:");
                var fontsForTesting = new[]
                {
                    "FontAwesome-Solid",
                    "FontAwesome-Regular",
                    "FontAwesome-Brands",
                    "OpenSansRegular",
                    "OpenSansSemiBold",
                    "OpenSansBold"
                };

                foreach (var font in fontsForTesting) {
                    sb.AppendLine($"- {font}");
                }

                // Check if any fonts are actually loaded
                sb.AppendLine("\nFont availability check:");
                sb.AppendLine("If icons show below, fonts are working:");
                
                FontInfoLabel.Text = sb.ToString();

                // Try to fill in some actual loaded font data by showing them
                Label1.FontFamily = "FontAwesome-Solid";
                Label2.FontFamily = "OpenSansRegular";
                Label3.FontFamily = "OpenSansBold";
            }
            catch (Exception ex) {
                FontInfoLabel.Text = $"Error: {ex.Message}";
            }
        }
    }
}
