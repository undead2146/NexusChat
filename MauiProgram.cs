using Microsoft.Extensions.Logging;
using NexusChat.Data; // We'll create this shortly
using Microsoft.Extensions.DependencyInjection;

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

        // Register database and repositories
        builder.Services.AddSingleton<DatabaseService>();

        // Register SQLite initializer
        builder.Services.AddTransient<IStartupInitializer, DatabaseInitializer>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
