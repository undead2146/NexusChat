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
using NexusChat.Services.AIProviders.Implementations;
using NexusChat.Services.AIManagement;
using NexusChat.Services;
using NexusChat.Services.ApiKeyManagement;
using NexusChat.Data.Interfaces;

namespace NexusChat;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit() // Required for EventToCommandBehavior
            .ConfigureMauiHandlers(handlers =>
            {
                // Remove specific handler registration for CollectionView - not needed
            })
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
        services.AddSingleton<INavigationService, NavigationService>();
        
        services.AddSingleton<IAIService, DummyAIService>();
        services.AddSingleton<IAIServiceFactory, AIServiceFactory>();
        services.AddSingleton<IModelManager, ModelManager>();
        services.AddSingleton<IApiKeyManager, ApiKeyManager>();
        services.AddSingleton<IModelLoaderService, ModelLoaderService>();
        services.AddSingleton<IEnvironmentService, EnvFileInitializer>();
        
        // Add memory cache for AIServiceFactory
        services.AddMemoryCache();
        
        // Register AIServiceFactory as singleton
        services.AddSingleton<IAIServiceFactory, AIServiceFactory>();
        
        // Add as startup initializer
        services.AddSingleton<IStartupInitializer>(provider =>
            (IStartupInitializer)provider.GetRequiredService<IAIServiceFactory>());
        
        // Register database-related services
        services.AddSingleton<DatabaseService>();
        
        // Register startup initializers
        services.AddSingleton<IStartupInitializer, DatabaseInitializer>();
        services.AddSingleton<IStartupInitializer, EnvFileInitializer>();
        
        // Register repositories
        services.AddTransient<IUserRepository, UserRepository>();
        services.AddTransient<IConversationRepository, ConversationRepository>();
        services.AddTransient<IMessageRepository, MessageRepository>();
        services.AddTransient<IAIModelRepository, AIModelRepository>();
        services.AddTransient<IModelConfigurationRepository, ModelConfigurationRepository>();
        
        // Register ViewModels
        services.AddTransient<MainPageViewModel>();
        services.AddTransient<ChatViewModel>();
        services.AddTransient<ThemesPageViewModel>();
        services.AddTransient<ModelTestingViewModel>();
        services.AddTransient<DatabaseViewerViewModel>();
        services.AddTransient<AIModelsViewModel>();
        // No ViewModel for DebugLogAnalyzerPage as it's relatively simple
        
        // Register Pages
        services.AddTransient<MainPage>();
        services.AddTransient<ChatPage>();
        services.AddTransient<ThemesPage>();
        services.AddTransient<ModelTestingPage>();
        services.AddTransient<DatabaseViewerPage>();
        services.AddTransient<AIModelsPage>();
        
        // Register service implementations
        services.AddTransient<GroqAIService>();
        services.AddTransient<OpenRouterAIService>();
        services.AddTransient<DummyAIService>();
        // services.AddTransient<AzureAIService>(); // Add Azure AI service
    }
}
