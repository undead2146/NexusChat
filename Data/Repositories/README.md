# Data/Repositories Directory

## 1. Identity & Purpose
*   **Component Name:** `Data/Repositories/`
*   **Type:** `Directory`
*   **Module:** `Data.Repositories`
*   **Primary Goal:** `Implements the Repository pattern for data access, providing async CRUD operations and domain-specific queries for all entities while abstracting SQLite database operations behind clean interfaces.`

## 2. Core Functionality / Key Contents
*   **(For Directories):**
    *   `BaseRepository.cs`: `Abstract base class implementing common CRUD operations with standardized error handling and cancellation support.`
    *   `UserRepository.cs`: `User-specific repository with authentication, username validation, and credential management functionality.`
    *   `ConversationRepository.cs`: `Conversation management with message relationships, user filtering, and search capabilities.`
    *   `MessageRepository.cs`: `Message CRUD operations with conversation-based queries and pagination support.`
    *   `AIModelRepository.cs`: `AI model repository with provider filtering, selection management, and usage tracking.`

## 3. Logic & Data Flow Highlights
*   **Key Operations/Workflow:** `Repository Pattern Flow: 1. Service/ViewModel calls repository method. 2. Repository uses BaseRepository for common operations. 3. SQLite database operations executed with error handling. 4. Results returned with proper cancellation support.`
*   **Data Input:** `Service layer method calls with entity objects and query parameters, CancellationTokens for operation cancellation, DatabaseService providing SQLite connections.`
*   **Data Output/Storage:** `SQLite database persistence via SQLite-net-pcl, Typed entity collections and individual objects, Boolean operation results for success/failure, Exception propagation for critical errors.`
*   **State Management (if applicable):** `No internal state - repositories are stateless services that delegate to DatabaseService for connection management.`

## 4. Design & Architectural Notes
*   **Design Patterns:** `Repository Pattern: Encapsulates data access logic and provides clean abstraction., Template Method: BaseRepository defines common operation structure., Async/Await: All operations support cancellation and async execution., Dependency Injection: Repositories injected as services.`
*   **Key Dependencies (Injected/Used):**
    *   Internal: `DatabaseService (connection management), Entity models (User, Conversation, Message, AIModel), Repository interfaces (IUserRepository, etc.)`
    *   External: `SQLite-net-pcl (ORM operations), SQLiteNetExtensions (relationships), System.Threading (CancellationToken), System.Diagnostics (logging)`
*   **Architectural Role:** `Data Access Layer providing clean separation between business logic and database operations.`

## 5. Interactions & Relationships
*   **Consumed By:** `Service implementations (ChatService, UserService, etc.), ViewModels for direct data access, Database seeding and migration operations, Unit tests and integration tests.`
*   **Interacts With:** `DatabaseService for connection management, SQLite database engine for persistence, Entity models for data mapping, Exception handling system for error management.`
*   **Events Published/Subscribed (if any):** `No events published - repositories are passive data access components that respond to method calls.`

## 6. Configuration & Environment (If Applicable)
*   **Environment Variables:** `No direct environment variable usage - configuration handled by DatabaseService dependency.`
*   **Settings/Constants:** `Default limits in search operations (limit = 50), Table name derivation from entity type names, Connection timeout and retry logic in BaseRepository.`

## 7. Key Considerations / Improvements
*   **Critical Logic Points:** `Standardized error handling in BaseRepository.ExecuteDbOperationAsync prevents silent failures, CancellationToken support throughout for responsive UI operations, Foreign key relationship management in ConversationRepository.`
*   **Security Notes (if applicable):** `Password verification handled securely in UserRepository using BCrypt, No direct SQL injection risk due to parameterized queries, API key references stored as variable names not actual keys.`
*   **Potential Improvements/TODOs:** `Implement soft delete functionality across all repositories for better data management, Add audit trail support for tracking entity changes over time, Consider implementing Unit of Work pattern for transaction management.`

---

# BaseRepository.cs

## 1. Identity & Purpose
*   **Component Name:** `BaseRepository.cs`
*   **Type:** `Abstract Class`
*   **Module:** `Data.Repositories`
*   **Primary Goal:** `Provides abstract base implementation of the Repository pattern with standardized CRUD operations, error handling, and cancellation support for all entity repositories.`

## 2. Core Functionality / Key Contents
*   **(For Files/Classes):**
    *   `ExecuteDbOperationAsync<TResult>`: `Wrapper method providing standardized error handling, cancellation support, and logging for all database operations.`
    *   `GetByIdAsync/GetAllAsync`: `Standard entity retrieval operations with cancellation token support and error handling.`
    *   `AddAsync/UpdateAsync/DeleteAsync`: `CRUD operations supporting both entity objects and ID-based deletion with transaction safety.`
    *   `FindAsync/FirstOrDefaultAsync`: `Query operations using LINQ expressions for flexible entity filtering.`
    *   `SearchAsync`: `Abstract method requiring implementation by derived classes for entity-specific search logic.`

## 3. Logic & Data Flow Highlights
*   **Key Operations/Workflow:** `Database Operation Flow: 1. Method called with parameters and optional CancellationToken. 2. ExecuteDbOperationAsync handles connection, error handling, and cancellation. 3. SQLite operation executed. 4. Results returned or exceptions propagated appropriately.`
*   **Data Input:** `Generic entity objects of type T, LINQ expression predicates for filtering, CancellationTokens for operation cancellation, Entity IDs for direct access.`
*   **Data Output/Storage:** `SQLite database persistence through DatabaseService, Generic entity collections and objects, Boolean success indicators for operations, Standardized exception handling and logging.`
*   **State Management (if applicable):** `Stateless design - no internal state maintained, relies on DatabaseService for connection management.`

## 4. Design & Architectural Notes
*   **Design Patterns:** `Template Method: Defines common operation structure for all repositories., Generic Programming: Type-safe operations for any entity type T., Error Handling Strategy: Centralized exception handling with operation-specific logging.`
*   **Key Dependencies (Injected/Used):**
    *   Internal: `DatabaseService for SQLite connection management, Generic entity constraints (class, new())`
    *   External: `SQLite-net-pcl for ORM operations, System.Threading for CancellationToken support, System.Diagnostics for Debug.WriteLine logging`
*   **Architectural Role:** `Foundation class providing consistent data access patterns across all entity repositories.`

## 5. Interactions & Relationships
*   **Consumed By:** `All concrete repository implementations (UserRepository, ConversationRepository, MessageRepository, AIModelRepository).`
*   **Interacts With:** `DatabaseService for connection management, SQLite database for persistence operations, Entity models through generic type constraints.`
*   **Events Published/Subscribed (if any):** `No events - provides synchronous operation results and exception propagation.`

## 6. Configuration & Environment (If Applicable)
*   **Environment Variables:** `None - configuration delegated to DatabaseService dependency.`
*   **Settings/Constants:** `Table name derived from entity type name, Default return values for failed operations (empty lists, null objects, false booleans).`

## 7. Key Considerations / Improvements
*   **Critical Logic Points:** `ExecuteDbOperationAsync centralizes all error handling ensuring consistent behavior across repositories, CancellationToken support throughout prevents UI blocking on long operations.`
*   **Security Notes (if applicable):** `Uses parameterized queries preventing SQL injection, No sensitive data stored in base class - handled by specific repositories.`
*   **Potential Improvements/TODOs:** `Add logging interface dependency for better log management beyond Debug.WriteLine, Implement retry logic for transient database errors, Consider adding batch operation support for bulk inserts/updates.`

---

# UserRepository.cs

## 1. Identity & Purpose
*   **Component Name:** `UserRepository.cs`
*   **Type:** `Repository Class`
*   **Module:** `Data.Repositories`
*   **Primary Goal:** `Manages user data persistence with authentication support, providing secure credential validation, username uniqueness checking, and user profile management.`

## 2. Core Functionality / Key Contents
*   **(For Files/Classes):**
    *   `GetByUsernameAsync`: `Retrieves user by unique username with case-sensitive matching and cancellation support.`
    *   `ValidateCredentialsAsync`: `Securely validates username/password combinations using BCrypt hash verification.`
    *   `UsernameExistsAsync`: `Checks username availability for registration validation without exposing user data.`
    *   `GetByDisplayNameAsync`: `Alternative user lookup by display name for UI features and user discovery.`
    *   `SearchAsync`: `Implements case-insensitive search across username and display name fields with result limiting.`

## 3. Logic & Data Flow Highlights
*   **Key Operations/Workflow:** `Authentication Flow: 1. GetByUsernameAsync retrieves user record. 2. BCrypt.Verify validates password against stored hash. 3. Boolean result returned for authentication decision. User Search: 1. Convert search text to lowercase. 2. Query both Username and DisplayName fields. 3. Return limited result set.`
*   **Data Input:** `Username/password combinations for authentication, Search text for user discovery, User entities for CRUD operations, CancellationTokens for operation control.`
*   **Data Output/Storage:** `User table in SQLite database, Boolean authentication results, User entity collections for search results, Exception propagation for critical errors.`
*   **State Management (if applicable):** `Stateless repository - no user session or authentication state maintained internally.`

## 4. Design & Architectural Notes
*   **Design Patterns:** `Repository Pattern: Clean data access abstraction for user operations., Authentication Strategy: Secure credential validation using industry-standard BCrypt hashing.`
*   **Key Dependencies (Injected/Used):**
    *   Internal: `BaseRepository<User> for common CRUD operations, User entity model, DatabaseService through base class`
    *   External: `BCrypt.Net for password hashing and verification, SQLite-net-pcl for database operations, System.Diagnostics for error logging`
*   **Architectural Role:** `User data access layer supporting authentication, user management, and profile operations.`

## 5. Interactions & Relationships
*   **Consumed By:** `Authentication services and middleware, User management ViewModels, Registration and login workflows, User profile management features.`
*   **Interacts With:** `User entity model for data mapping, BCrypt library for password security, DatabaseService for connection management, Logging system for error tracking.`
*   **Events Published/Subscribed (if any):** `No events published - authentication results returned directly to calling services.`

## 6. Configuration & Environment (If Applicable)
*   **Environment Variables:** `None - authentication configuration handled by calling services.`
*   **Settings/Constants:** `Search result limit (default 50 users), Case-insensitive search behavior, Username uniqueness constraint enforcement.`

## 7. Key Considerations / Improvements
*   **Critical Logic Points:** `BCrypt password verification ensures secure authentication without exposing plain text passwords, Username uniqueness checking prevents duplicate registrations, Case-insensitive search improves user discovery.`
*   **Security Notes (if applicable):** `Passwords never stored in plain text - BCrypt hashing used throughout, Username validation prevents enumeration attacks, No sensitive data exposed in search results.`
*   **Potential Improvements/TODOs:** `Add password complexity validation at repository level, Implement account lockout tracking for failed authentication attempts, Add audit logging for authentication events and user changes.`

---

# ConversationRepository.cs

## 1. Identity & Purpose
*   **Component Name:** `ConversationRepository.cs`
*   **Type:** `Repository Class`
*   **Module:** `Data.Repositories`
*   **Primary Goal:** `Manages conversation data with message relationships, providing user-specific filtering, search capabilities, and automatic schema migration for model tracking fields.`

## 2. Core Functionality / Key Contents
*   **(For Files/Classes):**
    *   `GetByUserIdAsync`: `Retrieves all conversations for a specific user with optional pagination and sorting by update time.`
    *   `AddMessageAsync`: `Adds messages to conversations while updating conversation metadata and timestamps automatically.`
    *   `GetMessagesAsync/GetMessagesReverseAsync`: `Retrieves conversation messages with flexible ordering and pagination support.`
    *   `SearchAsync`: `Searches conversations by title and summary content with relevance-based ordering.`
    *   `EnsureSchemaUpdatedAsync`: `Automatic database schema migration adding ModelName and ProviderName columns for AI tracking.`

## 3. Logic & Data Flow Highlights
*   **Key Operations/Workflow:** `Message Addition Flow: 1. Validate message and get conversation. 2. Update conversation timestamp and title if needed. 3. Save message via MessageRepository. 4. Update conversation metadata. Conversation Retrieval: 1. Query by user ID with filtering. 2. Apply sorting and pagination. 3. Return conversation collections.`
*   **Data Input:** `User IDs for filtering conversations, Message entities for addition to conversations, Search terms for content discovery, Pagination parameters (limit, offset), CancellationTokens for operation control.`
*   **Data Output/Storage:** `Conversations table in SQLite database, Message entities through MessageRepository integration, Conversation metadata updates, Search result collections with relevance ordering.`
*   **State Management (if applicable):** `Maintains database connection state through DatabaseService, automatic schema versioning state for migrations.`

## 4. Design & Architectural Notes
*   **Design Patterns:** `Repository Pattern: Encapsulates conversation data access with business logic., Composition: Integrates MessageRepository for related message operations., Schema Migration: Automatic database evolution for new features.`
*   **Key Dependencies (Injected/Used):**
    *   Internal: `BaseRepository<Conversation>, IMessageRepository for message operations, DatabaseService for connections, Conversation and Message entities`
    *   External: `SQLite-net-pcl for ORM operations, SQLiteNetExtensions for relationships, System.Diagnostics for migration logging`
*   **Architectural Role:** `Primary data access layer for conversation management with integrated message handling and user filtering.`

## 5. Interactions & Relationships
*   **Consumed By:** `ChatService for conversation management, ChatViewModel for UI data binding, Conversation management features, Search and filtering operations.`
*   **Interacts With:** `MessageRepository for message operations, User entities through foreign key relationships, DatabaseService for connection management, Schema migration system for database evolution.`
*   **Events Published/Subscribed (if any):** `No events published - operates as passive data access service responding to method calls.`

## 6. Configuration & Environment (If Applicable)
*   **Environment Variables:** `None - configuration handled through DatabaseService and dependency injection.`
*   **Settings/Constants:** `Default pagination limits (50 conversations, 100 messages), Conversation title auto-generation (50 character limit), Schema migration column names (ModelName, ProviderName).`

## 7. Key Considerations / Improvements
*   **Critical Logic Points:** `Automatic schema migration ensures backward compatibility when adding AI tracking features, Message addition updates conversation metadata maintaining data consistency, Foreign key relationship management between users, conversations, and messages.`
*   **Security Notes (if applicable):** `User ID filtering ensures conversation isolation between users, No direct SQL execution - uses parameterized queries through ORM, Message content handled securely without validation at repository level.`
*   **Potential Improvements/TODOs:** `Implement soft delete for conversations and messages, Add conversation archiving and backup functionality, Optimize message loading with lazy loading patterns for large conversations.`

---

# MessageRepository.cs

## 1. Identity & Purpose
*   **Component Name:** `MessageRepository.cs`
*   **Type:** `Repository Class`
*   **Module:** `Data.Repositories`
*   **Primary Goal:** `Handles message CRUD operations with conversation-based filtering, pagination support, and bulk operations for chat message management and conversation cleanup.`

## 2. Core Functionality / Key Contents
*   **(For Files/Classes):**
    *   `GetMessagesByConversationIdAsync`: `Retrieves all messages for a specific conversation ordered chronologically with cancellation support.`
    *   `GetMessagesByConversationIdBeforeTimestampAsync`: `Implements pagination by retrieving messages before a specific timestamp with result limiting.`
    *   `DeleteAllMessagesForConversationAsync`: `Bulk deletion of all messages in a conversation for cleanup operations.`
    *   `GetMessageCountForConversationAsync`: `Efficient message counting for pagination and UI display without loading full message content.`
    *   `SearchAsync`: `Content-based message search across all conversations with relevance ordering by timestamp.`

## 3. Logic & Data Flow Highlights
*   **Key Operations/Workflow:** `Message Retrieval: 1. Filter by conversation ID. 2. Apply timestamp or pagination constraints. 3. Order chronologically. 4. Return message collections. Message Management: 1. Add/update individual messages. 2. Track conversation relationships. 3. Handle bulk operations efficiently.`
*   **Data Input:** `Conversation IDs for message filtering, Message entities for CRUD operations, Timestamp boundaries for pagination, Search text for content discovery, CancellationTokens for operation control.`
*   **Data Output/Storage:** `Messages table in SQLite database, Message entity collections with conversation relationships, Message count integers for pagination, Boolean operation results for success tracking.`
*   **State Management (if applicable):** `Stateless repository design - no message caching or conversation state maintained internally.`

## 4. Design & Architectural Notes
*   **Design Patterns:** `Repository Pattern: Clean abstraction for message data access operations., Pagination Strategy: Timestamp-based pagination for efficient large conversation handling., Bulk Operations: Optimized deletion for conversation cleanup.`
*   **Key Dependencies (Injected/Used):**
    *   Internal: `BaseRepository<Message> for common operations, Message entity model, DatabaseService through base class`
    *   External: `SQLite-net-pcl for database operations, System.Threading for cancellation support, System.Diagnostics for error logging`
*   **Architectural Role:** `Message data access layer supporting chat functionality with efficient conversation-based operations.`

## 5. Interactions & Relationships
*   **Consumed By:** `ConversationRepository for integrated message operations, ChatService for message management, ChatViewModel for UI data binding, Message search and filtering features.`
*   **Interacts With:** `Message entity model for data mapping, Conversation entities through foreign key relationships, DatabaseService for connection management, Error logging system for operation tracking.`
*   **Events Published/Subscribed (if any):** `No events published - provides direct operation results to calling services and repositories.`

## 6. Configuration & Environment (If Applicable)
*   **Environment Variables:** `None - configuration managed through DatabaseService dependency injection.`
*   **Settings/Constants:** `Default pagination limits (20 messages per page), Search result limits (50 messages), Chronological ordering for message display.`

## 7. Key Considerations / Improvements
*   **Critical Logic Points:** `Timestamp-based pagination enables efficient loading of large conversations without memory issues, Bulk deletion operations handle conversation cleanup without individual message iteration, Foreign key constraints ensure message-conversation data integrity.`
*   **Security Notes (if applicable):** `Message content stored as-is without encryption - security handled at application level, Conversation ID filtering prevents cross-conversation message access, No user authentication at repository level - handled by calling services.`
*   **Potential Improvements/TODOs:** `Implement message soft delete for recovery capabilities, Add message threading support for reply chains, Optimize search with full-text search indexes for large message volumes.`

---

# AIModelRepository.cs

## 1. Identity & Purpose
*   **Component Name:** `AIModelRepository.cs`
*   **Type:** `Repository Class`
*   **Module:** `Data.Repositories`
*   **Primary Goal:** `Manages AI model data with provider organization, selection tracking, usage analytics, and configuration management for multi-provider AI model discovery and management.`

## 2. Core Functionality / Key Contents
*   **(For Files/Classes):**
    *   `SetCurrentModelAsync`: `Manages model selection state ensuring only one model is selected at a time with usage tracking.`
    *   `GetByProviderAsync`: `Retrieves models filtered by provider with default model prioritization for provider-specific model lists.`
    *   `GetActiveModelsAsync`: `Returns available models with API key configuration sorted by usage and preference for UI display.`
    *   `RecordUsageAsync/SetFavoriteStatusAsync`: `Tracks model usage statistics and user preferences for analytics and recommendation systems.`
    *   `SearchAsync`: `Implements comprehensive search across model name, provider, description, and display name with relevance ranking.`

## 3. Logic & Data Flow Highlights
*   **Key Operations/Workflow:** `Model Selection: 1. Clear existing selection flags. 2. Set new model as selected. 3. Update usage statistics and timestamp. Model Discovery: 1. Filter by availability and API key configuration. 2. Sort by selection, favorites, usage. 3. Return prioritized model list.`
*   **Data Input:** `Provider names for filtering models, Model selection preferences and favorites, Usage tracking for analytics, Search terms for model discovery, API key variable references for configuration.`
*   **Data Output/Storage:** `AIModels table in SQLite database, Model selection state and preferences, Usage analytics (count, timestamps), Search results with relevance ranking, Provider-specific model groupings.`
*   **State Management (if applicable):** `Maintains model selection state ensuring single selection constraint, tracks usage analytics over time for recommendation features.`

## 4. Design & Architectural Notes
*   **Design Patterns:** `Repository Pattern: Encapsulates AI model data access with business logic., State Management: Ensures consistent model selection across application, Analytics Tracking: Records usage patterns for intelligent model recommendations.`
*   **Key Dependencies (Injected/Used):**
    *   Internal: `BaseRepository<AIModel> for CRUD operations, AIModel entity with status and capability flags, DatabaseService for connection management`
    *   External: `SQLite-net-pcl for database operations, System.Threading for async operations, System.Diagnostics for operation logging`
*   **Architectural Role:** `Central data access layer for AI model management supporting provider integration and user preference tracking.`

## 5. Interactions & Relationships
*   **Consumed By:** `AIModelManager for model discovery and configuration, AIProviderFactory for provider-specific model access, Model selection UI components, Usage analytics and recommendation systems.`
*   **Interacts With:** `AIModel entities with capability and status tracking, Provider configuration systems, API key management for model availability, Usage tracking and analytics systems.`
*   **Events Published/Subscribed (if any):** `No events published - model state changes handled through direct repository method calls and service layer coordination.`

## 6. Configuration & Environment (If Applicable)
*   **Environment Variables:** `API key variable references stored in model configuration (OPENROUTER_API_KEY, GROQ_API_KEY, etc.) for runtime availability checking.`
*   **Settings/Constants:** `Model selection constraint (single selection), Default sorting priority (selection > favorites > usage > name), Search result limits and relevance ranking factors.`

## 7. Key Considerations / Improvements
*   **Critical Logic Points:** `Model selection state management prevents inconsistent UI state with atomic selection updates, Usage tracking provides data for intelligent model recommendations, Provider-based filtering supports multi-provider AI integration architecture.`
*   **Security Notes (if applicable):** `API key variables stored as references not actual keys, Model availability checking doesn't expose sensitive configuration, Usage analytics collected without exposing user conversation content.`
*   **Potential Improvements/TODOs:** `Implement model capability-based filtering for task-specific recommendations, Add model performance tracking (response time, success rates), Consider model versioning support for provider model updates.`
