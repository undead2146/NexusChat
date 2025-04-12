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
            .UseMauiCommunityToolkit() 
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
        
        // Add memory cache for services that need it
        services.AddMemoryCache();
        
        // Register database service first (needed by other services)
        services.AddSingleton<DatabaseService>();
        
        // Register environment and model loading services first
        services.AddSingleton<IEnvironmentService, EnvFileInitializer>();
        services.AddSingleton<IModelLoaderService, ModelLoaderService>();
        
        // Register AI services
        services.AddSingleton<IAIServiceFactory, AIServiceFactory>();
        services.AddSingleton<IAIService, DummyAIService>();
        services.AddSingleton<IApiKeyManager, ApiKeyManager>();
        
        // Register ModelManager with proper dependency order
        services.AddSingleton<IModelManager, ModelManager>();
        
        // Register startup initializers
        services.AddSingleton<IStartupInitializer, DatabaseInitializer>();
        services.AddSingleton<IStartupInitializer, EnvFileInitializer>();
        
        // Register service instances that implement IStartupInitializer explicitly
        // This handles the IAIServiceFactory registration by using a factory method
        services.AddSingleton<IStartupInitializer>(sp => 
            (IStartupInitializer)(sp.GetService<IAIServiceFactory>()));
        
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
        
        // Register Pages
        services.AddTransient<MainPage>();
        services.AddTransient<ChatPage>();
        services.AddTransient<ThemesPage>();
        services.AddTransient<ModelTestingPage>();
        services.AddTransient<DatabaseViewerPage>();
        services.AddTransient<AIModelsPage>();
        
        // Register AI service implementations
        services.AddTransient<GroqAIService>();
        services.AddTransient<OpenRouterAIService>();
        services.AddTransient<DummyAIService>();
        // services.AddTransient<AzureAIService>(); // Add Azure AI service
    }
}
