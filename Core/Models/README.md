# Core/Models Directory

## 1. Identity & Purpose
*   **Component Name:** `Core/Models/`
*   **Type:** `Directory`
*   **Module:** `Core.Models`
*   **Primary Goal:** `Contains all domain models and data entities for the NexusChat application, defining the core data structures for users, conversations, messages, AI models, and supporting types.`

## 2. Core Functionality / Key Contents
*   **(For Directories):**
    *   `AIModel.cs`: `Primary entity representing AI models with capabilities, status tracking, and provider information.`
    *   `User.cs`: `User entity with authentication, preferences, and profile management functionality.`
    *   `Conversation.cs`: `Chat conversation entity with metadata and message relationships.`
    *   `Message.cs`: `Individual message entity supporting both user and AI messages with rich metadata.`
    *   `ModelStatus.cs`: `Enumeration defining AI model availability states for status tracking.`
    *   `AIProvider.cs`: `Value object representing AI service providers and their configuration.`
    *   `ProviderConfiguration.cs`: `Configuration entity for AI provider settings and API endpoints.`
    *   `ScrollTargetInfo.cs`: `UI helper model for managing chat scroll behavior and animations.`

## 3. Logic & Data Flow Highlights
*   **Key Operations/Workflow:** `Model Creation & Persistence: 1. Models define SQLite table structures. 2. Entities support CRUD operations via repositories. 3. Navigation properties enable relationship queries. 4. Validation methods ensure data integrity.`
*   **Data Input:** `Repository operations (Create/Read/Update/Delete), Constructor parameters for initialization, Property setters for state changes, Factory methods for test data generation.`
*   **Data Output/Storage:** `SQLite database persistence via SQLite-net-pcl, Repository pattern return values, Property getters for data access, Navigation properties for related entities.`
*   **State Management (if applicable):** `SQLite attributes define persistence behavior, Navigation properties managed by SQLiteNetExtensions, Status enums provide type-safe state representation.`

## 4. Design & Architectural Notes
*   **Design Patterns:** `Entity Model: Represents domain objects with data and behavior., Repository Pattern: Models work with repository interfaces for data access., Factory Pattern: Models provide factory methods for test data creation., Value Object: Status enums and configuration objects.`
*   **Key Dependencies (Injected/Used):**
    *   Internal: `NexusChat.Helpers (TokenHelper for token estimation)`
    *   External: `SQLite-net-pcl (ORM attributes and relationships), SQLiteNetExtensions (Navigation properties), BCrypt.Net-Next (Password hashing), System.ComponentModel.DataAnnotations (Validation)`
*   **Architectural Role:** `Domain layer entities defining the core business objects and data structures for the application.`

## 5. Interactions & Relationships
*   **Consumed By:** `Repository implementations (AIModelRepository, UserRepository, etc.), ViewModels for data binding and business logic, Services for domain operations and validation, Database seeding and migration operations.`
*   **Interacts With:** `SQLite database engine for persistence, BCrypt library for password security, Validation frameworks for data integrity, Helper utilities for calculations (tokens, etc.).`
*   **Events Published/Subscribed (if any):** `Models themselves don't publish events, but changes are typically observed through repository operations and ViewModel property change notifications.`

## 6. Configuration & Environment (If Applicable)
*   **Environment Variables:** `Models reference API key environment variables (e.g., OPENROUTER_API_KEY, GROQ_API_KEY) through ApiKeyVariable property.`
*   **Settings/Constants:** `Default values defined in model properties (DefaultTemperature = 0.7f, PreferredTheme = "System"), Table names specified via SQLite Table attributes, MaxLength constraints for string properties.`

## 7. Key Considerations / Improvements
*   **Critical Logic Points:** `User password hashing using BCrypt for security, Foreign key relationships ensuring data integrity, Status enum mapping for persistent storage, Clone methods for creating model copies safely.`
*   **Security Notes (if applicable):** `Passwords stored as BCrypt hashes never plain text, API key variables referenced by name not stored directly, Email validation prevents malformed addresses, Username validation enforces character restrictions.`
*   **Potential Improvements/TODOs:** `Add soft delete functionality to models for better data management, Implement audit trails for tracking model changes over time, Consider adding model versioning for future schema migrations.`

---

# Model Entity Details

## AIModel.cs
**Purpose:** Represents AI models with comprehensive metadata including capabilities, status, usage statistics, and provider information.

**Key Properties:**
- `ModelName`: Provider-specific model identifier
- `Status`: Current availability state using ModelStatus enum
- `SupportsStreaming/Vision/CodeCompletion`: Capability flags
- `IsSelected/IsFavorite/IsDefault`: User preference flags
- `UsageCount/LastUsed`: Usage tracking for analytics

## User.cs
**Purpose:** User entity with authentication, preferences, and profile management.

**Key Features:**
- BCrypt password hashing for security
- Email validation and username constraints
- Theme preferences and AI model selection
- Comprehensive validation with detailed error messages
- Test user factory methods for development

## Conversation.cs
**Purpose:** Chat conversation container with metadata and message relationships.

**Key Properties:**
- Foreign key relationship to User
- One-to-many relationship with Messages
- Title, category, and summary for organization
- Archive and favorite flags for user management
- Model/provider tracking for conversation context

## Message.cs
**Purpose:** Individual chat messages supporting rich metadata and different message types.

**Key Features:**
- Bidirectional conversation relationship
- Support for user and AI message types
- Token counting and usage tracking
- Error handling and status tracking
- Model/provider attribution for AI responses

## ModelStatus.cs
**Purpose:** Type-safe enumeration for AI model availability states.

**Values:**
- `Available`: Ready to use
- `NoApiKey`: Requires API key configuration
- `Unavailable`: Not accessible
- `Error`: System error occurred

## Supporting Models
- **AIProvider**: Value object for provider information
- **ProviderConfiguration**: Persistent provider settings
- **ScrollTargetInfo**: UI helper for chat scroll management
