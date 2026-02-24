# Data Model: Inventory Service

**Feature**: 001-inventory-service  
**Date**: 2026-02-21

## Entities

### BatchStatus (Enum)

Represents the depletion state of an inventory batch.

| Value | Name | Description |
|-------|------|-------------|
| 0 | Active | Batch has remaining material and participates in FIFO deduction |
| 1 | Depleted | Batch is exhausted (RemainingWeightGrams = 0) |

```csharp
namespace Maliev.InventoryService.Data.Entities;

public enum BatchStatus
{
    Active = 0,
    Depleted = 1
}
```

---

### InventoryBatch (Entity)

Represents a physical batch of raw material (e.g., a spool, container, or shipment).

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | Guid | PK, auto-generated | Unique identifier |
| MaterialId | Guid | NOT NULL, indexed | Foreign key to Material Service |
| InitialWeightGrams | decimal(18,2) | NOT NULL, > 0 | Original weight when batch was received |
| RemainingWeightGrams | decimal(18,2) | NOT NULL, >= 0, default = InitialWeightGrams | Current available weight |
| Status | BatchStatus | NOT NULL, indexed, default = Active | Current state of the batch |
| Location | string | NOT NULL, max 200 chars | Physical storage location (e.g., "Cabinet A") |
| LowStockThresholdGrams | decimal(18,2) | NOT NULL, default = 100 | Threshold for low-stock alerting |
| HasAlerted | bool | NOT NULL, default = false | Prevents duplicate low-stock alerts |
| ReceivedAt | DateTime | NOT NULL, indexed, default = UTC now | Timestamp for FIFO ordering |
| RowVersion | byte[] | NOT NULL, rowversion | Optimistic concurrency token |

```csharp
namespace Maliev.InventoryService.Data.Entities;

public class InventoryBatch
{
    public Guid Id { get; set; }
    
    public Guid MaterialId { get; set; }
    
    public decimal InitialWeightGrams { get; set; }
    
    public decimal RemainingWeightGrams { get; set; }
    
    public BatchStatus Status { get; set; } = BatchStatus.Active;
    
    public string Location { get; set; } = string.Empty;
    
    public decimal LowStockThresholdGrams { get; set; } = 100m;
    
    public bool HasAlerted { get; set; } = false;
    
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    
    public byte[] RowVersion { get; set; } = null!;
}
```

#### Indexes

| Index | Columns | Purpose |
|-------|---------|---------|
| PK_InventoryBatches | Id | Primary key |
| IX_InventoryBatches_MaterialId | MaterialId | Fast material lookup |
| IX_InventoryBatches_Status | Status | Filter by Active/Depleted |
| IX_InventoryBatches_ReceivedAt | ReceivedAt | FIFO ordering |
| IX_InventoryBatches_MaterialId_Status_ReceivedAt_Id | MaterialId, Status, ReceivedAt, Id | Composite for FIFO query optimization |

#### Validation Rules

1. `InitialWeightGrams` must be > 0 at creation
2. `RemainingWeightGrams` cannot exceed `InitialWeightGrams`
3. `RemainingWeightGrams` must be >= 0
4. `Location` is required and max 200 characters
5. `LowStockThresholdGrams` defaults to 100g if not specified
6. `ReceivedAt` is set to UTC now at creation and immutable thereafter

#### State Transitions

```
Active ──[deduction to zero]──> Depleted
```

- A batch starts as `Active` when created
- Transition to `Depleted` occurs when `RemainingWeightGrams` reaches 0 during deduction
- No transition back to `Active` (immutable)

---

## DbContext Configuration

### InventoryDbContext

```csharp
namespace Maliev.InventoryService.Data;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) 
        : base(options) { }

    public DbSet<InventoryBatch> InventoryBatches { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryBatch>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.MaterialId).IsRequired();
            
            entity.Property(e => e.InitialWeightGrams)
                .HasPrecision(18, 2)
                .IsRequired();
            
            entity.Property(e => e.RemainingWeightGrams)
                .HasPrecision(18, 2)
                .IsRequired();
            
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasDefaultValue(BatchStatus.Active)
                .IsRequired();
            
            entity.Property(e => e.Location)
                .HasMaxLength(200)
                .IsRequired();
            
            entity.Property(e => e.LowStockThresholdGrams)
                .HasPrecision(18, 2)
                .HasDefaultValue(100m)
                .IsRequired();
            
            entity.Property(e => e.HasAlerted)
                .HasDefaultValue(false)
                .IsRequired();
            
            entity.Property(e => e.ReceivedAt).IsRequired();
            
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsRequired();
            
            // Indexes
            entity.HasIndex(e => e.MaterialId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ReceivedAt);
            entity.HasIndex(e => new { e.MaterialId, e.Status, e.ReceivedAt, e.Id });
        });
    }
}
```

---

## Relationships

### External References

| Entity | External System | Reference |
|--------|-----------------|-----------|
| InventoryBatch.MaterialId | Maliev.MaterialService | Materials table (Guid) |

**Note**: No foreign key constraint - MaterialId is a logical reference to an external microservice.

---

## Migration

```bash
dotnet ef migrations add InitialInventorySchema \
  --project Maliev.InventoryService.Data \
  --startup-project Maliev.InventoryService.Api
```

---

## Query Patterns

### FIFO Batch Selection (for deduction)

```csharp
var activeBatches = await context.InventoryBatches
    .Where(b => b.MaterialId == materialId && b.Status == BatchStatus.Active)
    .OrderBy(b => b.ReceivedAt)
    .ThenBy(b => b.Id)
    .ToListAsync(cancellationToken);
```

### Status Summary (for GET endpoint)

```csharp
var summary = await context.InventoryBatches
    .Where(b => b.Status == BatchStatus.Active)
    .GroupBy(b => b.MaterialId)
    .Select(g => new
    {
        MaterialId = g.Key,
        ActiveBatches = g.Count(),
        TotalRemainingGrams = g.Sum(b => b.RemainingWeightGrams),
        LowestBatchGrams = g.Min(b => b.RemainingWeightGrams),
        HasLowStockAlert = g.Any(b => b.RemainingWeightGrams < b.LowStockThresholdGrams)
    })
    .ToListAsync(cancellationToken);
```

---

## Concurrency Handling

### Optimistic Concurrency with Retry

```csharp
async Task ExecuteWithRetry(Func<Task> operation, int maxRetries = 3)
{
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            await operation();
            return;
        }
        catch (DbUpdateConcurrencyException)
        {
            if (i == maxRetries - 1) throw;
            await Task.Delay(ExponentialBackoff(i));
        }
    }
}

TimeSpan ExponentialBackoff(int retryCount)
{
    var delay = Math.Min(100 * Math.Pow(2, retryCount), 5000);
    return TimeSpan.FromMilliseconds(delay);
}
```

**Rationale**: Spec FR-013 requires optimistic concurrency with automatic retry.
