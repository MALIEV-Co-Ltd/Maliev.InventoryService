# Quick Start: Inventory Service

**Feature**: 001-inventory-service  
**Date**: 2026-02-21

## Prerequisites

- .NET 8.0 SDK
- PostgreSQL 16+
- RabbitMQ 3.13+
- Docker (optional, for local infrastructure)

## Project Structure

```
Maliev.InventoryService/
├── Maliev.InventoryService.slnx
├── Maliev.InventoryService.Api/
│   ├── Controllers/
│   ├── Clients/
│   ├── Consumers/
│   ├── DTOs/
│   └── Program.cs
├── Maliev.InventoryService.Data/
│   ├── Entities/
│   └── InventoryDbContext.cs
└── Maliev.InventoryService.Tests/
```

## Setup Steps

### 1. Create Projects

```bash
dotnet new sln -n Maliev.InventoryService
dotnet new webapi -n Maliev.InventoryService.Api -o Maliev.InventoryService.Api
dotnet new classlib -n Maliev.InventoryService.Data -o Maliev.InventoryService.Data
dotnet new xunit -n Maliev.InventoryService.Tests -o Maliev.InventoryService.Tests

dotnet sln add Maliev.InventoryService.Api
dotnet sln add Maliev.InventoryService.Data
dotnet sln add Maliev.InventoryService.Tests
```

### 2. Add Dependencies

```bash
# Api project
dotnet add Maliev.InventoryService.Api package MassTransit.RabbitMQ
dotnet add Maliev.InventoryService.Api package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add Maliev.InventoryService.Api package Maliev.MessagingContracts

# Data project
dotnet add Maliev.InventoryService.Data package Microsoft.EntityFrameworkCore
dotnet add Maliev.InventoryService.Data package Npgsql.EntityFrameworkCore.PostgreSQL

# Tests project
dotnet add Maliev.InventoryService.Tests package Moq
dotnet add Maliev.InventoryService.Tests package Microsoft.EntityFrameworkCore.InMemory
```

### 3. Configure appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=inventory;Username=postgres;Password=postgres"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Username": "guest",
    "Password": "guest"
  },
  "MaterialService": {
    "BaseUrl": "http://localhost:5001"
  }
}
```

### 4. Run Database Migration

```bash
dotnet ef migrations add InitialInventorySchema \
  --project Maliev.InventoryService.Data \
  --startup-project Maliev.InventoryService.Api

dotnet ef database update \
  --project Maliev.InventoryService.Data \
  --startup-project Maliev.InventoryService.Api
```

### 5. Run the Service

```bash
cd Maliev.InventoryService.Api
dotnet run
```

## Testing

```bash
dotnet test
```

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | /api/inventory/batches | Register new batch |
| GET | /api/inventory/batches/status | Get inventory status |

## Event Consumers

| Event | Queue | Description |
|-------|-------|-------------|
| JobStartedEvent | inventory-job-started | Material deduction |

## Next Steps

1. Review [data-model.md](./data-model.md) for entity details
2. Review [contracts/api.md](./contracts/api.md) for API specs
3. Review [contracts/events.md](./contracts/events.md) for event contracts
4. Begin implementation following Phase 2-6 of user input plan
