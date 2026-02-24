# Maliev.InventoryService — Material & Stock Agent

## 🏭 Service Identity
**Service Name**: `Maliev.InventoryService`
**Role**: Manages the material inventory and stock levels in the shop floor.
**Domain**: Inventory (Material Deduction, Stock Alerts, Batch Management).

## 📜 Constitution & Compliance
This service strictly adheres to the **Maliev Technical Constitution** (see root `AGENTS.md`).
- **Framework**: .NET 10.0
- **Documentation**: Scalar UI at `/inventory/scalar`
- **Auth**: GCP-style Permissions (`inventory.stock.read`, `inventory.stock.write`)
- **Logging**: Serilog via ServiceDefaults
- **Database**: PostgreSQL (InventoryDbContext) via ServiceDefaults

## 🏗️ Architecture
- **API**: ASP.NET Core Web API
- **Events**: Consumes `JobStartedEvent` (from JobService) to passively deduct material stock.
- **Rules**: Passive estimation (no manual weighing). FIFO deduction from oldest active batch.

## 🛠️ Development Guidelines
1.  **Build**: `dotnet build Maliev.InventoryService.slnx`
2.  **Test**: `dotnet test` (Must maintain >80% coverage)
3.  **Run**: `dotnet run --project Maliev.InventoryService.Api` (Use Aspire for full stack)

## 📦 Dependencies
- `Maliev.Aspire.ServiceDefaults`: Observability, Resiliency.
- `Maliev.MessagingContracts`: Event definitions.
- `MassTransit.RabbitMQ`: Message bus.
- `Npgsql.EntityFrameworkCore.PostgreSQL`: Data persistence.
