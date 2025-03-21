using System.Diagnostics;
using Microsoft.Maui.Platform;

namespace NexusChat.Helpers {
    public static class FontAwesomeHelper {
        public static void VerifyFontAwesomeFonts() {
            try {
                // Define expected font families
                var expectedFonts = new[]
                {
                    "FontAwesome-Solid",
                    "FontAwesome-Regular",
                    "FontAwesome-Brands"
                };

                Debug.WriteLine("===== Checking Font Awesome Fonts =====");

                // Check each required font
                foreach (var font in expectedFonts) {
                    bool fontExists = Microsoft.Maui.Font.Default.Family.ToString().Contains(font, StringComparison.OrdinalIgnoreCase);
                    Debug.WriteLine($"Font {font}: {(fontExists ? "Found" : "Not Found")}");

                    if (!fontExists) {
                        Debug.WriteLine($"WARNING: {font} not found. Ensure it's properly registered in MauiProgram.cs");
                    }
                }

                // Additional verification message
                Debug.WriteLine("\nMake sure these fonts are:");
                Debug.WriteLine("1. Located in Resources/Fonts/");
                Debug.WriteLine("2. Have Build Action: MauiFont");
                Debug.WriteLine("3. Registered in MauiProgram.cs using builder.ConfigureFonts()");
            }
            catch (Exception ex) {
                Debug.WriteLine($"Error checking FontAwesome fonts: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
