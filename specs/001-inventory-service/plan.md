# Implementation Plan: Inventory Service - Silent Material Accountant

**Branch**: `001-inventory-service` | **Date**: 2026-02-21 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-inventory-service/spec.md`

## Summary

Maliev.InventoryService is a .NET microservice that passively tracks raw material consumption by consuming JobStartedEvent messages and automatically deducting material from inventory batches. It eliminates manual weighing by estimating consumption from job volume data and material density, with FIFO-based cascade deduction across batches and low-stock alerting.

## Technical Context

**Language/Version**: C# .NET 10.0 (matching Maliev.JobService pattern from spec)
**Primary Dependencies**: ASP.NET Core, Entity Framework Core, MassTransit, PostgreSQL, Aspire ServiceDefaults (.NET Aspire orchestration, service discovery, health checks, and OpenTelemetry)
**Storage**: PostgreSQL (inventory batches, status tracking)
**Testing**: xUnit, EF Core InMemory provider, MassTransit test harness
**Target Platform**: Linux container (Kubernetes/Docker)
**Project Type**: web-service (microservice with REST API + message consumer)
**Performance Goals**: <2s deduction latency, <500ms status query, <1s alert publishing
**Constraints**: 10% waste buffer in calculations, FIFO ordering mandatory, optimistic concurrency
**Scale/Scope**: Up to 1000 batches, 10 batches per cascade transaction, 100 req/s

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

No constitution file found in `.specify/memory/constitution.md`. Using default architectural guidelines:

| Principle | Status | Notes |
|-----------|--------|-------|
| Multi-project separation | ✓ PASS | Api/Data/Tests follows Maliev.JobService pattern |
| Test coverage | ✓ PASS | Unit + integration tests planned (Phase 6) |
| Simplicity | ✓ PASS | Minimal layers: controller → service → db |
| Dependency count | ✓ PASS | Only essential deps: EF Core, MassTransit, HttpClient |

## Project Structure

### Documentation (this feature)

```text
specs/001-inventory-service/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (API contracts)
└── tasks.md             # Phase 2 output (not created yet)
```

### Source Code (repository root)

```text
Maliev.InventoryService/
├── Maliev.InventoryService.slnx
├── Maliev.InventoryService.Api/
│   ├── Controllers/
│   │   └── InventoryController.cs
│   ├── Clients/
│   │   ├── IMaterialServiceClient.cs
│   │   └── MaterialServiceClient.cs
│   ├── Consumers/
│   │   └── JobStartedEventConsumer.cs
│   ├── DTOs/
│   │   └── MaterialDto.cs
│   ├── Program.cs
│   └── appsettings.json
├── Maliev.InventoryService.Data/
│   ├── Entities/
│   │   ├── InventoryBatch.cs
│   │   └── BatchStatus.cs
│   └── InventoryDbContext.cs
└── Maliev.InventoryService.Tests/
    ├── Consumers/
    │   └── JobStartedEventConsumerTests.cs
    └── Controllers/
        └── InventoryControllerTests.cs
```

**Structure Decision**: Multi-project layered architecture (Api/Data/Tests) matching Maliev.JobService pattern from the implementation plan prerequisites.

## Complexity Tracking

> No constitution violations detected. Standard microservice pattern.

| Aspect | Justification |
|--------|---------------|
| 3 projects | Standard .NET separation: Api (presentation), Data (persistence), Tests |
| HTTP Client | Required for Material Service integration (density lookup) |
| MassTransit | Required for async event consumption (matches ecosystem) |

## Phase 0: Research

**Status**: COMPLETE

See [research.md](./research.md) for detailed findings.

Key decisions made:
1. Optimistic concurrency with EF Core RowVersion and automatic retry
2. FIFO ordering with composite index (MaterialId, Status, ReceivedAt, Id)
3. HasAlerted flag for low-stock deduplication
4. Multi-project layered architecture for simplicity (minimal layers: Api/Data/Tests)

## Phase 1: Design Artifacts

**Status**: COMPLETE

Generated artifacts:
- [x] `data-model.md` - Entity definitions and relationships
- [x] `contracts/api.md` - REST API contracts
- [x] `contracts/events.md` - MassTransit event contracts
- [x] `quickstart.md` - Development setup guide
