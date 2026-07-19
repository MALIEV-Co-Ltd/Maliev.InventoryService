# Maliev.InventoryService — Material & Stock Agent

This document contains instructions for AI agents operating in this repository.

## 1. Service Scope

**Service Name**: `Maliev.InventoryService`
**Role**: Manages the material inventory and stock levels in the shop floor.
**Domain**: Inventory (Material Deduction, Stock Alerts, Batch Management).

### Key Responsibilities
- **Material Tracking**: Track all consumables (not just filament/resin) — includes CNC cutting tools, build plates, post-processing supplies
- **Passive Deduction**: Automatically deduct material when job starts (no manual weighing)
- **FIFO Batches**: Deduct from oldest active batch first
- **Stock Alerts**: Publish `MaterialLowStockEvent` when stock drops below threshold
- **Event Consumption**: Consumes `JobStartedEvent` from JobService

## 2. Environment & Build

- **Framework**: .NET 10.0 (C# 13)
- **Database**: PostgreSQL 18 (using Entity Framework Core 10)
- **Architecture**: Clean Architecture (Api, Application, Domain, Infrastructure, Tests)
- **TreatWarningsAsErrors**: ENABLED. Zero compilation warnings allowed.
- **Documentation**: Scalar UI at `/inventory/scalar`

### Commands

All commands run from within this service directory (`B:\maliev\Maliev.InventoryService`).

```powershell
# Build (treats warnings as errors — all must be fixed)
dotnet build Maliev.InventoryService.slnx

# Run all tests
dotnet test Maliev.InventoryService.slnx --verbosity normal

# Run a single test method
dotnet test --filter "FullyQualifiedName~Namespace.ClassName.MethodName"

# Run all tests in a class
dotnet test --filter "FullyQualifiedName~Namespace.ClassName"

# Run with code coverage
dotnet test Maliev.InventoryService.slnx --collect:"XPlat Code Coverage"

# Format check
dotnet format Maliev.InventoryService.slnx

# Run API
dotnet run --project Maliev.InventoryService.Api

# EF Core migrations (Infrastructure project only)
dotnet ef migrations add <MigrationName> --project Maliev.InventoryService.Infrastructure --startup-project Maliev.InventoryService.Infrastructure

# Database update
dotnet ef database update --project Maliev.InventoryService.Infrastructure --startup-project Maliev.InventoryService.Infrastructure
```

## 3. Code Style & Conventions

### Workspace Structure
```
Maliev.InventoryService/
├── Maliev.InventoryService.Api/           # Controllers, Consumers, Middleware
├── Maliev.InventoryService.Application/   # Use cases, DTOs, Interfaces, Handlers
├── Maliev.InventoryService.Domain/        # Entities, value objects, domain interfaces
├── Maliev.InventoryService.Infrastructure/ # EF Core DbContext, repositories, HTTP clients
├── Maliev.InventoryService.Tests/         # Unit + Integration tests (xUnit)
├── Directory.Build.props                  # Central package versioning
└── Maliev.InventoryService.slnx          # Solution file (.slnx preferred over .sln)
```

### C# Naming & Formatting
- **Namespaces**: File-scoped (`namespace Maliev.InventoryService.Domain.Entities;`)
- **Classes/Methods/Properties**: `PascalCase`
- **Private fields**: `_camelCase` (underscore prefix)
- **Parameters/locals**: `camelCase`
- **Async methods**: Suffix with `Async` (e.g., `DeductMaterialAsync`)
- **Interfaces**: Prefix with `I` (e.g., `IInventoryRepository`)
- **Permissions**: GCP-style `{domain}.{plural-resource}.{action}` as `public const string` in a `Permissions` static class
  - Valid: `inventory.stock.read`, `inventory.batches.create`
  - Invalid: `inventory.stock` (missing action), `inventory.batch.create` (singular)
- **XML docs**: Required on ALL public methods and properties
- **Nullable**: Enabled (`<Nullable>enable</Nullable>`). Use `?` explicitly
- **Imports**: System first, then third-party, then local. Alphabetize within groups. Remove unused `using`
- **Braces**: Allman style (new line) for methods and control structures. Expression-bodied for properties/accessors
- **Indentation**: 4 spaces, LF line endings, UTF-8, trim trailing whitespace

### C# Patterns
- **DI**: Constructor injection with `private readonly` fields
- **Controllers**: `[ApiController]`, `[ApiVersion("1")]`, `[Route("inventory/v{version:apiVersion}")]`
- **Logging**: `ILogger<T>` with structured placeholders (never interpolate): `_logger.LogInformation("Processing {BatchId}", batchId)`
- **Error handling**: Global exception middleware. Return `ProblemDetails` / `ErrorResponse` DTOs. Never expose stack traces
- **JSON**: Snake_case_lower for Auth service (`JsonNamingPolicy.SnakeCaseLower`); other services may vary — check existing conventions
- **Manual mapping**: Static extension methods (`ToDto()`, `ToEntity()`). AutoMapper is banned
- **Validation**: `System.ComponentModel.DataAnnotations` on DTOs. FluentValidation is banned

### Domain Entities
- **IDs**: Use `Guid` for primary keys.
- **Dates**: Use `DateTimeOffset` instead of `DateTime`.
- **Collections**: Initialize collection properties (e.g., `public ICollection<InventoryBatch> Batches { get; set; } = new List<InventoryBatch>();`).
- **Navigation Properties**: Mark as nullable if optional.

## 4. Banned Libraries (Build Will Fail)

| Banned | Use Instead |
|--------|-------------|
| AutoMapper | Manual mapping extensions |
| FluentValidation | DataAnnotations or manual validation |
| FluentAssertions | Standard xUnit `Assert.*` |
| Swashbuckle/Swagger | Scalar (at `/inventory/scalar`) |
| InMemoryDatabase (EF Core) | Testcontainers with real PostgreSQL |

## 5. Permissions

Use GCP-style permissions with plural resource format:

| Permission | Resource | Action |
|------------|----------|--------|
| `inventory.stock.read` | stock | List, Get |
| `inventory.stock.create` | stock | Add, Register |
| `inventory.stock.update` | stock | Adjust, Deduct |
| `inventory.batches.read` | batches | List, Get |
| `inventory.batches.create` | batches | Create |
| `inventory.alerts.read` | alerts | List |

## 6. Events

### Consumed
- `JobStartedEvent` — Triggers passive material deduction when job starts

### Published
- `MaterialLowStockEvent` — When stock drops below batch threshold
- `MaterialDeductedEvent` — When material is deducted from batch
- `MaterialRestockedEvent` — When new stock is registered

## 7. Testing Guidelines

- **Framework**: xUnit with standard `Assert` (`Assert.Equal`, `Assert.NotNull`, etc.)
- **Naming**: `MethodName_StateUnderTest_ExpectedBehavior` or `HTTP_METHOD_Path_Scenario_ExpectedStatus`
- **Coverage**: Minimum 80% per service
- **Integration tests**: `BaseIntegrationTestFactory<TProgram, TDbContext>` with Testcontainers (PostgreSQL, Redis, RabbitMQ). Never InMemoryDatabase
- **System tests** (Tier 3): `AspireTestFixture` with `[Collection("AspireDomainTests")]` — shared AppHost, never one per class
- **Eventual consistency**: Use `TestHelpers.WaitForAsync`. Never `Task.Delay`
- **MassTransit consumers**: Must have consumer tests using `AddMassTransitTestHarness()`

### Testing Strategy (4-Tier Pyramid Context)

This service's tests cover **Tier 1 (Unit)** and **Tier 2 (Service Integration)** of the Maliev testing pyramid:

| Tier | What to Test | Infrastructure |
|------|-------------|---------------|
| **Unit** | Business logic, domain models, service methods with mocked dependencies | None (mocks only) |
| **Service Integration** | API endpoints, database persistence, permission enforcement, input validation | `BaseIntegrationTestFactory` + Testcontainers (Postgres/Redis/RabbitMQ) |

**Tier 3 (System Integration)** — cross-service workflows and event chains — is tested in `Maliev.Aspire.Tests/`.

> Full ecosystem test strategy: `Maliev.Aspire.Tests/TEST_PLAN.md`

## 8. Mandatory Rules

- **`TreatWarningsAsErrors = true`**: Zero warnings allowed. No suppression
- **`[RequirePermission("inventory.resources.action")]`**: On all endpoints, not plain `[Authorize]`
- **API versioning**: All routes versioned (`v1/`)
- **Service prefix**: Routes prefixed with service domain (`/inventory`)
- **Scalar docs**: Configured at `/inventory/scalar`
- **Secrets**: Never hardcoded. Use GCP Secret Manager or environment variables
- **Async/await**: All the way down. Pass `CancellationToken`
- **EF Core Design package**: Only in Infrastructure project, never in Api
- **PostgreSQL xmin**: Shadow property only — `entity.Property<uint>("xmin").HasColumnType("xid").IsRowVersion()`. Never add entity property
- **Temporary files**: Generate in `/temp` folder, clean up afterwards

## 9. Specific Workflows

### Material Deduction Formula
When a job starts:
```
deduction = VolumeCm3 × Density × 1.10  // 10% buffer for waste
```

### Adding a New Consumable Type
1. Add Entity in `Domain` (e.g., `CuttingTool`, `BuildPlate`)
2. Create Repository Interface in `Domain/Interfaces`
3. Implement Repository in `Infrastructure`
4. Add deduction logic in Application layer
5. Add Integration Tests

### Restocking
1. Employee scans/registers new item when opening
2. System creates new batch with current timestamp
3. FIFO ensures oldest batch is used first

## 10. Agent Behavior
- **Proactive Fixes**: If you see a warning, fix it.
- **Verification**: ALWAYS run `dotnet build` after changes.
- **Safety**: Do not commit secrets.

## Git Rules

- Each `Maliev.*` folder is an independent git repo. `cd` into it before git commands
- **Commit early and often** after every meaningful unit of work. Do not accumulate changes
- **Never use `git checkout` to restore files** — commit first, then `git revert` or `git reset --soft`
- Feature branches merged to `develop` via PR. Do not push without being asked

## Database & EF Core — Mandatory Rules

### EF Core Design Package
- `Microsoft.EntityFrameworkCore.Design` MUST NOT be in Api projects
- It belongs ONLY in the Infrastructure (or Data) project where migrations live
- Migration commands must target Infrastructure as both project and startup-project:
  ```
  dotnet ef migrations add <Name> --project Maliev.InventoryService.Infrastructure --startup-project Maliev.InventoryService.Infrastructure
  ```

### PostgreSQL xmin Concurrency — Mandatory Pattern
Use shadow property ONLY. Never add a Xmin/xmin property to domain entities.
```csharp
entity.Property<uint>("xmin").HasColumnType("xid").IsRowVersion();
```
- Never use `UseXminAsConcurrencyToken()` (removed in Npgsql EF v7)
- Never use entity property `public uint Xmin { get; set; }` or `public uint xmin { get; set; }`
- Never use `.Ignore(e => e.Xmin)` — remove the entity property instead
