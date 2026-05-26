using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Maliev.InventoryService.Application.Abstractions;
using Maliev.InventoryService.Application.Models;
using Maliev.InventoryService.Domain.Entities;
using Maliev.InventoryService.Infrastructure.Persistence;
using InventoryServiceImpl = Maliev.InventoryService.Infrastructure.Services.InventoryService;
using ILoggerInv = Microsoft.Extensions.Logging.ILogger<Maliev.InventoryService.Infrastructure.Services.InventoryService>;

namespace Maliev.InventoryService.Tests.Services;

/// <summary>
/// Unit tests for InventoryService in the Infrastructure layer.
/// </summary>
public class InventoryServiceTests : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private readonly Mock<ILoggerInv> _loggerMock;
    private InventoryDbContext _context = null!;

    public InventoryServiceTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _loggerMock = new Mock<ILoggerInv>();
    }

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;
        _context = new InventoryDbContext(options);
        await _context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    private async Task ClearDataAsync()
    {
        _context.InventoryConsumptionEvents.RemoveRange(_context.InventoryConsumptionEvents);
        _context.InventoryBatches.RemoveRange(_context.InventoryBatches);
        await _context.SaveChangesAsync();
    }

    private InventoryServiceImpl CreateService()
    {
        return new InventoryServiceImpl(_context, _loggerMock.Object);
    }

    /// <summary>
    /// Verifies that CreateBatchAsync creates a batch with correct values.
    /// </summary>
    [Fact]
    public async Task CreateBatchAsync_WithValidRequest_CreatesBatch()
    {
        await ClearDataAsync();
        var service = CreateService();

        var request = new CreateBatchRequest
        {
            MaterialId = Guid.NewGuid(),
            InitialWeightGrams = 1000m,
            Location = "Cabinet A",
            LowStockThresholdGrams = 200m
        };

        var result = await service.CreateBatchAsync(request, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(request.MaterialId, result.MaterialId);
        Assert.Equal(1000m, result.InitialWeightGrams);
        Assert.Equal(1000m, result.RemainingWeightGrams);
        Assert.Equal("Active", result.Status);
        Assert.Equal("Cabinet A", result.Location);
        Assert.Equal(200m, result.LowStockThresholdGrams);
    }

    /// <summary>
    /// Verifies that CreateBatchAsync uses default threshold when not specified.
    /// </summary>
    [Fact]
    public async Task CreateBatchAsync_WithoutThreshold_UsesDefault100()
    {
        await ClearDataAsync();
        var service = CreateService();

        var request = new CreateBatchRequest
        {
            MaterialId = Guid.NewGuid(),
            InitialWeightGrams = 1000m,
            Location = "Cabinet A",
            LowStockThresholdGrams = null
        };

        var result = await service.CreateBatchAsync(request, CancellationToken.None);

        Assert.Equal(100m, result.LowStockThresholdGrams);
    }

    /// <summary>
    /// Verifies that GetStatusAsync returns all active batches when no filter.
    /// </summary>
    [Fact]
    public async Task GetStatusAsync_NoFilters_ReturnsAllBatches()
    {
        await ClearDataAsync();
        var materialId = Guid.NewGuid();
        
        _context.InventoryBatches.AddRange(
            new InventoryBatch { Id = Guid.NewGuid(), MaterialId = materialId, InitialWeightGrams = 1000m, RemainingWeightGrams = 800m, Status = BatchStatus.Active, Location = "A", LowStockThresholdGrams = 100m, ReceivedAt = DateTime.UtcNow },
            new InventoryBatch { Id = Guid.NewGuid(), MaterialId = materialId, InitialWeightGrams = 500m, RemainingWeightGrams = 500m, Status = BatchStatus.Active, Location = "B", LowStockThresholdGrams = 50m, ReceivedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        var service = CreateService();

        var result = await service.GetStatusAsync(null, null, CancellationToken.None);

        Assert.Single(result);
        var summary = result.First();
        Assert.Equal(materialId, summary.MaterialId);
        Assert.Equal(2, summary.ActiveBatches);
        Assert.Equal(1300m, summary.TotalRemainingGrams);
        Assert.Equal(500m, summary.LowestBatchGrams);
    }

    /// <summary>
    /// Verifies that GetStatusAsync filters by material ID.
    /// </summary>
    [Fact]
    public async Task GetStatusAsync_WithMaterialIdFilter_ReturnsFiltered()
    {
        await ClearDataAsync();
        var materialId1 = Guid.NewGuid();
        var materialId2 = Guid.NewGuid();
        
        _context.InventoryBatches.AddRange(
            new InventoryBatch { Id = Guid.NewGuid(), MaterialId = materialId1, InitialWeightGrams = 1000m, RemainingWeightGrams = 800m, Status = BatchStatus.Active, Location = "A", LowStockThresholdGrams = 100m, ReceivedAt = DateTime.UtcNow },
            new InventoryBatch { Id = Guid.NewGuid(), MaterialId = materialId2, InitialWeightGrams = 500m, RemainingWeightGrams = 500m, Status = BatchStatus.Active, Location = "B", LowStockThresholdGrams = 50m, ReceivedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        var service = CreateService();

        var result = await service.GetStatusAsync(materialId1, null, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(materialId1, result.First().MaterialId);
    }

    /// <summary>
    /// Verifies that GetStatusAsync filters by status.
    /// </summary>
    [Fact]
    public async Task GetStatusAsync_WithStatusFilter_ReturnsFiltered()
    {
        await ClearDataAsync();
        var materialId = Guid.NewGuid();
        
        _context.InventoryBatches.AddRange(
            new InventoryBatch { Id = Guid.NewGuid(), MaterialId = materialId, InitialWeightGrams = 1000m, RemainingWeightGrams = 800m, Status = BatchStatus.Active, Location = "A", LowStockThresholdGrams = 100m, ReceivedAt = DateTime.UtcNow },
            new InventoryBatch { Id = Guid.NewGuid(), MaterialId = materialId, InitialWeightGrams = 500m, RemainingWeightGrams = 0m, Status = BatchStatus.Depleted, Location = "B", LowStockThresholdGrams = 50m, ReceivedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        var service = CreateService();

        var result = await service.GetStatusAsync(null, "Active", CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(1, result.First().ActiveBatches);
        Assert.Equal(800m, result.First().TotalRemainingGrams);
    }

    /// <summary>
    /// Verifies that GetStatusAsync detects low stock alert.
    /// </summary>
    [Fact]
    public async Task GetStatusAsync_WithLowStockBatch_DetectsAlert()
    {
        await ClearDataAsync();
        var materialId = Guid.NewGuid();
        
        _context.InventoryBatches.AddRange(
            new InventoryBatch { Id = Guid.NewGuid(), MaterialId = materialId, InitialWeightGrams = 1000m, RemainingWeightGrams = 500m, Status = BatchStatus.Active, Location = "A", LowStockThresholdGrams = 100m, ReceivedAt = DateTime.UtcNow },
            new InventoryBatch { Id = Guid.NewGuid(), MaterialId = materialId, InitialWeightGrams = 200m, RemainingWeightGrams = 50m, Status = BatchStatus.Active, Location = "B", LowStockThresholdGrams = 100m, ReceivedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        var service = CreateService();

        var result = await service.GetStatusAsync(null, null, CancellationToken.None);

        Assert.Single(result);
        Assert.True(result.First().HasLowStockAlert);
    }

    /// <summary>
    /// Verifies that GetStatusAsync returns empty when no batches exist.
    /// </summary>
    [Fact]
    public async Task GetStatusAsync_NoBatches_ReturnsEmpty()
    {
        await ClearDataAsync();
        var service = CreateService();

        var result = await service.GetStatusAsync(null, null, CancellationToken.None);

        Assert.Empty(result);
    }

    /// <summary>
    /// Verifies that CreateBatchAsync persists to database.
    /// </summary>
    [Fact]
    public async Task CreateBatchAsync_PersistsToDatabase()
    {
        await ClearDataAsync();
        var service = CreateService();

        var request = new CreateBatchRequest
        {
            MaterialId = Guid.NewGuid(),
            InitialWeightGrams = 1000m,
            Location = "Cabinet A"
        };

        await service.CreateBatchAsync(request, CancellationToken.None);

        var count = await _context.InventoryBatches.CountAsync();
        Assert.Equal(1, count);
    }

    /// <summary>
    /// Verifies that CreateInventoryItemAsync records a physical stock item with scan-ready label data.
    /// </summary>
    [Fact]
    public async Task CreateInventoryItemAsync_WithBlockMetadata_GeneratesTrackingAndQrPayload()
    {
        await ClearDataAsync();
        var service = CreateService();
        var materialId = Guid.NewGuid();

        var result = await service.CreateInventoryItemAsync(new CreateInventoryItemRequest
        {
            MaterialId = materialId,
            InitialQuantity = 1m,
            QuantityUnit = "pcs",
            FormFactor = "Block",
            Location = "Rack A3",
            LengthMm = 100m,
            WidthMm = 100m,
            HeightMm = 50m,
            MaterialGrade = "Delrin POM",
            Color = "Black",
            ReceivedBy = "employee-1"
        }, CancellationToken.None);

        Assert.StartsWith("INV-", result.TrackingCode);
        Assert.Contains(result.TrackingCode, result.QrPayload, StringComparison.Ordinal);
        Assert.Equal(materialId, result.MaterialId);
        Assert.Equal("Block", result.FormFactor);
        Assert.Equal("pcs", result.QuantityUnit);
        Assert.Equal(1m, result.RemainingQuantity);
        Assert.Equal(100m, result.LengthMm);
        Assert.Equal(100m, result.WidthMm);
        Assert.Equal(50m, result.HeightMm);
        Assert.Equal("Delrin POM", result.MaterialGrade);

        var stored = await _context.InventoryBatches.SingleAsync();
        Assert.Equal(result.TrackingCode, stored.TrackingCode);
        Assert.Equal(result.QrPayload, stored.QrPayload);
    }

    /// <summary>
    /// Verifies that ConsumeInventoryItemAsync deducts from one exact stock item and records a job audit event.
    /// </summary>
    [Fact]
    public async Task ConsumeInventoryItemAsync_WithJobContext_DeductsStockAndCreatesAuditEvent()
    {
        await ClearDataAsync();
        var service = CreateService();
        var jobId = Guid.NewGuid();
        var materialId = Guid.NewGuid();

        var created = await service.CreateInventoryItemAsync(new CreateInventoryItemRequest
        {
            MaterialId = materialId,
            InitialQuantity = 1000m,
            QuantityUnit = "g",
            FormFactor = "Spool",
            Location = "Printer rack",
            LowStockThresholdQuantity = 100m,
            ReceivedBy = "employee-1"
        }, CancellationToken.None);

        var result = await service.ConsumeInventoryItemAsync(
            created.TrackingCode,
            new ConsumeInventoryItemRequest
            {
                JobId = jobId,
                OperatorId = "operator-1",
                QuantityConsumed = 250m,
                Notes = "Assigned to production job"
            },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(750m, result.RemainingQuantity);
        Assert.Equal(750m, result.RemainingWeightGrams);

        var auditEvent = await _context.InventoryConsumptionEvents.SingleAsync();
        Assert.Equal(created.Id, auditEvent.InventoryBatchId);
        Assert.Equal(jobId, auditEvent.JobId);
        Assert.Equal("operator-1", auditEvent.OperatorId);
        Assert.Equal(250m, auditEvent.QuantityConsumed);
        Assert.Equal(750m, auditEvent.RemainingQuantityAfter);
    }

    /// <summary>
    /// Verifies that GetStatusAsync ignores invalid status filter.
    /// </summary>
    [Fact]
    public async Task GetStatusAsync_WithInvalidStatusFilter_ReturnsAll()
    {
        await ClearDataAsync();
        var materialId = Guid.NewGuid();
        
        _context.InventoryBatches.AddRange(
            new InventoryBatch { Id = Guid.NewGuid(), MaterialId = materialId, InitialWeightGrams = 1000m, RemainingWeightGrams = 800m, Status = BatchStatus.Active, Location = "A", LowStockThresholdGrams = 100m, ReceivedAt = DateTime.UtcNow },
            new InventoryBatch { Id = Guid.NewGuid(), MaterialId = materialId, InitialWeightGrams = 500m, RemainingWeightGrams = 0m, Status = BatchStatus.Depleted, Location = "B", LowStockThresholdGrams = 50m, ReceivedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        var service = CreateService();

        var result = await service.GetStatusAsync(null, "InvalidStatus", CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(800m, result.First().TotalRemainingGrams);
    }

    /// <summary>
    /// Verifies that GetStatusAsync handles empty status string.
    /// </summary>
    [Fact]
    public async Task GetStatusAsync_WithEmptyStatus_ReturnsAll()
    {
        await ClearDataAsync();
        var materialId = Guid.NewGuid();
        
        _context.InventoryBatches.Add(new InventoryBatch { Id = Guid.NewGuid(), MaterialId = materialId, InitialWeightGrams = 1000m, RemainingWeightGrams = 800m, Status = BatchStatus.Active, Location = "A", LowStockThresholdGrams = 100m, ReceivedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        var service = CreateService();

        var result = await service.GetStatusAsync(null, "", CancellationToken.None);

        Assert.Single(result);
    }
}
