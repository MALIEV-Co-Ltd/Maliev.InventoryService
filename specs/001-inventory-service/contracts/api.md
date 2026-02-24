# API Contracts: Inventory Service

**Feature**: 001-inventory-service  
**Date**: 2026-02-21  
**Base URL**: `/api/inventory`

## Authentication

All endpoints require JWT authentication with "Employee" role claim.

**Headers**:
```
Authorization: Bearer {jwt_token}
```

**Error Responses**:
- `401 Unauthorized` - Missing or invalid token
- `403 Forbidden` - Token valid but missing required role

---

## Endpoints

### POST /api/inventory/batches

Register a new material batch.

**Request**:
```http
POST /api/inventory/batches
Content-Type: application/json
Authorization: Bearer {token}

{
  "materialId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "initialWeightGrams": 1000.00,
  "location": "Cabinet A",
  "lowStockThresholdGrams": 100.00
}
```

**Request Body Schema**:
| Field | Type | Required | Constraints | Description |
|-------|------|----------|-------------|-------------|
| materialId | string (UUID) | Yes | Valid GUID | ID of material in Material Service |
| initialWeightGrams | number | Yes | > 0 | Initial weight in grams |
| location | string | Yes | 1-200 chars | Physical storage location |
| lowStockThresholdGrams | number | No | >= 0, default = 100 | Alert threshold in grams |

**Success Response** (201 Created):
```http
HTTP/1.1 201 Created
Location: /api/inventory/batches/{batchId}
Content-Type: application/json

{
  "id": "4a85f64-5717-4562-b3fc-2c963f66afa7",
  "materialId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "initialWeightGrams": 1000.00,
  "remainingWeightGrams": 1000.00,
  "status": "Active",
  "location": "Cabinet A",
  "lowStockThresholdGrams": 100.00,
  "receivedAt": "2026-02-21T10:00:00Z"
}
```

**Response Schema**:
| Field | Type | Description |
|-------|------|-------------|
| id | string (UUID) | Unique batch identifier |
| materialId | string (UUID) | Material reference |
| initialWeightGrams | number | Original weight |
| remainingWeightGrams | number | Current available weight |
| status | string | "Active" or "Depleted" |
| location | string | Storage location |
| lowStockThresholdGrams | number | Alert threshold |
| receivedAt | string (ISO 8601) | Registration timestamp |

**Error Responses**:
```http
HTTP/1.1 404 Not Found
Content-Type: application/json

{
  "error": "Material {materialId} not found."
}
```

```http
HTTP/1.1 400 Bad Request
Content-Type: application/json

{
  "errors": {
    "initialWeightGrams": ["Must be greater than 0"],
    "location": ["Location is required"]
  }
}
```

---

### GET /api/inventory/batches/status

Retrieve inventory status summaries grouped by material.

**Request**:
```http
GET /api/inventory/batches/status?materialId={guid}&status={status}
Authorization: Bearer {token}
```

**Query Parameters**:
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| materialId | string (UUID) | No | (all) | Filter by material ID |
| status | string | No | "Active" | Filter by status ("Active" or "Depleted") |

**Success Response** (200 OK):
```http
HTTP/1.1 200 OK
Content-Type: application/json

[
  {
    "materialId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "activeBatches": 2,
    "totalRemainingGrams": 800.00,
    "lowestBatchGrams": 300.00,
    "hasLowStockAlert": true
  },
  {
    "materialId": "5fa85f64-5717-4562-b3fc-2c963f66afa9",
    "activeBatches": 1,
    "totalRemainingGrams": 1500.00,
    "lowestBatchGrams": 1500.00,
    "hasLowStockAlert": false
  }
]
```

**Response Schema** (array of):
| Field | Type | Description |
|-------|------|-------------|
| materialId | string (UUID) | Material identifier |
| activeBatches | integer | Count of active batches |
| totalRemainingGrams | number | Sum of remaining weight across batches |
| lowestBatchGrams | number | Lowest remaining weight of any batch |
| hasLowStockAlert | boolean | True if any batch is below threshold |

**Notes**:
- Empty array returned if no batches match filters
- `status` filter only applies to batch inclusion in summary
- `hasLowStockAlert` only considers batches included in summary

---

## DTO Definitions

### CreateBatchRequest

```csharp
namespace Maliev.InventoryService.Api.DTOs;

public record CreateBatchRequest
{
    public Guid MaterialId { get; init; }
    public decimal InitialWeightGrams { get; init; }
    public string Location { get; init; } = string.Empty;
    public decimal? LowStockThresholdGrams { get; init; }
}
```

### CreateBatchResponse

```csharp
namespace Maliev.InventoryService.Api.DTOs;

public record CreateBatchResponse
{
    public Guid Id { get; init; }
    public Guid MaterialId { get; init; }
    public decimal InitialWeightGrams { get; init; }
    public decimal RemainingWeightGrams { get; init; }
    public string Status { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public decimal LowStockThresholdGrams { get; init; }
    public DateTime ReceivedAt { get; init; }
}
```

### MaterialStatusSummary

```csharp
namespace Maliev.InventoryService.Api.DTOs;

public record MaterialStatusSummary
{
    public Guid MaterialId { get; init; }
    public int ActiveBatches { get; init; }
    public decimal TotalRemainingGrams { get; init; }
    public decimal LowestBatchGrams { get; init; }
    public bool HasLowStockAlert { get; init; }
}
```

---

## Error Codes

| HTTP Status | Code | Description |
|-------------|------|-------------|
| 400 | ValidationError | Request body validation failed |
| 401 | Unauthorized | Authentication required |
| 403 | Forbidden | Insufficient permissions (missing Employee role) |
| 404 | MaterialNotFound | Material ID does not exist in Material Service |
| 500 | InternalError | Unexpected server error |

---

## Rate Limiting

Not specified in requirements. Implement if needed based on production load.
