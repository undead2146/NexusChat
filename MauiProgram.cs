using Microsoft.Extensions.Logging;
using NexusChat.Data.Context;
using CommunityToolkit.Maui;
using NexusChat.Views.Pages.DevTools;
using NexusChat.Views.Pages;
using NexusChat.Core.ViewModels.DevTools;
using NexusChat.Core.ViewModels;
using NexusChat.Data.Repositories;
using NexusChat.Services.Interfaces;
using NexusChat.Services.AIProviders;
using NexusChat.Services;

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
                
                // Add FontAwesome font
                fonts.AddFont("FontAwesome-Solid.otf", "FontAwesome-Solid");
            });

        // Register services
        RegisterServices(builder.Services);
        
#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
    
    private static void RegisterServices(IServiceCollection services)
    {
        // Register core services
        services.AddSingleton<NavigationService>();
        services.AddSingleton<DatabaseService>();
        services.AddSingleton<IAIService, DummyAIService>();
        
        // Register startup initializers
        services.AddSingleton<IStartupInitializer, DatabaseInitializer>();
        
        // Register repositories
        services.AddTransient<IUserRepository, UserRepository>();
        services.AddTransient<IConversationRepository, ConversationRepository>();
        services.AddTransient<IMessageRepository, MessageRepository>();
        
        // Register ViewModels
        services.AddTransient<MainPageViewModel>();
        services.AddTransient<ChatViewModel>();
        services.AddTransient<ThemesPageViewModel>();
        services.AddTransient<ModelTestingViewModel>();
        services.AddTransient<DatabaseViewerViewModel>();
        
        // Register Pages
        services.AddTransient<App>();
        services.AddTransient<AppShell>();
        services.AddTransient<MainPage>();
        services.AddTransient<ChatPage>();
        services.AddTransient<ThemesPage>();
        services.AddTransient<ModelTestingPage>();
        services.AddTransient<DatabaseViewerPage>();
    }
}
