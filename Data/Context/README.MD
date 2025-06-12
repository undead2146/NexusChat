# Data/Context Module Documentation

This module provides database context services and initialization for the NexusChat application, managing SQLite database connections, schema migrations, and search operations.

---

# [Component Name: DatabaseService.cs]

## 1. Identity & Purpose

* **Component Name:** `DatabaseService.cs`
* **Type:** `Service Class`
* **Module:** `Data.Context`
* **Primary Goal:** `Manages SQLite database connections, schema initialization, and provides thread-safe database access for the entire application.`

## 2. Core Functionality / Key Contents

* **(For Files/Classes):**
  * `Initialize`: `Initializes SQLite database connection and creates/migrates tables with thread-safety using SemaphoreSlim.`
  * `CreateTablesIfNeededAsync`: `Creates or updates database schema for all model types (Message, User, Conversation, AIModel) with automatic migration support.`
  * `GetAsyncConnection/GetSyncConnection`: `Provides access to SQLite connections with lazy initialization and connection pooling.`
  * `ClearAllData`: `Development utility to completely reset database data while preserving schema.`
  * `TableExistsAsync`: `Schema introspection utility to check table existence for conditional operations.`

## 3. Logic & Data Flow Highlights

* **Key Operations/Workflow:** `Database Initialization: 1. Thread-safe initialization check. 2. Create SQLiteAsyncConnection with proper flags. 3. Schema creation/migration for all model tables. 4. Mark as initialized.`
* **Data Input:** `Model type definitions via reflection, Database file path from FileSystem.AppDataDirectory, Schema migration requirements from PropertyInfo analysis.`
* **Data Output/Storage:** `SQLite database file (nexus_chat.db3), Table creation/alteration SQL commands, Connection instances for repositories.`
* **State Management (if applicable):** `Thread-safe initialization state via _isInitialized bool and SemaphoreSlim locking mechanism.`

## 4. Design & Architectural Notes

* **Design Patterns:** `Singleton Pattern: Single database instance across application., Factory Pattern: Connection creation on demand., Template Method: Generic table creation/migration logic.`
* **Key Dependencies (Injected/Used):**
  * Internal: `Core.Models (Message, User, Conversation, AIModel)`
  * External: `SQLite-net-pcl, Microsoft.Maui.Storage (FileSystem)`
* **Architectural Role:** `Database Access Layer foundation - provides infrastructure for all repository implementations and data persistence operations.`

## 5. Interactions & Relationships

* **Consumed By:** `All Repository implementations (UserRepository, ConversationRepository, MessageRepository, AIModelRepository), DatabaseSearchService, DatabaseInitializer`
* **Interacts With:** `SQLite database engine, FileSystem API for database path resolution, Model classes for schema reflection`
* **Events Published/Subscribed (if any):** `None - operates as synchronous service dependency.`

## 6. Configuration & Environment (If Applicable)

* **Environment Variables:** `None directly used.`
* **Settings/Constants:** `DatabaseFilename = "nexus_chat.db3": Database file name constant, SQLiteOpenFlags: ReadWrite | Create | SharedCache for optimal performance.`

## 7. Key Considerations / Improvements

* **Critical Logic Points:** `Thread-safe initialization prevents race conditions during app startup. Schema migration logic handles adding new columns without data loss. Connection management ensures proper resource disposal.`
* **Security Notes (if applicable):** `Database stored in secure app data directory with OS-level access control.`
* **Potential Improvements/TODOs:** `Implement connection pooling for better performance under load. Add database encryption for sensitive data. Consider implementing database backup/restore functionality.`

---

# [Component Name: DatabaseSearchService.cs]

## 1. Identity & Purpose

* **Component Name:** `DatabaseSearchService.cs`
* **Type:** `Service Class`
* **Module:** `Data.Context`
* **Primary Goal:** `Provides unified search functionality across all database tables with pagination support and result formatting for development/debugging tools.`

## 2. Core Functionality / Key Contents

* **(For Files/Classes):**
  * `SearchTableAsync`: `Main entry point that routes search requests to appropriate table-specific search methods based on table name.`
  * `SearchUserTableAsync`: `Searches User table using repository pattern with username/display name filtering.`
  * `SearchConversationTableAsync`: `Searches Conversation table with title and metadata filtering capabilities.`
  * `SearchMessageTableAsync`: `Searches Message table with content truncation for performance and readability in search results.`
  * `SearchModelTableAsync`: `Searches AIModel table for provider/model name matching.`

## 3. Logic & Data Flow Highlights

* **Key Operations/Workflow:** `Search Flow: 1. Route by table name. 2. Delegate to repository search method. 3. Convert results to dictionary format. 4. Apply pagination limits. 5. Return formatted results.`
* **Data Input:** `Search text (user input), Table name selection, Page size limits, CancellationToken for operation control.`
* **Data Output/Storage:** `List of Dictionary<string, object> for flexible result display, Truncated content for large text fields, Paginated results for performance.`
* **State Management (if applicable):** `Stateless service - no internal state maintained between search operations.`

## 4. Design & Architectural Notes

* **Design Patterns:** `Strategy Pattern: Different search strategies per table type., Repository Pattern: Delegates actual search to repository layer., Converter Pattern: Uses DataObjectConverter for consistent result formatting.`
* **Key Dependencies (Injected/Used):**
  * Internal: `Repository interfaces (IUserRepository, IConversationRepository, etc.), DataObjectConverter helper, Core.Models`
  * External: `System.Threading for cancellation support`
* **Architectural Role:** `Search abstraction layer - bridges UI search requirements with repository data access patterns.`

## 5. Interactions & Relationships

* **Consumed By:** `Development tools ViewModels (DatabaseToolsViewModel), Admin/debugging interfaces`
* **Interacts With:** `All repository implementations for data access, DataObjectConverter for result formatting, Model classes for type-safe operations`
* **Events Published/Subscribed (if any):** `None - operates as request/response service.`

## 6. Configuration & Environment (If Applicable)

* **Environment Variables:** `None used.`
* **Settings/Constants:** `Content truncation limits (200 chars for Content, 50 chars for RawResponse), Case-insensitive search matching.`

## 7. Key Considerations / Improvements

* **Critical Logic Points:** `Cancellation token support prevents UI blocking during long searches. Content truncation balances performance with useful search result display. Repository delegation maintains proper separation of concerns.`
* **Security Notes (if applicable):** `Search operates through repository layer which should handle access control and data filtering.`
* **Potential Improvements/TODOs:** `Implement full-text search indexing for better performance. Add search result highlighting. Consider search result caching for repeated queries.`

---

# [Component Name: DatabaseInitializer.cs]

## 1. Identity & Purpose

* **Component Name:** `DatabaseInitializer.cs`
* **Type:** `Service Class`
* **Module:** `Data.Context`
* **Primary Goal:** `Handles application startup database initialization and seeds essential default data to ensure the application has required baseline data.`

## 2. Core Functionality / Key Contents

* **(For Files/Classes):**
  * `InitializeAsync`: `Main startup method that coordinates database structure initialization and data seeding operations.`
  * `SeedInitialDataAsync`: `Creates default user and essential data if database is empty, ensuring application baseline functionality.`
  * `IStartupInitializer implementation`: `Integrates with application startup sequence through well-defined initialization interface.`

## 3. Logic & Data Flow Highlights

* **Key Operations/Workflow:** `Startup Initialization: 1. Initialize database structure via DatabaseService. 2. Check if seeding is needed (empty User table). 3. Create default user if required. 4. Complete initialization successfully.`
* **Data Input:** `DatabaseService instance for database operations, Application startup context.`
* **Data Output/Storage:** `Default User entity in database if none exists, Initialization completion status for startup sequence.`
* **State Management (if applicable):** `Stateless service - operates only during application startup phase.`

## 4. Design & Architectural Notes

* **Design Patterns:** `Initializer Pattern: Implements IStartupInitializer for consistent startup behavior., Dependency Injection: Receives DatabaseService for database operations., Seeding Pattern: Checks and creates baseline data as needed.`
* **Key Dependencies (Injected/Used):**
  * Internal: `DatabaseService for database operations, Core.Models.User for default user creation, Services.Interfaces.IStartupInitializer interface`
  * External: `System.Threading.Tasks for async operations`
* **Architectural Role:** `Application bootstrap component - ensures database readiness and baseline data availability for application functionality.`

## 5. Interactions & Relationships

* **Consumed By:** `Application startup sequence (MauiProgram.cs or App.xaml.cs), Dependency injection container during app initialization`
* **Interacts With:** `DatabaseService for database operations, User model for default entity creation`
* **Events Published/Subscribed (if any):** `None - operates as one-time startup initialization.`

## 6. Configuration & Environment (If Applicable)

* **Environment Variables:** `None used.`
* **Settings/Constants:** `Default user configuration (username: "default", display name: "Default User"), UTC timestamp for creation tracking.`

## 7. Key Considerations / Improvements

* **Critical Logic Points:** `Idempotent operation - safe to run multiple times without side effects. Error handling ensures startup failures are properly reported. Seeding logic checks prevent duplicate default data creation.`
* **Security Notes (if applicable):** `Default user creation provides baseline functionality but should be secured/replaced in production scenarios.`
* **Potential Improvements/TODOs:** `Make default user configuration externally configurable. Add more comprehensive initial data seeding (AI models, sample conversations). Implement database version tracking for future migration needs.`

---

## Module Overview

### Purpose

The Data/Context module serves as the foundational database layer for NexusChat, providing:
* Thread-safe database connection management
* Automatic schema creation and migration
* Unified search capabilities across all data tables
* Application startup database initialization

### Key Design Principles

1. **Thread Safety**: All database operations are designed to be thread-safe
2. **Schema Evolution**: Automatic migration support for adding new model properties
3. **Separation of Concerns**: Clear separation between connection management, search, and initialization
4. **Repository Integration**: Designed to work seamlessly with the repository pattern

### Dependencies

- **SQLite-net-pcl**: Core database engine
* **Repository Pattern**: All search operations delegate to repositories
* **Model Classes**: Schema reflection and data structure definition
* **MAUI FileSystem**: Database file location management

### Usage Patterns

```csharp
// Database service registration (in MauiProgram.cs)
services.AddSingleton<DatabaseService>();
services.AddSingleton<DatabaseSearchService>();
services.AddTransient<IStartupInitializer, DatabaseInitializer>();

// Typical usage in repositories
var connection = await _databaseService.GetConnectionAsync();
var users = await connection.Table<User>().ToListAsync();

// Search usage in ViewModels
var results = await _searchService.SearchTableAsync("User", "john", 50, cancellationToken);
```
