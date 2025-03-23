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
                
                // FontAwesome fonts
                fonts.AddFont("fa-regular-400.ttf", "FontAwesome-Regular"); 
                fonts.AddFont("fa-solid-900.ttf", "FontAwesome-Solid");     
                fonts.AddFont("fa-brands-400.ttf", "FontAwesome-Brands");   
            });

        // Register services
        builder.Services.AddSingleton<DatabaseService>();
        
        // Register the startup initializer
        builder.Services.AddSingleton<IStartupInitializer, DatabaseInitializer>();
        
        // Register repositories
        builder.Services.AddSingleton<IUserRepository, UserRepository>();
        builder.Services.AddSingleton<IConversationRepository, ConversationRepository>();
        builder.Services.AddSingleton<IMessageRepository, MessageRepository>();
        
        // Register AI services
        builder.Services.AddSingleton<IAIService, DummyAIService>();
        
        // Register ViewModels
        builder.Services.AddTransient<ChatViewModel>();
        builder.Services.AddTransient<MainPageViewModel>();
        builder.Services.AddTransient<ThemesPageViewModel>();
        builder.Services.AddTransient<ModelTestingViewModel>();
        builder.Services.AddTransient<DatabaseViewerViewModel>();
        
        // Register Pages
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<ChatPage>();
        builder.Services.AddTransient<ThemesPage>();
        builder.Services.AddTransient<ModelTestingPage>();
        builder.Services.AddTransient<DatabaseViewerPage>();
        
        // Register Theme Components
        builder.Services.AddTransient<ColorPalette>();
        builder.Services.AddTransient<Typography>();
        builder.Services.AddTransient<Buttons>();
        builder.Services.AddTransient<FunctionalColors>();
        builder.Services.AddTransient<InputControls>();
        builder.Services.AddTransient<ChatComponents>();
        builder.Services.AddTransient<StatusIndicators>();
        builder.Services.AddTransient<LayoutComponents>();
        builder.Services.AddTransient<FormComponents>();
        builder.Services.AddTransient<Accessibilities>();
        builder.Services.AddTransient<Icons>(); // Add Icons control registration
        
#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
