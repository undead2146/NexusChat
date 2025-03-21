using Microsoft.Extensions.Logging;

namespace NexusChat;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                // Open Sans fonts
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemiBold");
                fonts.AddFont("OpenSans-Bold.ttf", "OpenSansBold");
                
                // FontAwesome fonts
                fonts.AddFont("fa-regular-400.ttf", "FontAwesome-Regular");  // Make sure filename matches exactly
                fonts.AddFont("fa-solid-900.ttf", "FontAwesome-Solid");      // Make sure filename matches exactly
                fonts.AddFont("fa-brands-400.ttf", "FontAwesome-Brands");    // Make sure filename matches exactly
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
