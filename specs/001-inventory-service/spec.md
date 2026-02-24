# Feature Specification: Inventory Service - Silent Material Accountant

**Feature Branch**: `001-inventory-service`  
**Created**: 2026-02-21  
**Status**: Draft  
**Input**: User description: "Maliev.InventoryService is a new microservice that passively tracks raw material consumption on the shop floor. It eliminates manual weighing by estimating material used from job data and deducting it automatically from the correct inventory batch."

## Clarifications

### Session 2026-02-21

- Q: How should concurrent JobStartedEvent deductions for the same material be handled? → A: Optimistic concurrency with retry (EF Core detects conflicts, deduction logic retries)
- Q: How should Material Service unavailability during density lookup be handled? → A: Retry with exponential backoff (auto-recovers when service returns)
- Q: What authorization mechanism should validate Employee access to API endpoints? → A: JWT validation with role claim (stateless, standard for microservices)
- Q: What does hasLowStockAlert in status response indicate? → A: True if any Active batch for material is below its threshold (immediate visibility)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Register New Material Batch (Priority: P1)

As a shop floor employee, I need to register new material batches (e.g., when a new spool arrives) so that the system can track available inventory for automatic deduction.

**Why this priority**: Without registered batches, no material tracking can occur. This is the foundation for all other functionality.

**Independent Test**: Can be fully tested by submitting a batch registration request with material ID, weight, and location, then verifying the batch is stored with correct initial values and Active status.

**Acceptance Scenarios**:

1. **Given** a valid material exists in the Material Service, **When** employee submits batch registration with materialId, initialWeightGrams (1000g), location ("Cabinet A"), and lowStockThresholdGrams (100g), **Then** a new batch is created with RemainingWeightGrams = 1000g, Status = Active, and a unique batch ID is returned
2. **Given** an invalid material ID, **When** employee attempts to register a batch, **Then** the system returns 404 Not Found
3. **Given** a valid material, **When** employee omits lowStockThresholdGrams, **Then** the system defaults to 100g

---

### User Story 2 - Automatic Material Deduction from Job Events (Priority: P1)

As a production manager, I need material consumption to be automatically deducted from inventory when a job starts, so that inventory levels remain accurate without manual weighing.

**Why this priority**: This is the core value proposition - eliminating manual weighing through passive tracking. Without this, the service provides no automation benefit.

**Independent Test**: Can be fully tested by triggering a JobStartedEvent with specific material and volume, then verifying the correct batch has its RemainingWeightGrams reduced by VolumeCm3 × Density × 1.10.

**Acceptance Scenarios**:

1. **Given** an active batch with 1000g remaining for Material X (density 1.2 g/cm³), **When** a job starts using 100cm³ of Material X, **Then** the batch's RemainingWeightGrams becomes 868g (1000 - 100 × 1.2 × 1.10)
2. **Given** no active batch exists for Material Y, **When** a job starts using Material Y, **Then** a warning is logged and the message is acknowledged
3. **Given** Material Service returns an error for material lookup, **When** a job starts, **Then** the message is not acknowledged (MassTransit retries)

---

### User Story 3 - Cascade Deduction Across Multiple Batches (Priority: P2)

As a production manager, I need material deductions to cascade across batches when a single batch cannot cover the full consumption, so that accurate tracking continues even when batches are nearly depleted.

**Why this priority**: This ensures continuous operation without manual intervention when transitioning between batches.

**Independent Test**: Can be fully tested by creating two batches for the same material (e.g., 200g and 1000g), triggering a job that consumes more than 200g, and verifying the first batch is marked Depleted and remainder is taken from the second.

**Acceptance Scenarios**:

1. **Given** Batch A has 200g remaining and Batch B has 1000g remaining (both Active, same Material), **When** a job requires 500g deduction, **Then** Batch A becomes Depleted with 0g remaining, and Batch B has 700g remaining
2. **Given** three batches with 100g, 200g, and 1000g remaining, **When** a job requires 350g deduction, **Then** first two batches become Depleted and third has 950g remaining
3. **Given** total active inventory is less than required deduction, **When** cascade deduction completes, **Then** all relevant batches are Depleted and a warning is logged

---

### User Story 4 - Low Stock Alerting (Priority: P2)

As a shop floor employee, I need to receive alerts when material batches fall below threshold levels, so that I can reorder materials before stock runs out.

**Why this priority**: This enables proactive inventory management and prevents production delays due to stockouts.

**Independent Test**: Can be fully tested by creating a batch with 150g and 100g threshold, triggering a job that consumes 60g, and verifying MaterialLowStockEvent is published with correct values.

**Acceptance Scenarios**:

1. **Given** a batch with 150g remaining and 100g threshold, **When** deduction leaves 90g remaining, **Then** MaterialLowStockEvent is published with MaterialId, BatchId, RemainingWeightGrams=90, ThresholdGrams=100
2. **Given** a batch already below threshold, **When** further deduction occurs, **Then** no duplicate alert is published for the same low-stock state
3. **Given** a batch transitions to Depleted status, **When** deduction completes, **Then** no low-stock alert is published (batch is already exhausted)

---

### User Story 5 - View Inventory Status Summary (Priority: P3)

As a production manager, I need to view current stock summaries grouped by material, so that I can understand overall inventory health at a glance.

**Why this priority**: Provides visibility for planning but is not critical for day-to-day automated operations.

**Independent Test**: Can be fully tested by creating multiple batches for different materials, then querying the status endpoint and verifying aggregated totals are correct.

**Acceptance Scenarios**:

1. **Given** Material A has 2 active batches with 500g and 300g remaining, **When** status is queried without filters, **Then** response shows Material A with activeBatches=2, totalRemainingGrams=800, lowestBatchGrams=300
2. **Given** Material B has 1 depleted batch, **When** status is queried with status=Active filter, **Then** Material B does not appear in results
3. **Given** multiple materials exist, **When** status is queried with materialId filter, **Then** only that material's summary is returned

---

### Edge Cases

- What happens when a job event references a material that doesn't exist in Material Service? Message is not acknowledged, MassTransit retries.
- What happens when deduction amount exactly equals batch remaining? Batch becomes Depleted with 0g.
- What happens when multiple batches have the same ReceivedAt timestamp? System processes them in consistent order (additional sort by Id).
- What happens when VolumeCm3 is zero or negative? No deduction occurs, message is acknowledged.
- What happens when density lookup times out? Message is not acknowledged, MassTransit retries.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow employees to register new material batches with materialId, initial weight, location, and optional low-stock threshold
- **FR-002**: System MUST validate material existence via Material Service before creating a batch, returning 404 if not found
- **FR-003**: System MUST consume JobStartedEvent messages and automatically deduct material from inventory based on volume and material density
- **FR-004**: System MUST calculate deduction as VolumeCm3 × Density × 1.10 (10% waste buffer)
- **FR-005**: System MUST apply FIFO (First-In-First-Out) deduction order, processing oldest batches first based on ReceivedAt timestamp
- **FR-006**: System MUST cascade deductions across multiple batches when a single batch cannot cover the full amount
- **FR-007**: System MUST mark batches as Depleted when their RemainingWeightGrams reaches zero
- **FR-008**: System MUST publish MaterialLowStockEvent when a batch's remaining weight falls below its threshold while still Active
- **FR-009**: System MUST provide a status endpoint returning inventory summaries grouped by material with active batch count, total remaining weight, lowest batch weight, and hasLowStockAlert=true when any Active batch's remaining weight is below its threshold
- **FR-010**: System MUST validate JWT tokens with "Employee" role claim for all API endpoints; unauthenticated requests receive 401 Unauthorized
- **FR-011**: System MUST acknowledge JobStartedEvent messages even when no active batch exists (stock-out monitoring gap, not processing error)
- **FR-012**: System MUST NOT acknowledge JobStartedEvent messages when Material Service lookup fails; retries MUST use exponential backoff for automatic recovery
- **FR-013**: System MUST handle concurrent deductions for the same material using optimistic concurrency with automatic retry on conflict detection

### Key Entities

- **InventoryBatch**: Represents a physical batch of raw material (e.g., a spool). Tracks material reference, initial weight, remaining weight, status (Active/Depleted), storage location, low-stock threshold, and registration timestamp. Supports FIFO ordering via ReceivedAt.
- **BatchStatus**: Enumeration with Active (0) and Depleted (1) states. Active batches participate in deduction; Depleted batches are exhausted.

### Event Contracts

- **MaterialLowStockEvent**: Event payload containing MaterialId, BatchId, RemainingWeightGrams, ThresholdGrams, and AlertedAt timestamp for notifying downstream systems of low inventory.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Employees can register a new material batch in under 10 seconds (single scan + confirmation; measured on LAN with typical shop floor network latency)
- **SC-002**: Material deductions from job events complete within 2 seconds of event receipt
- **SC-003**: Low-stock alerts are published within 1 second of threshold breach
- **SC-004**: Inventory status endpoint returns results in under 500ms for up to 1000 batches (measured on standard PostgreSQL instance, 2 vCPU, 4GB RAM)
- **SC-005**: Zero manual weighing required for material consumption tracking
- **SC-006**: 100% of job events result in attempted deduction (logged if no stock available)
- **SC-007**: Cascade deduction correctly handles up to 10 batches in a single transaction
