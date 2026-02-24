# Event Contracts: Inventory Service

**Feature**: 001-inventory-service  
**Date**: 2026-02-21  
**Transport**: MassTransit over RabbitMQ

## Consumed Events

### JobStartedEvent

**Source**: Maliev.JobService  
**Purpose**: Trigger automatic material deduction when a job starts

**Contract** (defined in Maliev.MessagingContracts):
```csharp
namespace Maliev.MessagingContracts.Events;

public interface JobStartedEvent
{
    Guid JobId { get; }
    Guid MaterialId { get; }
    decimal VolumeCm3 { get; }
    DateTime StartedAt { get; }
}
```

**Field Definitions**:
| Field | Type | Description |
|-------|------|-------------|
| JobId | Guid | Unique job identifier |
| MaterialId | Guid | Material being consumed |
| VolumeCm3 | decimal | Volume in cubic centimeters |
| StartedAt | DateTime | UTC timestamp when job started |

**Consumer Logic**:
1. Look up material density from MaterialService
2. Calculate required grams: `VolumeCm3 × Density × 1.10`
3. Deduct from active batches using FIFO with cascade
4. Publish MaterialLowStockEvent for batches crossing threshold

**Message Handling**:
- **Acknowledge** if: No active batch exists (log warning, FR-011)
- **Acknowledge** if: VolumeCm3 <= 0 (no deduction needed)
- **Do NOT acknowledge** if: MaterialService lookup fails (FR-012)
- **Do NOT acknowledge** if: Concurrency conflict (retry automatically)

**Queue Name**: `inventory-job-started`

**Retry Policy**: Exponential backoff, 5 retries, max 30-second delay

---

## Published Events

### MaterialLowStockEvent

**Destination**: Downstream consumers (purchasing, notifications)  
**Purpose**: Alert when a batch falls below threshold during deduction

**Contract** (to be defined in Maliev.MessagingContracts):
```csharp
namespace Maliev.MessagingContracts.Events;

public interface MaterialLowStockEvent
{
    Guid MaterialId { get; }
    Guid BatchId { get; }
    decimal RemainingWeightGrams { get; }
    decimal ThresholdGrams { get; }
    DateTime AlertedAt { get; }
}
```

**Field Definitions**:
| Field | Type | Description |
|-------|------|-------------|
| MaterialId | Guid | Material identifier for restocking |
| BatchId | Guid | Specific batch that triggered alert |
| RemainingWeightGrams | decimal | Current remaining weight |
| ThresholdGrams | decimal | The threshold that was crossed |
| AlertedAt | DateTime | UTC timestamp when alert triggered |

**Publish Timing**:
- After successful `SaveChangesAsync()` in consumer
- Only for batches that crossed threshold during current deduction
- Only if `HasAlerted` is false (deduplication)

**Exchange**: `material-low-stock`

**Routing Key**: `{MaterialId}` (allows per-material subscriptions)

---

## MassTransit Configuration

### Consumer Registration

```csharp
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<JobStartedEventConsumer>();
    
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"], h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"]);
            h.Password(builder.Configuration["RabbitMQ:Password"]);
        });
        
        cfg.ReceiveEndpoint("inventory-job-started", e =>
        {
            e.ConfigureConsumer<JobStartedEventConsumer>(context);
            
            // Retry policy for transient failures
            e.UseMessageRetry(r => r
                .Exponential(5, TimeSpan.FromSeconds(1), 
                           TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2)));
        });
    });
});
```

### Publisher Registration

```csharp
// IPublishEndpoint is automatically registered by AddMassTransit
// Inject into consumer:
public class JobStartedEventConsumer : IConsumer<JobStartedEvent>
{
    private readonly IPublishEndpoint _publishEndpoint;
    
    public JobStartedEventConsumer(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }
}
```

---

## Event Flow Diagram

```
┌─────────────────┐
│  JobService     │
│  (Publisher)    │
└────────┬────────┘
         │ JobStartedEvent
         ▼
┌─────────────────┐         ┌─────────────────┐
│  RabbitMQ       │         │ MaterialService │
│  Exchange       │         │  (HTTP API)     │
└────────┬────────┘         └────────┬────────┘
         │                           │
         │                           │ GET /api/materials/{id}
         ▼                           │
┌─────────────────┐                  │
│  InventorySvc   │◄─────────────────┘
│  (Consumer)     │
└────────┬────────┘
         │ MaterialLowStockEvent
         │ (after deduction)
         ▼
┌─────────────────┐
│  RabbitMQ       │
│  Exchange       │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Downstream     │
│  Consumers      │
│  (Purchasing,   │
│   Notifications)│
└─────────────────┘
```

---

## Concurrency Considerations

### Concurrent JobStartedEvent Handling

**Scenario**: Multiple jobs for same material arrive simultaneously

**Solution**: Optimistic concurrency via RowVersion

1. Consumer loads batches
2. Performs deduction
3. Attempts SaveChangesAsync
4. If DbUpdateConcurrencyException:
   - Retry entire deduction (reload batches)
   - Up to 3 retries with exponential backoff
   - If still failing, do NOT ack message (MassTransit retries)

**Implementation**:
```csharp
public async Task Consume(ConsumeContext<JobStartedEvent> context)
{
    const int maxRetries = 3;
    for (int attempt = 0; attempt < maxRetries; attempt++)
    {
        try
        {
            var material = await _materialClient.GetMaterialAsync(...);
            var batches = await _db.InventoryBatches
                .Where(b => b.MaterialId == materialId && b.Status == Active)
                .OrderBy(b => b.ReceivedAt).ThenBy(b => b.Id)
                .ToListAsync();
            
            // ... deduction logic ...
            
            await _db.SaveChangesAsync();
            
            // ... publish alerts ...
            
            return; // Success - message acked
        }
        catch (DbUpdateConcurrencyException)
        {
            if (attempt == maxRetries - 1) throw;
            await Task.Delay(ExponentialBackoff(attempt));
            _db.ChangeTracker.Clear();
        }
    }
}
```

---

## Monitoring

### Key Metrics

| Metric | Type | Description |
|--------|------|-------------|
| inventory.events.consumed | Counter | JobStartedEvent messages consumed |
| inventory.events.published | Counter | MaterialLowStockEvent messages published |
| inventory.deduction.duration | Histogram | Time to complete deduction |
| inventory.deduction.cascade_depth | Histogram | Number of batches in cascade |
| inventory.alerts.threshold_crossed | Counter | Low-stock alerts published |

### Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString)
    .AddRabbitMQ(rabbitConnectionString);
```
