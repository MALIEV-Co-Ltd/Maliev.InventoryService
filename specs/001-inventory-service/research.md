# Research: Inventory Service - Silent Material Accountant

**Feature**: 001-inventory-service  
**Date**: 2026-02-21  
**Status**: Complete

## Prerequisites Verification

### 1. Maliev.MessagingContracts Package

**Requirement**: `JobStartedEvent` and `MaterialLowStockEvent` must exist in generated C# contracts.

**Research Task**: Verify messaging contract definitions.

**Decision**: Assume standard MassTransit event structure based on spec requirements.

**JobStartedEvent Fields** (to be verified against Maliev.JobService):
```csharp
public interface JobStartedEvent
{
    Guid JobId { get; }
    Guid MaterialId { get; }
    decimal VolumeCm3 { get; }
    DateTime StartedAt { get; }
}
```

**MaterialLowStockEvent Fields** (to be created if not exists):
```csharp
public interface MaterialLowStockEvent
{
    Guid MaterialId { get; }
    Guid BatchId { get; }
    decimal RemainingWeightGrams { get; }
    decimal ThresholdGrams { get; }
    DateTime AlertedAt { get; }
}
```

**Rationale**: Standard MassTransit interface-based contracts for loose coupling.

**Action Required**: Verify against Maliev.MessagingContracts package once available.

---

### 2. Maliev.MaterialService Density Field

**Requirement**: `Density` field must be present on `GET /api/materials/{id}` response.

**Research Task**: Confirm Material Service API contract.

**Decision**: Define MaterialDto with Density field as per spec.

```json
{
  "id": "guid",
  "name": "string",
  "density": 1.2  // g/cm³
}
```

**Rationale**: Spec FR-004 requires density for calculating consumption: `VolumeCm3 × Density × 1.10`.

**Action Required**: Verify Material Service returns density field once deployed.

---

### 3. Maliev.JobService Event Field Names

**Requirement**: Confirm `JobStartedEvent` field names before writing consumer.

**Research Task**: Review JobService spec or message contract.

**Decision**: Use standard naming convention based on spec requirements:
- `JobId` (Guid) - unique job identifier
- `MaterialId` (Guid) - material being consumed
- `VolumeCm3` (decimal) - volume in cubic centimeters
- `StartedAt` (DateTime) - timestamp when job started

**Rationale**: Consistent with domain terminology in spec.

**Action Required**: Cross-reference with Maliev.JobService implementation once available.

---

## Technical Decisions

### 4. Optimistic Concurrency Strategy

**Question**: How to handle concurrent JobStartedEvent deductions for the same material?

**Decision**: EF Core optimistic concurrency with automatic retry.

**Implementation**:
```csharp
// On SaveChangesAsync, catch DbUpdateConcurrencyException
// Retry deduction logic up to 3 times with exponential backoff
```

**Rationale**: Spec clarification confirms this is the expected behavior (FR-013).

**Alternatives Considered**:
- Pessimistic locking: Rejected - would block throughput unnecessarily
- Event sourcing: Rejected - over-engineering for current scale

---

### 5. FIFO Ordering Guarantee

**Question**: How to ensure consistent FIFO order when batches have same timestamp?

**Decision**: Secondary sort by `Id` (Guid) as tiebreaker.

**Implementation**:
```csharp
batches.OrderBy(b => b.ReceivedAt).ThenBy(b => b.Id)
```

**Rationale**: Spec edge case states "System processes them in consistent order (additional sort by Id)".

---

### 6. Material Service Unavailability

**Question**: How to handle Material Service failures during density lookup?

**Decision**: Do NOT ack message; let MassTransit retry with exponential backoff.

**Implementation**:
- Configure MassTransit retry policy
- Throw exception in consumer on HTTP failure
- Message remains in queue until service recovers

**Rationale**: Spec FR-012 mandates non-acknowledgment on Material Service failure.

**Alternatives Considered**:
- Circuit breaker: Rejected - MassTransit retry handles transient failures
- Dead letter queue: Rejected - should auto-recover when service returns

---

### 7. Low Stock Alert Deduplication

**Question**: How to prevent duplicate alerts for same batch?

**Decision**: Track `HasAlerted` flag on InventoryBatch entity.

**Implementation**:
- Add `HasAlerted` boolean field to InventoryBatch
- Only publish MaterialLowStockEvent if `!HasAlerted && RemainingWeightGrams < ThresholdGrams`
- Set `HasAlerted = true` after first alert

**Rationale**: Spec User Story 4 states "no duplicate alert is published for the same low-stock state".

---

### 8. Cascade Deduction Algorithm

**Question**: Exact implementation of FIFO cascade across batches?

**Decision**: Iterative deduction with batch depletion tracking.

**Algorithm**:
```
1. Get all Active batches for MaterialId, ordered by ReceivedAt then Id
2. Calculate requiredGrams = VolumeCm3 × Density × 1.10
3. For each batch in order:
   a. If requiredGrams <= 0, break
   b. If batch.RemainingGrams >= requiredGrams:
      - Deduct requiredGrams from batch
      - requiredGrams = 0
      - Break
   c. Else:
      - requiredGrams -= batch.RemainingGrams
      - batch.RemainingGrams = 0
      - batch.Status = Depleted
4. If requiredGrams > 0, log warning (insufficient stock)
5. SaveChanges
6. For each modified batch that crossed threshold, publish alert
```

**Rationale**: Spec FR-006 requires cascade, FR-005 requires FIFO, FR-007 requires Depleted marking.

---

## Technology Stack

### Confirmed Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| .NET | 8.0 | Runtime |
| ASP.NET Core | 8.0 | Web API |
| EF Core | 8.0 | ORM |
| Npgsql.EntityFrameworkCore.PostgreSQL | 8.0 | PostgreSQL provider |
| MassTransit | 8.x | Message bus |
| MassTransit.RabbitMQ | 8.x | RabbitMQ transport |
| xunit | 2.x | Testing |
| Moq | 4.x | Mocking |
| Microsoft.NET.Test.Sdk | 17.x | Test runner |

### Architecture Pattern

**Pattern**: Vertical Slice Architecture (minimal layers)

**Layers**:
1. **API Layer**: Controllers + MassTransit consumers
2. **Data Layer**: EF Core DbContext + entities
3. **Integration Layer**: HTTP clients for external services

**Rationale**: Simplicity for microservice scope (no domain/application layer split needed).

---

## Open Questions

1. **Q**: Connection string configuration via Aspire ServiceDefaults?
   **A**: Use `builder.AddServiceDefaults()` pattern (assumed standard Aspire approach)

2. **Q**: JWT validation - shared authority across services?
   **A**: Assume centralized auth service (configure via `JwtSettings:Authority`)

3. **Q**: MassTransit endpoint naming convention?
   **A**: Use default MassTransit conventions (queue per consumer type)

---

## Summary

All NEEDS CLARIFICATION items from Technical Context have been resolved through spec analysis and domain reasoning. Key decisions:

1. **Optimistic concurrency** with EF Core retry (FR-013)
2. **FIFO with Guid tiebreaker** for consistent ordering (edge case spec)
3. **No message ack on Material Service failure** (FR-012)
4. **HasAlerted flag** for deduplication (User Story 4)
5. **Vertical slice architecture** for simplicity

**Next Phase**: Proceed to data-model.md and contract definitions.
