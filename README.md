# Maliev Inventory Service

[![Build Status](https://img.shields.io/badge/Build-Passing-success)](https://github.com/MALIEV-Co-Ltd/Maliev.InventoryService)
[![.NET Version](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Database](https://img.shields.io/badge/Database-PostgreSQL-blue)](https://www.postgresql.org/)

Manages material inventory and stock levels in the shop floor.

**Role in MALIEV Architecture**: The Inventory Service tracks raw material consumption. It uses passive estimation to deduct stock when jobs start, ensuring real-time visibility without manual weighing or spool scanning.

---

## 🏗️ Architecture & Tech Stack

- **Framework**: ASP.NET Core 10.0 (C# 13)
- **Database**: PostgreSQL with Entity Framework Core 10.x
- **Messaging**: RabbitMQ via MassTransit
- **API Documentation**: OpenAPI 3.1 + Scalar UI
- **Observability**: OpenTelemetry (Metrics, Traces, Logging)

---

## ⚖️ Constitution Rules

This service strictly adheres to the platform development mandates:

### Banned Libraries
- ❌ **Swagger / Swashbuckle**: Using **Scalar** for API documentation.
- ❌ **AutoMapper**: Explicit manual mapping only.
- ❌ **FluentValidation**: Standard Data Annotations or manual logic only.
- ❌ **FluentAssertions**: Standard xUnit `Assert` methods only.

### Mandatory Practices
- ✅ **TreatWarningsAsErrors**: Enabled in all `.csproj` files.
- ✅ **XML Documentation**: Required on all public methods and properties.
- ✅ **No Secrets in Code**: All sensitive configuration injected via environment variables.
- ✅ **Aspire Integration**: Fully integrated with Maliev.Aspire for local development.

---

## ✨ Key Features

- **Passive Estimation**: Automatically deducts material based on part volume and density.
- **Batch Management**: FIFO-based deduction from oldest active material batches.
- **Stock Alerts**: Publishes `MaterialLowStockEvent` when thresholds are breached.
- **Domain-specific Auth**: Fine-grained stock management permissions.

---

## 🚀 Quick Start

### Prerequisites
- .NET 10.0 SDK
- Docker Desktop (for infrastructure)
- PostgreSQL & RabbitMQ

### Local Development Setup

1. **Clone the repository**
```bash
git clone https://github.com/MALIEV-Co-Ltd/Maliev.InventoryService.git
cd Maliev.InventoryService
```

2. **Run via Aspire**
The easiest way to run the service is through the `Maliev.Aspire.AppHost` project.

3. **Manual Run**
```bash
dotnet run --project Maliev.InventoryService.Api
```

The service will be available at `http://localhost:5300/inventory`. Access the interactive documentation at `http://localhost:5300/inventory/scalar`.

---

## 📡 API Endpoints

All endpoints are prefixed with `/inventory/v1/`.

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/stock` | List current stock levels |
| POST | `/batches` | Register new material batch (e.g., new spool) |
| GET | `/batches/{id}` | Get specific batch details |
| POST | `/deduct` | Manually deduct stock (rarely used) |

---

## 🧪 Testing

```bash
dotnet test --verbosity normal
```

---

## 📦 Deployment

Deployment is managed via ArgoCD using the `maliev-gitops` repository.

---

## 📄 License

Proprietary - © 2026 MALIEV Co., Ltd. All rights reserved.
