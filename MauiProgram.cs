using Microsoft.Extensions.Logging;
using NexusChat.Data.Context;
using CommunityToolkit.Maui;
using NexusChat.Views.Pages;
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
        
        // Load environment variables at startup
        LoadEnvironmentVariables();
        
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemiBold");
                fonts.AddFont("OpenSans-Bold.ttf", "OpenSansBold");
                fonts.AddFont("fa-solid-900.ttf", "FontAwesome-Solid");
            });

        // Register Services
        RegisterServices(builder.Services);

        // Register ViewModels
        RegisterViewModels(builder.Services);

        // Register Pages
        RegisterPages(builder.Services);

        // Routes are registered in AppShell.xaml.cs - no need for duplicate registration here

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    private static void LoadEnvironmentVariables()
    {
        try
        {
            // Centralized .env loading - check multiple common locations
            var possiblePaths = new[]
            {
                ".env",
                "../.env",
                "../../.env",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".env"),
                Path.Combine(FileSystem.AppDataDirectory, ".env")
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    DotNetEnv.Env.Load(path);
                    System.Diagnostics.Debug.WriteLine($"Loaded .env from: {path}");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load .env file: {ex.Message}");
        }
    }

    private static void RegisterServices(IServiceCollection services)
    {
        // Singletons - Core Services (as per project instructions)
        services.AddSingleton<DatabaseService>();
        services.AddSingleton<IAIModelRepository, AIModelRepository>();
        services.AddSingleton<IConversationRepository, ConversationRepository>();
        services.AddSingleton<IMessageRepository, MessageRepository>();
        services.AddSingleton<IApiKeyStorageProvider, ApiKeyStorageProvider>();
        services.AddSingleton<IApiKeyManager, ApiKeyManager>();
        services.AddSingleton<IAIProviderFactory, AIProviderFactory>();
        services.AddSingleton<IAIModelDiscoveryService, AIModelDiscoveryService>();
        services.AddSingleton<IAIModelManager, AIModelManager>();
        services.AddSingleton<IChatService, ChatService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<ConversationService>();
        services.AddSingleton<DatabaseInitializer>();
        services.AddSingleton<IStartupInitializer>(sp => sp.GetRequiredService<DatabaseInitializer>());
        services.AddMemoryCache();

        // Transients - AI Service Implementations and ViewModels
        services.AddTransient<GroqAIService>();
        services.AddTransient<OpenRouterAIService>();
        services.AddTransient<DummyAIService>();
        services.AddTransient<IAIProviderService, DummyAIService>();
    }

    private static void RegisterViewModels(IServiceCollection services)
    {
        // Transients - ViewModels (as per project instructions)
        services.AddTransient<MainPageViewModel>();
        services.AddTransient<ChatViewModel>();
        services.AddTransient<AIModelsViewModel>();
        services.AddTransient<ConversationsSidebarViewModel>();
    }

    private static void RegisterPages(IServiceCollection services)
    {
        // Transients - Pages (as per project instructions)
        services.AddTransient<MainPage>();
        services.AddTransient<ChatPage>();
        services.AddTransient<AIModelsPage>();
    }
}
