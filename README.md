# NexusChat - Multi-Model AI Chat for .NET MAUI

[![Platform](https://img.shields.io/badge/Platform-.NET%20MAUI-blueviolet)]()
[![License](https://img.shields.io/badge/License-MIT-blue.svg)]()

NexusChat is a cross-platform mobile application built with .NET MAUI that provides a user-friendly interface to interact with various AI language models. The app allows users to have conversations with AI, save chat history, customize settings, select different AI models, and manage multiple conversation threads.

## 🚀 Features

- **Multi-Model AI Integration** - Support for various AI models (OpenAI, Anthropic, etc.)
- **User Authentication** - Secure login system with user profiles
- **Conversation Management** - Create, save, and categorize different chat threads
- **Chat History** - Local database storage for all your past AI conversations
- **Customizable Settings** - Personalize your experience with themes and preferences
- **API Key Management** - Securely add and manage your own API keys
- **Offline Support** - Basic functionality when disconnected from the internet

## 🛠️ Technology Stack

- **.NET MAUI 9.0** - Cross-platform UI framework
- **MVVM Architecture** - Using CommunityToolkit.Mvvm 8.2.1
- **SQLite** - Local database for conversation storage
- **RestSharp** - For API communication
- **Newtonsoft.Json** - JSON processing
- **AI Providers** - Modular support for multiple AI backends

## 📱 Supported Platforms

- Android
- iOS
- Windows
- MacOS (partially supported)

## 🏗️ Project Structure

The project follows MVVM architecture with a clear separation of concerns:

```
NexusChat/
├── Core/
│   ├── Models/ - Domain model classes (User, Conversation, Message, AIModel)
│   └── ViewModels/ - MVVM ViewModels with business logic
│       └── DevTools/ - Specialized ViewModels for developer tools
├── Data/
│   ├── Context/ - Database context and initialization
│   └── Repositories/ - Data access repositories with CRUD operations
├── Helpers/ - Utility classes and extension methods
├── Resources/
│   ├── AppIcon/ - Application icon assets
│   ├── Fonts/ - Application fonts including FontAwesome
│   ├── Images/ - Image assets used in the UI
│   ├── Raw/ - Raw resource files
│   ├── Splash/ - Splash screen assets
│   └── Styles/ - XAML styles and themes
│       ├── DarkTheme.xaml - Dark mode resources
│       ├── LightTheme.xaml - Light mode resources
│       └── MessageBubbleStyles.xaml - Specialized styles for chat bubbles
├── Services/
│   ├── AIProviders/ - AI service implementations for different providers
│   └── Interfaces/ - Service interfaces for dependency injection
└── Views/
    ├── Controls/ - Reusable UI components
    ├── Converters/ - Value converters for XAML bindings
    └── Pages/ - Application pages/screens
        └── DevTools/ - Developer tools and debugging pages
```

## 📋 Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/vs/) with .NET MAUI workload or [Visual Studio Code](https://code.visualstudio.com/)
- API keys for the AI models you want to use

## 🚀 Getting Started

1. Clone this repository
   ```
   git clone https://github.com/undead2146/NexusChat.git
   ```

2. Open the solution file in Visual Studio or VS Code
   ```
   cd NexusChat
   start NexusChat.sln
   ```

3. Restore NuGet packages
   ```
   dotnet restore
   ```

4. Build the application
   ```
   dotnet build
   ```

5. Run on your preferred platform
   ```
   dotnet run -f net9.0-android
   dotnet run -f net9.0-ios
   dotnet run -f net9.0-windows
   ```

6. Add your AI provider API keys in the Settings section

## 💻 Core Implementation Details

### Database Schema

The app uses SQLite with the following key tables:
- **Users** - Authentication and preferences
- **Conversations** - Chat thread management
- **Messages** - Individual chat messages
- **AIModels** - Available AI models
- **APIKeys** - Securely stored API credentials

### Key Components

1. **User Authentication**
   - Local password-based authentication
   - Profile customization options
   - Secure credential storage with SHA-256 hashing

2. **Chat Interface**
   - Real-time AI interactions with typing indicators
   - Message bubble UI with sender distinction
   - Timestamp and message status display
   - Light and dark theme support

3. **Model Selection**
   - Switch between different AI models
   - Configure model parameters
   - Compare model capabilities

4. **Developer Tools**
   - Database viewer for inspecting SQLite data
   - Model testing environment
   - Theme and UI component showcase

## 🧪 Testing Strategy

- Unit tests for services and repositories
- UI tests for critical workflows
- Cross-platform compatibility testing
- Performance testing for database operations

## 📧 Contact

Project Link: [https://github.com/undead2146/NexusChat.git](https://github.com/undead2146/NexusChat.git)

