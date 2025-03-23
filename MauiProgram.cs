using Microsoft.Extensions.Logging;
using NexusChat.Data;
using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Maui;
using NexusChat.Views;
using NexusChat.ViewModels;

namespace NexusChat;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit() // Required for EventToCommandBehavior
            .ConfigureFonts(fonts =>
            {
                // Open Sans fonts
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemiBold");
                fonts.AddFont("OpenSans-Bold.ttf", "OpenSansBold");
                
                // FontAwesome fonts
                fonts.AddFont("fa-regular-400.ttf", "FontAwesome-Regular"); 
                fonts.AddFont("fa-solid-900.ttf", "FontAwesome-Solid");     
                fonts.AddFont("fa-brands-400.ttf", "FontAwesome-Brands");   
            });

        // Register services as singletons to ensure they are properly shared
        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddSingleton<IStartupInitializer, DatabaseInitializer>();
        
        // Register ViewModels
        builder.Services.AddTransient<DatabaseViewerViewModel>();
        builder.Services.AddTransient<ModelTestingViewModel>();
        
        // Register Views
        builder.Services.AddTransient<DatabaseViewerPage>();
        builder.Services.AddTransient<ModelTestingPage>();
        
#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
