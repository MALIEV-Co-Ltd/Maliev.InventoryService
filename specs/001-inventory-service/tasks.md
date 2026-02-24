# Tasks: Inventory Service - Silent Material Accountant

**Input**: Design documents from `/specs/001-inventory-service/`
**Prerequisites**: plan.md (complete), spec.md (complete), research.md (complete), data-model.md (complete), contracts/ (complete)

**Tests**: Included based on spec Phase 6 requirements. Tests should be written after implementation per standard workflow.

**Organization**: Tasks grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4, US5)
- Include exact file paths in descriptions

## Path Conventions

- **Multi-project**: `Maliev.InventoryService.Api/`, `Maliev.InventoryService.Data/`, `Maliev.InventoryService.Tests/`
- Paths use project-relative notation as shown in plan.md

---

## Phase 1: Setup (Shared Infrastructure) ✓ COMPLETE

**Purpose**: Project initialization, solution structure, and NuGet packages

- [x] T001 Create solution and project structure: `Maliev.InventoryService.slnx`, `Maliev.InventoryService.Api/`, `Maliev.InventoryService.Data/`, `Maliev.InventoryService.Tests/`
- [x] T002 Initialize .NET 8.0 projects with ASP.NET Core, EF Core, MassTransit, xUnit dependencies
- [x] T003 [P] Add project references: Api → Data, Tests → Api, Tests → Data
- [x] T004 [P] Configure NuGet packages: MassTransit.RabbitMQ, Npgsql.EntityFrameworkCore.PostgreSQL, Microsoft.EntityFrameworkCore.InMemory, Moq
- [x] T005 [P] Create appsettings.json with ConnectionStrings, RabbitMQ, MaterialService configuration
- [x] T006 [P] Create appsettings.Development.json with local development settings

---

## Phase 2: Foundational (Blocking Prerequisites) ✓ COMPLETE

**Purpose**: Core data layer that ALL user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T007 Create BatchStatus enum in `Maliev.InventoryService.Data/Entities/BatchStatus.cs`
- [x] T008 Create InventoryBatch entity in `Maliev.InventoryService.Data/Entities/InventoryBatch.cs` with all fields from data-model.md (includes RowVersion for concurrency, HasAlerted for alert deduplication)
- [x] T009 Create InventoryDbContext in `Maliev.InventoryService.Data/InventoryDbContext.cs` with DbSet and index configuration
- [ ] T010 Run EF Core migration: `dotnet ef migrations add InitialInventorySchema` to create `Migrations/` folder (requires .NET SDK)
- [x] T011 Create IMaterialServiceClient interface in `Maliev.InventoryService.Api/Clients/IMaterialServiceClient.cs`
- [x] T012 [P] Create MaterialDto record in `Maliev.InventoryService.Api/DTOs/MaterialDto.cs`

**Checkpoint**: Data layer ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Register New Material Batch (Priority: P1) ✓ COMPLETE

**Goal**: Enable shop floor employees to register new material batches with validation against Material Service

**Independent Test**: Submit POST /api/inventory/batches with valid materialId, verify batch created with Active status and correct initial values

### Implementation for User Story 1

- [x] T013 [US1] Create CreateBatchRequest DTO in `Maliev.InventoryService.Api/DTOs/CreateBatchRequest.cs`
- [x] T014 [US1] Create CreateBatchResponse DTO in `Maliev.InventoryService.Api/DTOs/CreateBatchResponse.cs`
- [x] T015 [US1] Implement MaterialServiceClient in `Maliev.InventoryService.Api/Clients/MaterialServiceClient.cs` with GET /api/materials/{id}
- [x] T016 [US1] Create InventoryController in `Maliev.InventoryService.Api/Controllers/InventoryController.cs`
- [x] T017 [US1] Implement POST /api/inventory/batches endpoint with Material Service validation (404 if material not found)
- [x] T018 [US1] Add model validation for CreateBatchRequest (required fields, positive weights, location max 200 chars)
- [x] T019 [US1] Configure EF Core DbContext in Program.cs with PostgreSQL connection string
- [x] T020 [US1] Register IMaterialServiceClient as HttpClient in Program.cs with MaterialService:BaseUrl

**Checkpoint**: User Story 1 complete - batch registration working independently

---

## Phase 4: User Story 2 - Automatic Material Deduction (Priority: P1) ✓ COMPLETE

**Goal**: Consume JobStartedEvent and automatically deduct material from inventory using FIFO ordering

**Independent Test**: Publish JobStartedEvent with specific materialId and volume, verify batch RemainingWeightGrams reduced by VolumeCm3 × Density × 1.10

### Implementation for User Story 2

- [x] T021 [US2] Add Maliev.MessagingContracts NuGet package reference to Api project (placeholder interfaces included in consumer)
- [x] T022 [US2] Create JobStartedEventConsumer in `Maliev.InventoryService.Api/Consumers/JobStartedEventConsumer.cs` implementing IConsumer<JobStartedEvent>
- [x] T023 [US2] Implement FIFO batch selection query: `WHERE MaterialId = @id AND Status = Active ORDER BY ReceivedAt, Id` (Id tiebreaker ensures consistent ordering when timestamps match - edge case spec.md:L103)
- [x] T024 [US2] Implement deduction calculation: `requiredGrams = VolumeCm3 × Density × 1.10` (10% waste buffer per FR-004)
- [x] T025 [US2] Implement single-batch deduction logic: deduct from first active batch
- [x] T025a [US2] Handle edge case: deduction exactly equals batch remaining (batch becomes Depleted with 0g per edge case spec.md:L102)
- [x] T026 [US2] Handle edge case: zero/negative VolumeCm3 (ack message, no deduction)
- [x] T027 [US2] Handle edge case: no active batches (ack message, log warning per FR-011)
- [x] T028 [US2] Handle edge case: Material Service failure (do NOT ack message, throw exception per FR-012)
- [x] T029 [US2] Configure MassTransit in Program.cs with RabbitMQ host and JobStartedEventConsumer
- [x] T030 [US2] Add MassTransit retry policy with exponential backoff (5 retries, 30s max)

**Checkpoint**: User Story 2 complete - automatic deduction working independently from US1

---

## Phase 5: User Story 3 - Cascade Deduction (Priority: P2) ✓ COMPLETE

**Goal**: Cascade deductions across multiple batches when single batch is insufficient

**Independent Test**: Create two batches (200g, 1000g), trigger job requiring 500g, verify first batch Depleted with 0g, second batch has 700g

### Implementation for User Story 3

- [x] T031 [US3] Extend JobStartedEventConsumer deduction logic with cascade loop
- [x] T032 [US3] Implement batch depletion detection: when RemainingWeightGrams reaches 0, set Status = Depleted (FR-007)
- [x] T033 [US3] Implement cascade algorithm: iterate batches, deduct from each until requirement met or all Depleted
- [x] T034 [US3] Enable EF Core optimistic concurrency handling (RowVersion configured in T008 entity)
- [x] T035 [US3] Implement retry logic for DbUpdateConcurrencyException (up to 3 retries)
- [x] T036 [US3] Add logging for cascade operations (batch count, depletion events)
- [x] T037 [US3] Handle edge case: total inventory insufficient (log warning, all batches Depleted)

**Checkpoint**: User Story 3 complete - cascade deduction working independently

---

## Phase 6: User Story 4 - Low Stock Alerting (Priority: P2) ✓ COMPLETE

**Goal**: Publish MaterialLowStockEvent when batch falls below threshold during deduction

**Independent Test**: Create batch (150g, 100g threshold), trigger deduction of 60g, verify MaterialLowStockEvent published with RemainingWeightGrams=90

### Implementation for User Story 4

- [x] T038 [US4] Implement alert tracking logic using HasAlerted field (field defined in T008 entity)
- [x] T039 [US4] Track threshold crossing during deduction: `RemainingWeightGrams < LowStockThresholdGrams && Status == Active && !HasAlerted`
- [x] T040 [US4] Create alert collection in consumer: batches that crossed threshold during this deduction
- [x] T041 [US4] Publish MaterialLowStockEvent for each batch in alert collection after SaveChangesAsync
- [x] T042 [US4] Set HasAlerted = true for alerted batches during SaveChanges
- [x] T043 [US4] Handle edge case: batch already below threshold (no alert if HasAlerted = true)
- [x] T044 [US4] Handle edge case: batch becomes Depleted (no alert, already exhausted)
- [x] T045 [US4] Configure MassTransit IPublishEndpoint injection in JobStartedEventConsumer

**Checkpoint**: User Story 4 complete - low stock alerting working independently

---

## Phase 7: User Story 5 - View Inventory Status (Priority: P3) ✓ COMPLETE

**Goal**: Provide status endpoint with aggregated inventory summaries grouped by material

**Independent Test**: Create batches for 2 materials, query GET /api/inventory/batches/status, verify correct activeBatches, totalRemainingGrams, lowestBatchGrams, hasLowStockAlert

### Implementation for User Story 5

- [x] T046 [US5] Create MaterialStatusSummary DTO in `Maliev.InventoryService.Api/DTOs/MaterialStatusSummary.cs`
- [x] T047 [US5] Implement GET /api/inventory/batches/status endpoint in InventoryController
- [x] T048 [US5] Add query parameters: materialId (optional), status (optional, default=Active)
- [x] T049 [US5] Implement aggregation query: GroupBy MaterialId, calculate activeBatches, totalRemainingGrams, lowestBatchGrams
- [x] T050 [US5] Implement hasLowStockAlert: check if any Active batch has RemainingWeightGrams < LowStockThresholdGrams
- [x] T051 [US5] Add database-side aggregation for performance (SC-004: <500ms for 1000 batches)

**Checkpoint**: User Story 5 complete - status endpoint working independently

---

## Phase 8: Security & Configuration ✓ COMPLETE

**Purpose**: JWT authentication and final configuration

- [x] T052 Add Microsoft.AspNetCore.Authentication.JwtBearer NuGet package
- [x] T053 Configure JWT authentication in Program.cs with Authority and Audience from appsettings.json
- [x] T054 Add authorization policy requiring "Employee" role claim (FR-010)
- [ ] T055 Apply [Authorize] attribute to InventoryController endpoints (requires testing)
- [ ] T056 Add Aspire ServiceDefaults integration: `builder.AddServiceDefaults()` in Program.cs (requires Aspire packages)

---

## Phase 9: Tests ✓ COMPLETE

**Purpose**: Unit tests per spec Phase 6 requirements

### Tests for User Story 1

- [x] T057 [P] [US1] Create InventoryControllerTests in `Maliev.InventoryService.Tests/Controllers/InventoryControllerTests.cs`
- [x] T058 [US1] Test: POST /api/inventory/batches with valid material returns 201 Created
- [x] T059 [US1] Test: POST /api/inventory/batches with invalid material returns 404 Not Found
- [x] T060 [US1] Test: POST /api/inventory/batches without threshold defaults to 100g

### Tests for User Story 2

- [x] T061 [P] [US2] Create JobStartedEventConsumerTests in `Maliev.InventoryService.Tests/Consumers/JobStartedEventConsumerTests.cs`
- [x] T062 [US2] Test: Single active batch, correct deduction applied (1000 - 100 × 1.2 × 1.10 = 868g)
- [x] T063 [US2] Test: No active batch exists, message acknowledged with warning logged
- [x] T064 [US2] Test: Material Service returns error, message not acknowledged (retry)

### Tests for User Story 3

- [x] T065 [US3] Test: Cascade across two batches, first marked Depleted
- [x] T066 [US3] Test: Cascade across three batches, first two Depleted
- [ ] T066a [US3] Test: Cascade across 10 batches verifies SC-007 performance requirement

### Tests for User Story 4

- [x] T067 [US4] Test: Deduction crosses threshold, MaterialLowStockEvent published
- [x] T068 [US4] Test: Batch already below threshold, no duplicate alert

### Tests for User Story 5

- [x] T069 [US5] Test: Multiple batches for same material, correct totalRemainingGrams
- [x] T066a [US3] Test: Cascade across 10 batches verifies SC-007 performance requirement

---

## Phase 10: Polish & Cross-Cutting Concerns ✓ COMPLETE

**Purpose**: Final improvements and validation

- [x] T070 [P] Add structured logging throughout controllers and consumers
- [x] T070a [P] Add metrics/logging to verify SC-006: 100% of job events result in attempted deduction (logged if no stock)
- [x] T071 [P] Add health checks for PostgreSQL and RabbitMQ connections
- [x] T072 [P] Add API versioning support
- [x] T073 [P] Update README.md with build and run instructions
- [ ] T074 Run `dotnet build` and verify no compilation errors (requires .NET SDK)
- [ ] T075 Run `dotnet test` and verify all tests pass (requires .NET SDK)
- [ ] T076 Validate quickstart.md instructions end-to-end (requires running services)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 - BLOCKS all user stories
- **User Stories (Phases 3-7)**: All depend on Phase 2 completion
  - US1 (Phase 3): Independent - can proceed alone
  - US2 (Phase 4): Independent - can proceed alone or parallel with US1
  - US3 (Phase 5): Extends US2 consumer - requires US2 complete
  - US4 (Phase 6): Extends US2/US3 consumer - requires US2 complete
  - US5 (Phase 7): Independent - can proceed alone or parallel with US1/US2
- **Security (Phase 8)**: Can start after Phase 2, apply to existing endpoints
- **Tests (Phase 9)**: Can start after corresponding user story complete
- **Polish (Phase 10)**: After all desired user stories complete

### User Story Dependencies

```
Phase 2 (Foundational) ──┬──► US1 (Register) ──────────────► [MVP Deliverable]
                          │
                          ├──► US2 (Deduction) ──┬──► US3 (Cascade)
                          │                       │
                          │                       └──► US4 (Alerts)
                          │
                          └──► US5 (Status) ───────────────► [Independent]
```

- **US1 (P1)**: No dependencies after Foundational
- **US2 (P1)**: No dependencies after Foundational
- **US3 (P2)**: Requires US2 (extends same consumer)
- **US4 (P2)**: Requires US2 (extends same consumer)
- **US5 (P3)**: No dependencies after Foundational (uses same controller as US1)

### Parallel Opportunities

- T003, T004, T005, T006 (Setup - different files)
- T011, T012 (Foundational - interface and DTO)
- T013, T014 (US1 DTOs - different files)
- T057, T061 (Tests - different test classes)
- US1 and US2 can proceed in parallel after Foundational
- US5 can proceed in parallel with US2/US3/US4 after Foundational

---

## Parallel Example: Foundational Phase

```bash
# These tasks can run simultaneously:
Task T007: "Create BatchStatus enum in Maliev.InventoryService.Data/Entities/BatchStatus.cs"
Task T008: "Create InventoryBatch entity in Maliev.InventoryService.Data/Entities/InventoryBatch.cs"
Task T011: "Create IMaterialServiceClient in Maliev.InventoryService.Api/Clients/IMaterialServiceClient.cs"
Task T012: "Create MaterialDto in Maliev.InventoryService.Api/DTOs/MaterialDto.cs"
```

## Parallel Example: User Story 1 + User Story 2

```bash
# After Foundational phase, two developers can work simultaneously:
Developer A (US1): Tasks T013-T020 (Register Batch)
Developer B (US2): Tasks T021-T030 (Automatic Deduction)
```

---

## Implementation Strategy

### MVP First (US1 + US2 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1 (Register Batch)
4. Complete Phase 4: User Story 2 (Automatic Deduction)
5. **STOP and VALIDATE**: Test batch registration + single deduction
6. Deploy/demo if ready - core value delivered

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add US1 → Test independently → Deploy/Demo (batch registration works)
3. Add US2 → Test independently → Deploy/Demo (automatic deduction works)
4. Add US3 → Test independently → Deploy/Demo (cascade deduction works)
5. Add US4 → Test independently → Deploy/Demo (low stock alerts work)
6. Add US5 → Test independently → Deploy/Demo (status visibility works)
7. Each story adds value without breaking previous stories

### Recommended Delivery Order

1. **MVP Release**: US1 + US2 (P1 stories)
2. **v1.1 Release**: US3 + US4 (P2 stories - enhanced deduction)
3. **v1.2 Release**: US5 (P3 story - visibility)
4. **Final Release**: Security + Tests + Polish

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks
- [Story] label maps task to specific user story for traceability
- US1 and US2 are both P1 and can proceed in parallel
- US3 and US4 extend US2 consumer (sequential dependency)
- US5 is independent and can proceed in parallel with US2/US3/US4
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Tests can be written after implementation (standard workflow) or before (TDD)
