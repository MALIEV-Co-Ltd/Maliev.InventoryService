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

- **Build**: `dotnet build Maliev.InventoryService.slnx`
- **Test (All)**: `dotnet test`
- **Test (Single)**: `dotnet test --filter "FullyQualifiedName~Namespace.ClassName.MethodName"`
- **Run API**: `dotnet run --project Maliev.InventoryService.Api`
- **Database Migrations**: `dotnet ef migrations add <MigrationName> --project Maliev.InventoryService.Infrastructure --startup-project Maliev.InventoryService.Api`
- **Database Update**: `dotnet ef database update --project Maliev.InventoryService.Infrastructure --startup-project Maliev.InventoryService.Api`

## 3. Code Style & Conventions

### General
- **Namespaces**: Use file-scoped namespaces (e.g., `namespace Maliev.InventoryService.Domain.Entities;`).
- **Formatting**: Standard C# conventions (PascalCase for classes/methods, camelCase for local variables).
- **Nullability**: `Nullable` context is ENABLED. Handle nulls explicitly. Use `?` for optional references.
- **Documentation**: XML documentation `///` is **REQUIRED** for all public methods and properties.

### Domain Entities
- **IDs**: Use `Guid` for primary keys.
- **Dates**: Use `DateTimeOffset` instead of `DateTime`.
- **Collections**: Initialize collection properties (e.g., `public ICollection<InventoryBatch> Batches { get; set; } = new List<InventoryBatch>();`).
- **Navigation Properties**: Mark as nullable if optional.

### Architecture Rules (Strict)
- **No AutoMapper**: Perform manual mapping.
- **No FluentValidation**: Use Data Annotations (`[Required]`, `[EmailAddress]`).
- **No FluentAssertions**: Use standard xUnit `Assert`.
- **No In-Memory DB**: Use **Testcontainers** for integration tests.
- **No Secrets**: Configuration via environment variables only.

## 4. Permissions

Use GCP-style permissions with plural resource format:

| Permission | Resource | Action |
|------------|----------|--------|
| `inventory.stock.read` | stock | List, Get |
| `inventory.stock.create` | stock | Add, Register |
| `inventory.stock.update` | stock | Adjust, Deduct |
| `inventory.batches.read` | batches | List, Get |
| `inventory.batches.create` | batches | Create |
| `inventory.alerts.read` | alerts | List |

## 5. Events

### Consumed
- `JobStartedEvent` — Triggers passive material deduction when job starts

### Published
- `MaterialLowStockEvent` — When stock drops below batch threshold
- `MaterialDeductedEvent` — When material is deducted from batch
- `MaterialRestockedEvent` — When new stock is registered

## 6. Testing Guidelines

- **Integration over Unit**: Prioritize integration tests using Testcontainers/PostgreSQL.
- **Naming**: `MethodName_StateUnderWhichTestIsRunning_ExpectedBehavior` (e.g., `DeductMaterial_WithSufficientStock_ReducesQuantity`).
- **Structure**: Arrange, Act, Assert comments are optional but encouraged for complex tests.

## 7. Specific Workflows

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

## 8. Agent Behavior
- **Proactive Fixes**: If you see a warning, fix it.
- **Verification**: ALWAYS run `dotnet build` after changes.
- **Safety**: Do not commit secrets.


## Database & EF Core — Mandatory Rules

### EF Core Design Package
- ❌ `Microsoft.EntityFrameworkCore.Design` MUST NOT be in Api projects
- ✅ It belongs ONLY in the Infrastructure (or Data) project where migrations live
- Migration commands must target Infrastructure, not Api:
  ```
  dotnet ef migrations add <Name> --project Maliev.<Domain>Service.Infrastructure --startup-project ../Maliev.<Domain>Service.Api
  ```

### PostgreSQL xmin Concurrency — Mandatory Pattern
Use shadow property ONLY. Never add a Xmin/xmin property to domain entities.
```csharp
entity.Property<uint>("xmin").HasColumnType("xid").IsRowVersion();
```
- ❌ Never use `UseXminAsConcurrencyToken()` (removed in Npgsql EF v7)
- ❌ Never use entity property `public uint Xmin { get; set; }` or `public uint xmin { get; set; }`
- ❌ Never use `.Ignore(e => e.Xmin)` — remove the entity property instead
