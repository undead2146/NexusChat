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
using Microsoft.Extensions.DependencyInjection;

namespace NexusChat;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            // Add the CommunityToolkit.Maui
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                // Open Sans fonts
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemiBold");
                fonts.AddFont("OpenSans-Bold.ttf", "OpenSansBold");
                
                // Add FontAwesome font
                fonts.AddFont("FontAwesome-Solid.otf", "FontAwesome-Solid");
            });

        // Register services and view models
        builder.Services.AddSingleton<Data.Context.DatabaseService>();

        // Register repositories
        builder.Services.AddSingleton<Data.Interfaces.IAIModelRepository, Data.Repositories.AIModelRepository>();
        builder.Services.AddSingleton<Data.Interfaces.IConversationRepository, Data.Repositories.ConversationRepository>();
        builder.Services.AddSingleton<Data.Interfaces.IMessageRepository, Data.Repositories.MessageRepository>();

        // Register API key management
        builder.Services.AddSingleton<NexusChat.Services.Interfaces.IApiKeyProvider, NexusChat.Services.ApiKeyManagement.ApiKeyProvider>();
        builder.Services.AddSingleton<NexusChat.Services.Interfaces.IApiKeyManager, NexusChat.Services.ApiKeyManagement.ApiKeyManager>();

        // Register AI services
        builder.Services.AddSingleton<NexusChat.Services.Interfaces.IAIProviderFactory, NexusChat.Services.AIProviders.AIProviderFactory>();
        builder.Services.AddTransient<NexusChat.Services.AIProviders.Implementations.GroqAIService>();
        builder.Services.AddTransient<NexusChat.Services.AIProviders.Implementations.OpenRouterAIService>();
        builder.Services.AddTransient<NexusChat.Services.AIProviders.Implementations.DummyAIService>();
        builder.Services.AddTransient<NexusChat.Services.Interfaces.IAIProviderService, NexusChat.Services.AIProviders.Implementations.DummyAIService>(); // Default IAIProviderService implementation

        // Register model management
        builder.Services.AddSingleton<NexusChat.Services.Interfaces.IAIModelManager, NexusChat.Services.AIManagement.AIModelManager>();

        // Register chat service
        builder.Services.AddSingleton<NexusChat.Services.ChatService>();
        builder.Services.AddSingleton<IChatService, ChatService>();

        // Register view models
        builder.Services.AddTransient<NexusChat.Core.ViewModels.MainPageViewModel>();
        builder.Services.AddTransient<NexusChat.Core.ViewModels.AIModelsViewModel>();
        builder.Services.AddTransient<NexusChat.Core.ViewModels.ChatViewModel>();
        builder.Services.AddTransient<ConversationsPageViewModel>();

        // Register pages
        builder.Services.AddTransient<NexusChat.Views.Pages.MainPage>();
        builder.Services.AddTransient<NexusChat.Views.Pages.AIModelsPage>();
        builder.Services.AddTransient<NexusChat.Views.Pages.ChatPage>();
        builder.Services.AddTransient<ConversationsPage>();

        // Make sure the navigation service is registered
        builder.Services.AddSingleton<INavigationService, NavigationService>();

        RegisterServices(builder.Services);
        RegisterViewModels(builder.Services);
        RegisterPages(builder.Services);
        RegisterRoutes();
        
#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
    
    private static void RegisterViewModels(IServiceCollection services)
    {
        // Register view models
        services.AddTransient<MainPageViewModel>();
        services.AddTransient<ChatViewModel>();
        services.AddTransient<AIModelsViewModel>();
        services.AddTransient<ConversationsPageViewModel>();
    }

    private static void RegisterServices(IServiceCollection services)
    {
        // Register required services
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<ChatService>();
        services.AddSingleton<IChatService, ChatService>();
        
        // Register transient services for AI providers
        // These need to be transient since they're created on demand with different model parameters
        services.AddTransient<GroqAIService>();
        services.AddTransient<OpenRouterAIService>();
        services.AddTransient<DummyAIService>();
        
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddSingleton<DatabaseService>();
        services.AddMemoryCache();
        
        // Register database service first (needed by repositories)
        services.AddSingleton<DatabaseService>();
        

        // Register API key management services
        services.AddSingleton<IApiKeyProvider, ApiKeyProvider>();
        services.AddSingleton<IApiKeyManager, ApiKeyManager>();
        
        // Register model management services
        services.AddSingleton<IAIModelRepository, AIModelRepository>();
        
        // Register AI services
        services.AddSingleton<IAIProviderFactory, AIProviderFactory>();
        services.AddSingleton<IAIModelManager, AIModelManager>();
    }

    private static void RegisterPages(IServiceCollection services)
    {
        services.AddTransient<MainPage>();
        services.AddTransient<ChatPage>();
        services.AddTransient<AIModelsPage>();
        services.AddTransient<ConversationsPage>();
    }

    private static void RegisterRoutes()
    {
        Routing.RegisterRoute(nameof(ChatPage), typeof(ChatPage));
    }
}
