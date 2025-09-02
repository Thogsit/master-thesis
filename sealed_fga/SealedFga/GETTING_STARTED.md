# SealedFGA Getting Started Guide

SealedFGA is a .NET framework that provides **compile-time OpenFGA integration** for C# ASP.NET backends using Entity Framework Core. The library's primary goal is to make OpenFGA authorization as **secure and easy as possible** by providing guarantees **as early as possible in the development cycle** - ideally at compile time rather than runtime.

## Table of Contents

1. [Framework Overview & Philosophy](#framework-overview--philosophy)
2. [Prerequisites & Installation](#prerequisites--installation)
3. [Complete Setup Guide](#complete-setup-guide)
4. [Quick Start Tutorial](#quick-start-tutorial)
5. [Core Concepts](#core-concepts)
6. [API Reference](#api-reference)
7. [Authorization Patterns](#authorization-patterns)
8. [Static Analysis](#static-analysis)
9. [Migration from Vanilla OpenFGA](#migration-from-vanilla-openfga)
10. [Performance & Production](#performance--production)
11. [Best Practices & Troubleshooting](#best-practices--troubleshooting)

## Framework Overview & Philosophy

### Security Through Type Safety

SealedFGA follows a **security-first design philosophy** that prioritizes catching authorization bugs as early as possible:

**Strong Typing Over Stringly-Typed APIs:**

- Uses generated strongly-typed entity IDs (e.g., `SecretEntityId`) instead of raw `Guid` or `string` values
- Generated relation classes (e.g., `SecretEntityIdAttributes.can_edit`) prevent typos and mismatched entity-relation combinations
- Interface-driven design using `ISealedFgaType<TId>` to clearly mark which entities require authorization

**Early Error Detection:**

- **Compile-time model validation**: Invalid `model.fga` files cause build errors
- **Static analysis foundation**: Built-in Roslyn analyzer infrastructure for detecting authorization gaps
- **Type system constraints**: Generic constraints prevent incompatible entity/relation usage at compile time

**Developer Experience Focus:**

- **Automatic code generation**: Developers define authorization model once in `model.fga`, everything else is generated
- **Seamless integration**: Works with existing ASP.NET Core and Entity Framework patterns
- **IntelliSense support**: Generated types provide full IDE support for discoverability

### Multi-Layer Authorization Strategy

SealedFGA provides multiple layers of security:

**Layer 1: Type System Guarantees (Compile-time)**

- Strongly-typed entity IDs prevent ID confusion between different entity types
- Generic constraints ensure relations can only be used with compatible entities
- Generated code eliminates possibility of typos in relation names

**Layer 2: Attribute-Based Authorization (Runtime)**

- `[FgaAuthorize]` and `[FgaAuthorizeList]` attributes on controller parameters
- Custom model binders automatically perform OpenFGA checks and entity retrieval
- Transparent to developer - authorization happens automatically

**Layer 3: Manual Authorization Guards (Compile-time Verifiable)**

- `SealedFgaGuard.RequireCheck()` methods for explicit authorization declarations
- Compile to NOPs but provide static analysis targets
- Additive system - multiple checks accumulate authorization state

**Layer 4: Static Analysis Verification**

- Roslyn analyzer that verifies authorization coverage across all code paths
- Detects missing authorization before database access
- Validates that required relations are established before entity property access
- Built on Microsoft's Roslyn Data Flow Analysis framework

## Prerequisites & Installation

### System Requirements

- **.NET 8.0+ SDK**
- **C# Language Preview Version** (required for latest language features)
- **Entity Framework Core** (for database integration)
- **ASP.NET Core** (for web API integration)
- **OpenFGA Server** (running locally or remotely)

### Installation

> ⚠️ **Current Installation Method (Evaluation Phase)**
>
> SealedFGA is currently only available as a ProjectReference during the evaluation phase. This is a temporary limitation.

```xml
<ProjectReference Include="../path/to/SealedFga/SealedFga.csproj" />
```

> 🔮 **Future Installation (Post-Evaluation)**
>
> SealedFGA will be available as a NuGet package:
> ```bash
> dotnet add package SealedFga
> ```

### Dependencies

SealedFGA automatically includes the following dependencies:

- **OpenFGA .NET SDK** - For OpenFGA server communication
- **TickerQ** - Background job processing for performance optimization
- **Microsoft.CodeAnalysis** - Static analysis engine
- **EntityFramework.Core** - Database integration

## Complete Setup Guide

### Program.cs Configuration

Your `Program.cs` requires several specific configurations for SealedFGA to work properly:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenFga.Sdk.Client;
using SealedFga.Fga;
using SealedFga.ModelBinder;
using TickerQ.Dashboard.DependencyInjection;
using TickerQ.DependencyInjection;
using TickerQ.EntityFrameworkCore.DependencyInjection;

public static class Program {
    public static void Main(string[] args) {
        // REQUIRED: Initialize SealedFGA before anything else
        SealedFgaInit.Initialize();

        var builder = WebApplication.CreateBuilder(args);

        // 1. Configure Entity Framework with SealedFGA interceptor
        builder.Services.AddDbContext<YourDbContext>((sp, options) => {
            options.UseYourDatabase("ConnectionString");
            // REQUIRED: Add SealedFGA save changes interceptor
            options.AddInterceptors(
                sp.GetRequiredService<SealedFgaSaveChangesInterceptor>()
            );
        });

        // 2. Configure ASP.NET Core with SealedFGA model binders
        builder.Services.AddControllers(options => {
            // REQUIRED: Insert SealedFGA model binder provider
            options.ModelBinderProviders.Insert(0,
                new SealedFgaModelBinderProvider<YourDbContext>());
        });

        // 3. Configure OpenFGA client
        builder.Services.AddSingleton<OpenFgaClient>(_ => {
            var fgaClient = new OpenFgaClient(new ClientConfiguration {
                ApiUrl = "http://localhost:8080", // Your OpenFGA server
            });

            // Get store ID (first store in this example)
            var storeId = fgaClient.ListStores(null).Result.Stores[0].Id;
            fgaClient.Dispose();

            // Recreate client with store ID
            fgaClient = new OpenFgaClient(new ClientConfiguration {
                ApiUrl = "http://localhost:8080",
                StoreId = storeId,
            });

            // Get authorization model ID (latest model)
            var authModelId = fgaClient.ReadAuthorizationModels().Result
                .AuthorizationModels[0].Id;
            fgaClient.Dispose();

            // Final client configuration
            return new OpenFgaClient(new ClientConfiguration {
                ApiUrl = "http://localhost:8080",
                StoreId = storeId,
                AuthorizationModelId = authModelId,
            });
        });

        // 4. Register SealedFGA services
        builder.Services.AddScoped<SealedFgaService>();
        builder.Services.AddScoped<SealedFgaSaveChangesInterceptor>();

        // 5. Configure TickerQ for background processing
        builder.Services.AddTickerQ(opt => {
            opt.AddOperationalStore<YourDbContext>(efOpt => {
                efOpt.UseModelCustomizerForMigrations();
            });
            opt.AddDashboard("/tickerq"); // Optional: TickerQ dashboard
        });

        var app = builder.Build();

        // 6. Configure middleware
        app.UseRouting();
        app.MapControllers();
        app.UseTickerQ(); // REQUIRED: Enable TickerQ processing

        // 7. Initialize database
        using (var scope = app.Services.CreateScope()) {
            var context = scope.ServiceProvider.GetRequiredService<YourDbContext>();
            context.Database.EnsureCreated();
        }

        app.Run();
    }
}
```

## Quick Start Tutorial

Let's create a simple document management system with SealedFGA.

### Step 1: Create the Authorization Model

Create a `model.fga` file in your project root:

```fga
model
  schema 1.1

type user

type organization
  relations
    define Member: [user]

type document
  relations
    define Owner: [organization]
    define can_view: [user] or Member from Owner
    define can_edit: [user] or Member from Owner
```

SealedFGA expects a certain naming style for relations to distinguish between specific permissions and more general relations. In the above example, the `Member` and `Owner` relations represent roles/groups and as such should be written in UpperCamelCase. The `can_view` and `can_edit` relations though represent specific permissions and as such should use snake_case.
SealedFGA's source generator identifies this and creates 2 different classes, the `DocumentIdAttributes` that contains `DocumentIdAttributes.can_view` as well as `DocumentIdAttributes.can_edit`, and the `DocumentIdGroups` that contains `DocumentIdGroups.Owner`. This is handy to easily distinguish the different type of relations we are working with in the source code.

### Step 2: Define Entity Classes

Create your entity classes implementing `ISealedFgaType<TId>`:

```csharp

// Strongly-typed IDs

[SealedFgaTypeId("document", SealedFgaTypeIdType.Guid)]
public partial class DocumentEntityId;

[SealedFgaTypeId("organization", SealedFgaTypeIdType.Guid)]
public partial class OrganizationEntityId;

[SealedFgaTypeId("user", SealedFgaTypeIdType.Guid)]
public partial class UserEntityId;

// Entity classes

public class DocumentEntity : ISealedFgaType<DocumentEntityId>
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DocumentEntityId Id { get; set; } = null!;
    public required string Title { get; set; }
    public required string Content { get; set; }

    // IMPORTANT: Use [SealedFgaRelation] for automatic relation updates to the OpenFGA server
    [SealedFgaRelation(nameof(DocumentEntityIdGroups.Owner))]
    public OrganizationEntityId OwnerOrganizationId { get; set; } = null!;
}

public class OrganizationEntity : ISealedFgaType<OrganizationEntityId>
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public OrganizationEntityId Id { get; set; } = null!;
    public required string Name { get; set; }
}

public class UserEntity : ISealedFgaType<UserEntityId>
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public UserEntityId Id { get; set; } = null!;
    public required string Name { get; set; }
    public required string Email { get; set; }
}
```

> **Note**: The strongly-typed ID classes (`DocumentEntityId`, `OrganizationEntityId`, etc.) have to be declared manually as seen in the example. They are automatically populated with utility functionality by SealedFGA's source generator based on your `model.fga` file. They also need to be declared so that the source generator creates the strongly typed relation classes that will be named `<StronglyTypedIdClassName>Attributes` and `<StronglyTypedIdClassName>Groups`.

### Step 3: Configure DbContext

```csharp
public class MyDbContext : DbContext
{
    public MyDbContext(DbContextOptions<MyDbContext> options) : base(options) { }

    // Only DbSets with ISealedFgaType<TId> entities are considered for authorization
    public DbSet<DocumentEntity> Documents { get; set; }
    public DbSet<OrganizationEntity> Organizations { get; set; }
    public DbSet<UserEntity> Users { get; set; }
}
```

### Step 4: Create Controllers with Attribute-Based Authorization

```csharp
[ApiController]
[Route("documents")]
public class DocumentController(MyDbContext context) : ControllerBase
{
    // Automatic authorization + entity retrieval
    [HttpGet("{documentId}")]
    public IActionResult GetDocument(
        [FromRoute] DocumentEntityId documentId,
        [FgaAuthorize(
            Relation = nameof(DocumentEntityIdAttributes.can_view),
            ParameterName = nameof(documentId)
        )] DocumentEntity document
    ) => Ok(document); // document is already authorized and loaded from the database!

    // List all documents the current user can view
    [HttpGet]
    public IActionResult GetAllDocuments(
        [FgaAuthorizeList(Relation = nameof(DocumentEntityIdAttributes.can_view))]
        List<DocumentEntity> documents
    ) => Ok(documents); // Only authorized documents are returned!

    // Update with automatic authorization
    [HttpPut("{documentId}")]
    public async Task<IActionResult> UpdateDocument(
        [FromRoute] DocumentEntityId documentId,
        [FgaAuthorize(
            Relation = nameof(DocumentEntityIdAttributes.can_edit),
            ParameterName = nameof(documentId)
        )] DocumentEntity document,
        [FromBody] UpdateDocumentRequest request
    ) {
        document.Title = request.Title;
        document.Content = request.Content;
        document.OwnerOrganizationId = request.Owner;
        await context.SaveChangesAsync(); // Owner relation updates to the OpenFGA service are handled automatically by SealedFGA!

        return Ok(document);
    }
}
```

### Step 5: Run and Test

1. **Build the project** - SealedFGA generates strongly-typed classes during compilation
2. **Start your application** - The setup from Program.cs handles all SealedFGA initialization
3. **Test authorization** - Try accessing documents with different user contexts

The attribute-based approach automatically handles:

- Authorization checks against OpenFGA
- Entity loading from database
- HTTP 404 responses for non-existent entities
- HTTP 401 responses for unauthorized access
- Background processing of relation updates

## Core Concepts

### Strongly-Typed Entity IDs

SealedFGA generates strongly-typed ID wrapper classes for each entity type in your `model.fga`:

```csharp
// Generated automatically from model.fga
public sealed class DocumentEntityId : ISealedFgaTypeId<DocumentEntityId>
{
    private readonly Guid value;

    public DocumentEntityId(Guid value) => this.value = value;

    // Conversion methods, equality, etc. are generated automatically
    public static implicit operator DocumentEntityId(Guid value) => new(value);
    public static implicit operator Guid(DocumentEntityId id) => id.value;

    // OpenFGA integration methods
    public string AsOpenFgaIdTupleString() => $"document:{value}";

    // And more, e.g. JSON and EF Core serialization etc.
    ...
}
```

**Benefits:**

- **Type Safety**: Can't accidentally use `OrganizationEntityId` where `DocumentEntityId` is expected
- **IntelliSense**: Full IDE support with autocomplete
- **Refactoring Safety**: Renaming OpenFGA types/relations only needs to be done in a single place, removed/renamed permissions lead to compile-time errors, ...

### Generated Relation Classes

For each entity, SealedFGA generates relation classes for use in authorization:

```csharp
// Generated automatically
public static class DocumentEntityIdAttributes // snake_case relations, especially permissions
{
    public static readonly SealedFgaRelation<DocumentEntityId> can_view = new("can_view");
    public static readonly SealedFgaRelation<DocumentEntityId> can_edit = new("can_edit");
}

public static class DocumentEntityIdGroups // uppercase relations/group memberships
{
    public static readonly SealedFgaRelation<DocumentEntityId> Owner = new("Owner");
}
```

**Usage:**

```csharp
// Type-safe relation usage
await sealedFgaService.CheckAsync(
    userId,
    DocumentEntityIdAttributes.can_view,  // Definitely exists in the model.fga file!
    documentId
);
```

### Automatic Relation Management

The `[SealedFgaRelation]` attribute automatically manages OpenFGA relations when entities are saved:

```csharp
public class DocumentEntity : ISealedFgaType<DocumentEntityId>
{
    // When this property changes, SealedFGA automatically:
    // 1. Deletes old relation: organization:old_id#member -> document:doc_id#owner
    // 2. Creates new relation: organization:new_id#member -> document:doc_id#owner
    [SealedFgaRelation(nameof(DocumentEntityIdGroups.owner))]
    public OrganizationEntityId OwnerOrganizationId { get; set; } = null!;
}
```

This happens automatically during `context.SaveChangesAsync()` (or the `SaveChanges()` pendant) via the SealedFGA interceptor.

### Background Processing with TickerQ

All OpenFGA operations are queued in the background for non-blocking updates and automatic retrial:

```csharp
// This queues the operation instead of blocking the HTTP request
await sealedFgaService.QueueWrite(userId, DocumentEntityIdAttributes.can_edit, documentId);

// TickerQ processes the queue with automatic retries:
// - 1 minute after first failure
// - 10 minutes after second failure
// - 1 hour after third failure
```

## API Reference

### SealedFgaService

The `SealedFgaService` is the main service for interacting with OpenFGA using strongly-typed APIs.

#### Authorization Checking Methods

```csharp
// Check if user has relation to object (returns bool)
Task<bool> CheckAsync<TUserId, TObjId>(
    TUserId user,
    ISealedFgaRelation<TObjId> relation,
    TObjId objectId,
    CancellationToken cancellationToken = default
);

// Check authorization and throw UnauthorizedAccessException if denied
Task EnsureCheckAsync<TUserId, TObjId>(
    TUserId user,
    ISealedFgaRelation<TObjId> relation,
    TObjId objectId,
    CancellationToken cancellationToken = default
);

// Batch check multiple permissions (better performance than individual checks)
Task<Dictionary<(TUserId User, ISealedFgaRelation<TObjId> Relation, TObjId Object), bool>>
BatchCheckAsync<TUserId, TObjId>(
    IEnumerable<(TUserId User, ISealedFgaRelation<TObjId> Relation, TObjId Object)> checks,
    CancellationToken cancellationToken = default
);
```

**Example Usage:**

```csharp
// Simple check
var canEdit = await sealedFgaService.CheckAsync(
    currentUserId,
    DocumentEntityIdAttributes.can_edit,
    documentId
);

// Ensure check (throws on failure)
await sealedFgaService.EnsureCheckAsync(
    currentUserId,
    DocumentEntityIdAttributes.can_view,
    documentId
);

// Batch check multiple documents
var checks = documentIds.Select(id => (
    User: currentUserId,
    Relation: DocumentEntityIdAttributes.can_view,
    Object: id
));
var results = await sealedFgaService.BatchCheckAsync(checks);
```

> **Note:** The `BatchCheckAsync` method is not covered by the static data-flow analysis!

#### Relation Management Methods

```csharp
// Queue a write operation (background processing)
Task QueueWrite<TUserId, TObjId>(
    TUserId user,
    ISealedFgaRelation<TObjId> relation,
    TObjId objectId
);

// Queue a delete operation
Task QueueDelete<TUserId, TObjId>(
    TUserId user,
    ISealedFgaRelation<TObjId> relation,
    TObjId objectId
);

// Queue multiple writes
Task QueueWrites<TUserId, TObjId>(
    IEnumerable<(TUserId User, ISealedFgaRelation<TObjId> Relation, TObjId Object)> operations
);

// Queue multiple deletes
Task QueueDeletes<TUserId, TObjId>(
    IEnumerable<(TUserId User, ISealedFgaRelation<TObjId> Relation, TObjId Object)> operations
);
```

**Example Usage:**

```csharp
// Grant edit permission to user
await sealedFgaService.QueueWrite(
    userId,
    DocumentEntityIdAttributes.can_edit,
    documentId
);

// Revoke view permission
await sealedFgaService.QueueDelete(
    userId,
    DocumentEntityIdAttributes.can_view,
    documentId
);

// Batch operations for better performance
var writeOps = userIds.Select(uid => (
    User: uid,
    Relation: DocumentEntityIdAttributes.can_view,
    Object: documentId
));
await sealedFgaService.QueueWrites(writeOps);
```

> **Note:** The relation write/delete methods should mostly be avoided and only used where necessary. The `OpenFgaRelationAttribute` should cover most cases automatically and should be used instead.

#### Query Methods

```csharp
// List all objects a user has specific relation to
Task<IEnumerable<TObjId>> ListObjectsAsync<TUserId, TObjId>(
    TUserId user,
    ISealedFgaRelation<TObjId> relation,
    CancellationToken cancellationToken = default
);

// Modify all relations containing an old ID to use new ID
Task ModifyIdAsync<TId>(
    TId oldId,
    TId newId,
    CancellationToken cancellationToken = default
) where TId : ISealedFgaTypeId<TId>;
```

**Example Usage:**

```csharp
// Get all documents user can view
var viewableDocumentIds = await sealedFgaService.ListObjectsAsync(
    currentUserId,
    DocumentEntityIdAttributes.can_view
);

// Update all relations when changing entity ID
await sealedFgaService.ModifyIdAsync(oldDocumentId, newDocumentId);
```

#### Safe Operation Methods

```csharp
// Safely write tuples (checks existence first to avoid conflicts)
Task SafeWriteTupleAsync(List<TupleKey> tuples, CancellationToken ct = default);

// Safely delete tuples (checks existence first)
Task SafeDeleteTupleAsync(List<TupleKey> tuples, CancellationToken ct = default);

// Combined safe write and delete operation
Task SafeWriteAndDeleteTuplesAsync(
    List<TupleKey> writeTuples,
    List<TupleKey> deleteTuples,
    CancellationToken ct = default
);
```

### Authorization Attributes

#### FgaAuthorizeAttribute

Automatically authorizes and loads a single entity in controller actions:

```csharp
[AttributeUsage(AttributeTargets.Parameter)]
public class FgaAuthorizeAttribute : ModelBinderAttribute
{
    public required string Relation { get; set; }      // Relation to check
    public required string ParameterName { get; set; } // Parameter containing entity ID
}
```

**Usage:**

```csharp
[HttpGet("{documentId}")]
public IActionResult GetDocument(
    [FromRoute] DocumentEntityId documentId,           // ID parameter
    [FgaAuthorize(
        Relation = nameof(DocumentEntityIdAttributes.can_view),
        ParameterName = nameof(documentId)             // References ID parameter
    )] DocumentEntity document                          // Auto-authorized and database loaded entity
) => Ok(document);
```

**Behavior:**

- Performs OpenFGA authorization check
- Loads entity from database if authorized
- Returns HTTP 404 if entity doesn't exist
- Returns HTTP 401 if user lacks permission
- Injects authorized entity into action parameter

#### FgaAuthorizeListAttribute

Automatically filters and loads multiple entities based on user permissions:

```csharp
[AttributeUsage(AttributeTargets.Parameter)]
public class FgaAuthorizeListAttribute : ModelBinderAttribute
{
    public required string Relation { get; set; }      // Relation to check
}
```

**Usage:**

```csharp
[HttpGet]
public IActionResult GetAllDocuments(
    [FgaAuthorizeList(Relation = nameof(DocumentEntityIdAttributes.can_view))]
    List<DocumentEntity> documents                      // Only authorized entities
) => Ok(documents);
```

**Behavior:**

- Uses OpenFGA ListObjects to get authorized entity IDs
- Loads only authorized entities from database
- Returns empty list if user has no permissions
- Efficient: Only queries database for authorized entities

#### SealedFgaRelationAttribute

Marks entity properties for automatic OpenFGA relation management:

```csharp
[AttributeUsage(AttributeTargets.Property)]
public class SealedFgaRelationAttribute : Attribute
{
    public string Relation { get; }                    // OpenFGA relation name
    public SealedFgaRelationTargetType TargetType { get; } // Object or User relation
}
```

**Usage:**

```csharp
public class DocumentEntity : ISealedFgaType<DocumentEntityId>
{
    // When OwnerOrganizationId changes, automatically updates OpenFGA relation:
    // organization:new_id#member -> document:this_id#owner
    [SealedFgaRelation(nameof(DocumentEntityIdGroups.owner))]
    public OrganizationEntityId OwnerOrganizationId { get; set; } = null!;

    // Can also explicitly specify direction of the relation (is this entity the object or user of the relation)
    [SealedFgaRelation("assigned_user", SealedFgaRelationTargetType.User)]
    public UserEntityId? AssignedUserId { get; set; }
}
```

**Behavior:**

- Automatically queues OpenFGA updates during `SaveChangesAsync()`
- Deletes old relations and creates new ones when property changes
- Works with nullable properties (handles null values correctly)
- Background processing via TickerQ for performance

#### ImplementedByAttribute

Enables static analysis to follow dependency injection correctly:

```csharp
[AttributeUsage(AttributeTargets.Interface)]
public class ImplementedByAttribute : Attribute
{
    public Type ImplementationType { get; }
}
```

**Usage:**

```csharp
[ImplementedBy(typeof(DocumentService))]
public interface IDocumentService
{
    Task<DocumentEntity> GetDocumentAsync(DocumentEntityId id);
}

public class DocumentService : IDocumentService
{
    public async Task<DocumentEntity> GetDocumentAsync(DocumentEntityId id) // "id" parameter inherits checked permissions from the calling method
    {
        SealedFgaGuard.RequireCheck(id, DocumentEntityIdAttributes.can_view); // Takes calling method's permission checks into account
        return await dbContext.Documents.FindAsync(id);
    }
}
```

**Why This Matters:**

- Static analysis follows interface method calls into concrete implementations
- Detects missing authorization checks in service layers
- Provides interprocedural analysis across dependency injection boundaries
- ⚠️ Without this attribute, static analysis stops at interface boundaries!

### SealedFgaGuard

Provides methods for explicit authorization declarations that are analyzed by the static analyzer:

```csharp
public static class SealedFgaGuard
{
    // Declare required permissions for an entity
    public static void RequireCheck<TId, TRel>(
        ISealedFgaType<TId> entity,
        params TRel[] relations
    );

    // Declare required permissions for an entity ID
    public static void RequireCheck<TId, TRel>(
        TId entityId,
        params TRel[] relations
    );
}
```

**Usage:**

```csharp
public async Task<string> GetDocumentContent(DocumentEntityId documentId)
{
    // Declare to static analyzer: this method requires these permissions
    SealedFgaGuard.RequireCheck(
        documentId,
        DocumentEntityIdAttributes.can_view,
        DocumentEntityIdAttributes.can_read_content
    );

    // Static analyzer verifies these permissions are checked before database access
    var document = await dbContext.Documents.FindAsync(documentId);
    return document.Content;
}
```

**Important Notes:**

- **Used by static analyzer** to track required permissions through code paths
- **Developer responsibility** to place `RequireCheck` calls in correct locations
- **Additive system** - multiple calls accumulate permission requirements
- ⚠️ **These methods compile to NOPs** - they don't perform any runtime authorization checks

## Authorization Patterns

SealedFGA supports multiple authorization patterns depending on your needs:

### 1. Attribute-Based Authorization (Recommended)

**Best for:** Standard CRUD operations, simple authorization requirements

```csharp
[HttpGet("{id}")]
public IActionResult GetDocument(
    [FromRoute] DocumentEntityId id,
    [FgaAuthorize(
        Relation = nameof(DocumentEntityIdAttributes.can_view),
        ParameterName = nameof(id)
    )] DocumentEntity document
) => Ok(document);
```

**Pros:**

- Completely automatic authorization and entity loading
- Handles HTTP status codes (404/401) automatically
- Reduced boilerplate code in controller actions
- Works with static analyzer automatically

**Cons:**

- Less flexible for complex authorization logic
- Cannot (currently) combine multiple permission checks easily

### 2. Manual SealedFgaService Calls

**Best for:** Complex authorization logic, multiple permission checks, service layers

```csharp
[HttpPost("{documentId}/share")]
public async Task<IActionResult> ShareDocument(
    [FromRoute] DocumentEntityId documentId,
    [FromBody] ShareDocumentRequest request
)
{
    // Manual authorization check
    await sealedFgaService.EnsureCheckAsync(
        GetCurrentUserId(),
        DocumentEntityIdAttributes.can_edit,
        documentId
    );

    // Additional business logic authorization
    var document = await dbContext.Documents.FindAsync(documentId);
    if (document.IsLocked) {
        await sealedFgaService.EnsureCheckAsync(
            GetCurrentUserId(),
            DocumentEntityIdAttributes.can_override_lock,
            documentId
        );
    }

    // Grant permissions to new users
    var shareOps = request.UserIds.Select(userId => (
        User: userId,
        Relation: DocumentEntityIdAttributes.can_view,
        Object: documentId
    ));
    await sealedFgaService.QueueWrites(shareOps);

    return Ok();
}
```

**Pros:**

- Full flexibility for complex authorization scenarios
- Can combine multiple permission types
- Works well in service layers
- `CheckAsync` and `EnsureCheckAsync` are correctly identified and followed by the static analyzer

**Cons:**

- More boilerplate code
- Manual error handling required
- Must manually load entities from database

### 3. Hybrid Approach

Combine multiple patterns; use attribute-based authorization where possible and fall back to manual checks where required.

```csharp
[HttpPut("{documentId}")]
public async Task<IActionResult> UpdateDocument(
    [FromRoute] DocumentEntityId documentId,
    // Attribute-based for primary authorization
    [FgaAuthorize(
        Relation = nameof(DocumentEntityIdAttributes.can_edit),
        ParameterName = nameof(documentId)
    )] DocumentEntity document,
    [FromBody] UpdateDocumentRequest request
)
{
    // Manual check for special operations
    if (request.ChangeOwner && request.NewOwnerId != document.OwnerOrganizationId) {
        await sealedFgaService.EnsureCheckAsync(
            GetCurrentUserId(),
            DocumentEntityIdAttributes.can_transfer_ownership,
            documentId
        );
    }

    // Update document
    document.Title = request.Title;
    document.Content = request.Content;

    if (request.ChangeOwner) {
        document.OwnerOrganizationId = request.NewOwnerId;
    }

    await dbContext.SaveChangesAsync();

    return Ok(document);
}

// Service layer with guard statements for static analysis
private async Task<bool> ValidateDocumentContent(DocumentEntityId documentId, string content)
{
    SealedFgaGuard.RequireCheck(
        documentId,
        DocumentEntityIdAttributes.can_edit
    );

    // Business logic that static analyzer can verify is properly authorized
    var document = await dbContext.Documents.FindAsync(documentId);
    return content.Length <= document.MaxContentLength;
}
```

## Static Analysis

SealedFGA includes a sophisticated static analysis engine that verifies authorization coverage across your codebase.

### Current Capabilities

#### Permission Tracking

- **FgaAuthorize Attributes**: Automatically registers permissions from `[FgaAuthorize]` and `[FgaAuthorizeList]` attributes
- **Service Method Calls**: Tracks permissions from `CheckAsync` and `EnsureCheckAsync` methods
- **Guard Statements**: Analyzes `SealedFgaGuard.RequireCheck` declarations

#### Interprocedural Analysis

- **Cross-Method Analysis**: Follows permissions through method calls up to depth 4
- **Permission Inheritance**: Tracks authorization state across method boundaries
- **Return Value Analysis**: Understands when methods return authorized entities

#### PointsTo Analysis

- **Variable Copying**: When `var copy = authorizedEntity`, `copy` inherits all permissions
- **Assignment Tracking**: Follows entity references through variable assignments
- **Parameter Passing**: Tracks permissions through method parameter passing

#### Dependency Injection Support

- **Interface Resolution**: Uses `[ImplementedBy]` attribute to follow interface calls
- **Service Layer Analysis**: Can analyze authorization in injected service classes
- **Cross-Boundary Tracking**: Maintains permission state across DI boundaries

### Current Limitations

#### Raw OpenFGA SDK Detection

The analyzer **cannot detect** vanilla OpenFGA .NET SDK calls:

```csharp
// NOT DETECTED by static analysis
var allowed = await openFgaClient.Check(new ClientCheckRequest {
    User = "user:alice",
    Relation = "can_view",
    Object = "document:123"
});

// DETECTED by static analysis
var allowed = await sealedFgaService.CheckAsync(
    userId,
    DocumentEntityIdAttributes.can_view,
    documentId
);
```

#### BatchCheckAsync Not Supported

Currently, `BatchCheckAsync` calls are not tracked by the static analyzer:

```csharp
// NOT DETECTED by static analysis
var results = await sealedFgaService.BatchCheckAsync(checks);

// DETECTED by static analysis (but sequential and slow)
foreach (var check in checks) {
    await sealedFgaService.CheckAsync(check.User, check.Relation, check.Object);
}
```

#### Developer Responsibility for Guard Placement

The analyzer can only verify that `RequireCheck` declarations match actual permission checks. It's the developer's responsibility to write the right checks:

```csharp
public async Task<DocumentEntity> EditDocument(DocumentEntity document, string content)
{
    // INCORRECT: RequireCheck for can_view although we perform an edit operation
    SealedFgaGuard.RequireCheck(document, DocumentEntityIdAttributes.can_view); // Should be can_edit
    document.Content = content;
    await dbContext.SaveChangesAsync();
}
```

### How Static Analysis Works

#### 1. Permission Registration

The analyzer tracks permissions as they are established:

```csharp
[HttpGet("{documentId}")]
public IActionResult GetDocument(
    [FromRoute] DocumentEntityId documentId,
    [FgaAuthorize(
        Relation = nameof(DocumentEntityIdAttributes.can_view),
        ParameterName = nameof(documentId)
    )] DocumentEntity document  // ← Registers: documentId + document have "can_view"
) => Ok(document);
```

#### 2. Permission Inheritance via PointsTo

When variables are copied, permissions are inherited:

```csharp
public IActionResult ProcessDocument(
    [FgaAuthorize(...)] DocumentEntity authorizedDoc  // Has "can_view" permission
)
{
    var docCopy = authorizedDoc;        // ← docCopy inherits "can_view"
    var docReference = docCopy;         // ← docReference inherits "can_view"

    return ProcessDocumentHelper(docReference);  // ← Permission flows through call
}

private IActionResult ProcessDocumentHelper(DocumentEntity doc)
{
    // Static analyzer knows 'doc' has "can_view" permission from call site
    SealedFgaGuard.RequireCheck(doc, DocumentEntityIdAttributes.can_view); // ✅ Satisfied

    return Ok(doc.Content);  // ✅ Database access is properly authorized
}
```

#### 3. Interprocedural Analysis

The analyzer follows permissions across method calls:

```csharp
[HttpPost("{documentId}/process")]
public async Task<IActionResult> ProcessDocument(
    [FromRoute] DocumentEntityId documentId,
    [FgaAuthorize(...)] DocumentEntity document
)
{
    // Call service method - permissions flow through
    var result = await documentService.ProcessDocumentAsync(document);
    return Ok(result);
}

// In DocumentService class:
public async Task<ProcessResult> ProcessDocumentAsync(DocumentEntity document)
{
    // Analyzer knows 'document' has permissions from controller call site
    SealedFgaGuard.RequireCheck(document, DocumentEntityIdAttributes.can_edit);

    // This database access is verified as properly authorized
    document.LastProcessed = DateTime.UtcNow;
    await dbContext.SaveChangesAsync();

    return new ProcessResult { Success = true };
}
```

#### 4. Dependency Injection Resolution

With `[ImplementedBy]` attribute, analyzer follows interface calls:

```csharp
[ImplementedBy(typeof(DocumentService))]
public interface IDocumentService
{
    Task ValidateDocumentAsync(DocumentEntity document);
}

public class DocumentController(IDocumentService documentService) : ControllerBase
{
    [HttpPost("{documentId}/validate")]
    public async Task<IActionResult> ValidateDocument(
        [FgaAuthorize(...)] DocumentEntity document  // Has permissions
    )
    {
        // Analyzer follows this call into DocumentService.ValidateDocumentAsync
        await documentService.ValidateDocumentAsync(document);
        return Ok();
    }
}

public class DocumentService : IDocumentService
{
    public async Task ValidateDocumentAsync(DocumentEntity document)
    {
        // Analyzer knows 'document' parameter has permissions from controller
        SealedFgaGuard.RequireCheck(document, DocumentEntityIdAttributes.can_view);

        // Database access is verified as authorized
        var validationRules = await dbContext.ValidationRules
            .Where(r => r.DocumentType == document.Type)
            .ToListAsync();

        // Complex validation logic...
    }
}
```

> **Note:** The interfaces are currently only resolved to their real implementing class when injected via *primary constructors* as seen in the example above!

### Best Practices for Static Analysis

#### Use SealedFGA APIs Consistently

```csharp
// GOOD: Static analyzer can track this
await sealedFgaService.CheckAsync(userId, relation, objectId);

// BAD: Static analyzer cannot track this
await openFgaClient.Check(new ClientCheckRequest { ... });
```

#### Annotate Interfaces with ImplementedBy

```csharp
// GOOD: Static analyzer can follow DI calls
[ImplementedBy(typeof(DocumentService))]
public interface IDocumentService { ... }

// BAD: Static analyzer stops at interface boundary
public interface IDocumentService { ... }
```

> **Note:** SealedFGA uses an analyzer to identify interfaces in your code that have only a single implementing class; a warning is then generated if the `ImplementedBy` attribute is not used which can be suppressed if this is deliberate.

#### Use Attribute-Based Authorization When Possible

Attribute-based authorization is automatically tracked and provides the clearest analysis:

```csharp
// BEST: Automatic tracking, zero boilerplate
[HttpGet("{id}")]
public IActionResult GetDocument(
    [FgaAuthorize(...)] DocumentEntity document
) => Ok(document);

// GOOD: Manual tracking, more flexible
[HttpGet("{id}")]
public async Task<IActionResult> GetDocument([FromRoute] DocumentEntityId id)
{
    await sealedFgaService.EnsureCheckAsync(userId, relation, id);
    var document = await dbContext.Documents.FindAsync(id);
    return Ok(document);
}
```

## Migration from Vanilla OpenFGA

If you have an existing application using the OpenFGA .NET SDK directly, here's how to migrate to SealedFGA:

### Step 1: Replace Raw IDs with Strongly-Typed IDs

**Before (Vanilla OpenFGA):**

```csharp
public class DocumentEntity
{
    public Guid Id { get; set; }                    // Raw GUID
    public Guid OwnerOrganizationId { get; set; }   // Raw GUID
    public string Title { get; set; }
}
```

**After (SealedFGA):**

```csharp
public class DocumentEntity : ISealedFgaType<DocumentEntityId>
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DocumentEntityId Id { get; set; } = null!;                      // Strongly-typed

    [SealedFgaRelation(nameof(DocumentEntityIdGroups.owner))]
    public OrganizationEntityId OwnerOrganizationId { get; set; } = null!; // Strongly-typed + automatic relation updating

    public string Title { get; set; }
}
```

### Step 2: Replace Manual Authorization Checks

**Before (Vanilla OpenFGA):**

```csharp
[HttpGet("{documentId}")]
public async Task<IActionResult> GetDocument([FromRoute] Guid documentId)
{
    // Manual authorization check
    var checkResponse = await openFgaClient.Check(new ClientCheckRequest {
        User = $"user:{GetCurrentUserId()}",
        Relation = "can_view",                      // String - typo prone & not easily refactorable!
        Object = $"document:{documentId}"
    });

    if (!(checkResponse.Allowed ?? false)) {
        return Unauthorized();
    }

    // Manual entity loading
    var document = await dbContext.Documents.FindAsync(documentId);
    if (document == null) {
        return NotFound();
    }

    return Ok(document);
}
```

**After (SealedFGA):**

```csharp
[HttpGet("{documentId}")]
public IActionResult GetDocument(
    [FromRoute] DocumentEntityId documentId,
    [FgaAuthorize(
        Relation = nameof(DocumentEntityIdAttributes.can_view),  // Strongly-typed!
        ParameterName = nameof(documentId)
    )] DocumentEntity document                                   // Automatic auth + loading!
) => Ok(document);
```

### Step 3: Replace Manual Relation Management

**Before (Vanilla OpenFGA):**

```csharp
[HttpPut("{documentId}/owner")]
public async Task<IActionResult> ChangeDocumentOwner(
    [FromRoute] Guid documentId,
    [FromBody] ChangeOwnerRequest request
)
{
    // Manual authorization
    var allowed = await openFgaClient.Check(new ClientCheckRequest {
        User = $"user:{GetCurrentUserId()}",
        Relation = "can_transfer_ownership",
        Object = $"document:{documentId}"
    });
    if (!(allowed.Allowed ?? false)) return Unauthorized();

    // Manual entity loading
    var document = await dbContext.Documents.FindAsync(documentId);
    if (document == null) return NotFound();

    // Manual relation management - error prone!
    await openFgaClient.Write(new ClientWriteRequest {
        Deletes = new List<ClientTupleKeyWithoutCondition> {
            new() {
                User = $"organization:{document.OwnerOrganizationId}",
                Relation = "owner",
                Object = $"document:{documentId}"
            }
        },
        Writes = new List<ClientTupleKey> {
            new() {
                User = $"organization:{request.NewOwnerId}",
                Relation = "owner",
                Object = $"document:{documentId}"
            }
        }
    });

    // Update entity
    document.OwnerOrganizationId = request.NewOwnerId;
    await dbContext.SaveChangesAsync();

    return Ok();
}
```

**After (SealedFGA):**

```csharp
[HttpPut("{documentId}/owner")]
public async Task<IActionResult> ChangeDocumentOwner(
    [FromRoute] DocumentEntityId documentId,
    [FgaAuthorize(
        Relation = nameof(DocumentEntityIdAttributes.can_transfer_ownership),
        ParameterName = nameof(documentId)
    )] DocumentEntity document,                          // Automatic auth + loading!
    [FromBody] ChangeOwnerRequest request
)
{
    // Simple property update - relations managed automatically!
    document.OwnerOrganizationId = request.NewOwnerId;
    await dbContext.SaveChangesAsync();                  // Automatic relation update via interceptor!

    return Ok();
}
```

### Step 4: Replace Service Layer Authorization

**Before (Vanilla OpenFGA):**

```csharp
public class DocumentService
{
    public async Task<DocumentEntity> GetDocumentAsync(Guid documentId, Guid currentUserId)
    {
        // Manual string-based authorization
        var allowed = await openFgaClient.Check(new ClientCheckRequest {
            User = $"user:{currentUserId}",
            Relation = "can_view",                       // Typo-prone string!
            Object = $"document:{documentId}"
        });

        if (!(allowed.Allowed ?? false)) {
            throw new UnauthorizedAccessException();
        }

        return await dbContext.Documents.FindAsync(documentId);
    }
}
```

**After (SealedFGA):**

```csharp
public class DocumentService
{
    public async Task<DocumentEntity> GetDocumentAsync(
        DocumentEntityId documentId,
        UserEntityId currentUserId
    ) {
        // Strongly-typed authorization with automatic error handling
        await sealedFgaService.EnsureCheckAsync(
            currentUserId,
            DocumentEntityIdAttributes.can_view,        // Compile-time checked!
            documentId
        );

        return await dbContext.Documents.FindAsync(documentId);
    }

    // Alternative: Use guard statements for static analysis
    public async Task<DocumentEntity> GetDocumentWithGuardAsync(DocumentEntityId documentId)
    {
        SealedFgaGuard.RequireCheck(
            documentId,
            DocumentEntityIdAttributes.can_view
        );

        return await dbContext.Documents.FindAsync(documentId);
    }
}
```

### Migration Checklist

- [ ] **Create `model.fga` file** with your authorization model if not already present
- [ ] **Add SealedFGA setup** to `Program.cs` (see Complete Setup Guide)
- [ ] **Replace raw ID types** with strongly-typed ID classes
- [ ] **Add `ISealedFgaType<TId>` interface** to entity classes
- [ ] **Add `[SealedFgaRelation]` attributes** to relation properties
- [ ] **Replace manual authorization** in controllers with `[FgaAuthorize]` attributes
- [ ] **Update service layer methods** to use `SealedFgaService` instead of `OpenFgaClient` where possible
- [ ] **Add `[ImplementedBy]` attributes** to interfaces for static analysis

## Conclusion

SealedFGA provides a comprehensive, type-safe approach to OpenFGA integration that catches authorization bugs at compile time while maintaining developer productivity. The multi-layer security approach ensures that authorization is handled correctly across attribute-based automation, manual service calls, and static analysis verification.

For evaluation and production use, the framework offers significant security improvements over vanilla OpenFGA SDK usage while providing performance optimizations through background processing and batch operations.
