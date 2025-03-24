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
using NexusChat.Views.Controls;

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
                
                // Add FontAwesome font - make sure the filename matches exactly what's in your Resources/Fonts folder
                fonts.AddFont("FontAwesome-Solid.otf", "FontAwesome-Solid");
                // If you're using other FontAwesome styles, add them too:
                // fonts.AddFont("FontAwesome-Regular.otf", "FontAwesome-Regular");
                // fonts.AddFont("FontAwesome-Brands.otf", "FontAwesome-Brands");
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
        // Register database service
        services.AddSingleton<DatabaseService>();
        
        // Register repositories
        services.AddTransient<IUserRepository, UserRepository>();
        services.AddTransient<IConversationRepository, ConversationRepository>();
        services.AddTransient<IMessageRepository, MessageRepository>();
        
        // Register ViewModels
        services.AddTransient<MainPageViewModel>();
        services.AddTransient<ChatViewModel>();  // Add ChatViewModel registration
        services.AddTransient<ThemesPageViewModel>();
        services.AddTransient<ModelTestingViewModel>();
        services.AddTransient<DatabaseViewerViewModel>();
        
        // Register Pages
        services.AddTransient<MainPage>();
        services.AddTransient<ChatPage>();  // Add ChatPage registration
        services.AddTransient<ThemesPage>();
        services.AddTransient<ModelTestingPage>();
        services.AddTransient<DatabaseViewerPage>();
        
        // Register Services
        services.AddSingleton<IAIService, DummyAIService>();
    }
}
