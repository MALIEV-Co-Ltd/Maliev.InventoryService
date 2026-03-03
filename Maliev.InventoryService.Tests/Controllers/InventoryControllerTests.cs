using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Maliev.InventoryService.Domain.Clients;
using Maliev.InventoryService.Domain.Models;
using Maliev.InventoryService.Api.Controllers;
using Maliev.InventoryService.Api.DTOs;
using Maliev.InventoryService.Infrastructure.Persistence;
using Maliev.InventoryService.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Maliev.InventoryService.Tests.Controllers;

/// <summary>
/// Tests for the InventoryController.
/// </summary>
public class InventoryControllerTests : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private InventoryDbContext _context = null!;
    private readonly Mock<IMaterialServiceClient> _materialClientMock;
    private readonly Mock<ILogger<InventoryController>> _loggerMock;

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryControllerTests"/> class.
    /// </summary>
    public InventoryControllerTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _materialClientMock = new Mock<IMaterialServiceClient>();
        _loggerMock = new Mock<ILogger<InventoryController>>();
    }

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;
        _context = new InventoryDbContext(options);
        await _context.Database.EnsureCreatedAsync();
        _context.InventoryBatches.RemoveRange(_context.InventoryBatches);
        await _context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    /// <summary>
    /// Verifies that a batch is correctly created when a valid material ID is provided.
    /// </summary>
    [Fact]
    public async Task CreateBatch_WithValidMaterial_Returns201Created()
    {
        // Arrange
        var materialId = Guid.NewGuid();
        _materialClientMock
            .Setup(c => c.GetMaterialAsync(materialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MaterialDto { Id = materialId, Name = "Test Material", Density = 1.2m });

        var controller = new InventoryController(_context, _materialClientMock.Object, _loggerMock.Object);
        var request = new CreateBatchRequest
        {
            MaterialId = materialId,
            InitialWeightGrams = 1000m,
            Location = "Cabinet A",
            LowStockThresholdGrams = 100m
        };

        // Act
        var result = await controller.CreateBatch(request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<CreateBatchResponse>(createdResult.Value);
        Assert.Equal(materialId, response.MaterialId);
        Assert.Equal(1000m, response.InitialWeightGrams);
        Assert.Equal(1000m, response.RemainingWeightGrams);
        Assert.Equal("Active", response.Status);
        Assert.Equal("Cabinet A", response.Location);
        Assert.Equal(100m, response.LowStockThresholdGrams);
    }

    /// <summary>
    /// Verifies that a 404 response is returned when an invalid material ID is provided.
    /// </summary>
    [Fact]
    public async Task CreateBatch_WithInvalidMaterial_Returns404NotFound()
    {
        // Arrange
        var materialId = Guid.NewGuid();
        _materialClientMock
            .Setup(c => c.GetMaterialAsync(materialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MaterialDto?)null);

        var controller = new InventoryController(_context, _materialClientMock.Object, _loggerMock.Object);
        var request = new CreateBatchRequest
        {
            MaterialId = materialId,
            InitialWeightGrams = 1000m,
            Location = "Cabinet A"
        };

        // Act
        var result = await controller.CreateBatch(request, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }

    /// <summary>
    /// Verifies that the low stock threshold defaults to 100g if not specified.
    /// </summary>
    [Fact]
    public async Task CreateBatch_WithoutThreshold_DefaultsTo100g()
    {
        // Arrange
        var materialId = Guid.NewGuid();
        _materialClientMock
            .Setup(c => c.GetMaterialAsync(materialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MaterialDto { Id = materialId, Name = "Test Material", Density = 1.2m });

        var controller = new InventoryController(_context, _materialClientMock.Object, _loggerMock.Object);
        var request = new CreateBatchRequest
        {
            MaterialId = materialId,
            InitialWeightGrams = 1000m,
            Location = "Cabinet A",
            LowStockThresholdGrams = null // Not specified
        };

        // Act
        var result = await controller.CreateBatch(request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<CreateBatchResponse>(createdResult.Value);
        Assert.Equal(100m, response.LowStockThresholdGrams); // Default value
    }

    /// <summary>
    /// Verifies that GetStatus returns all active batches when no filters are applied.
    /// </summary>
    [Fact]
    public async Task GetStatus_NoFilters_ReturnsAllActiveBatches()
    {
        // Arrange
        var materialId1 = Guid.NewGuid();
        var materialId2 = Guid.NewGuid();
        
        var batch1 = new InventoryBatch
        {
            Id = Guid.NewGuid(),
            MaterialId = materialId1,
            InitialWeightGrams = 1000m,
            RemainingWeightGrams = 800m,
            Status = BatchStatus.Active,
            Location = "Cabinet A",
            LowStockThresholdGrams = 100m,
            ReceivedAt = DateTime.UtcNow.AddDays(-1)
        };
        var batch2 = new InventoryBatch
        {
            Id = Guid.NewGuid(),
            MaterialId = materialId1,
            InitialWeightGrams = 500m,
            RemainingWeightGrams = 500m,
            Status = BatchStatus.Active,
            Location = "Cabinet B",
            LowStockThresholdGrams = 50m,
            ReceivedAt = DateTime.UtcNow
        };
        var batch3 = new InventoryBatch
        {
            Id = Guid.NewGuid(),
            MaterialId = materialId2,
            InitialWeightGrams = 1000m,
            RemainingWeightGrams = 1000m,
            Status = BatchStatus.Active,
            Location = "Cabinet C",
            LowStockThresholdGrams = 100m,
            ReceivedAt = DateTime.UtcNow
        };
        
        _context.InventoryBatches.AddRange(batch1, batch2, batch3);
        await _context.SaveChangesAsync();

        var controller = new InventoryController(_context, _materialClientMock.Object, _loggerMock.Object);

        // Act
        var result = await controller.GetStatus(null, null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var summaries = Assert.IsAssignableFrom<IEnumerable<MaterialStatusSummary>>(okResult.Value);
        var summaryList = summaries.ToList();
        
        Assert.Equal(2, summaryList.Count);
        
        var material1Summary = summaryList.First(s => s.MaterialId == materialId1);
        Assert.Equal(2, material1Summary.ActiveBatches);
        Assert.Equal(1300m, material1Summary.TotalRemainingGrams); // 800 + 500
        Assert.Equal(500m, material1Summary.LowestBatchGrams);
        
        var material2Summary = summaryList.First(s => s.MaterialId == materialId2);
        Assert.Equal(1, material2Summary.ActiveBatches);
        Assert.Equal(1000m, material2Summary.TotalRemainingGrams);
    }

    /// <summary>
    /// Verifies that GetStatus filters by material ID correctly.
    /// </summary>
    [Fact]
    public async Task GetStatus_WithMaterialIdFilter_ReturnsOnlyFilteredMaterial()
    {
        // Arrange
        var materialId1 = Guid.NewGuid();
        var materialId2 = Guid.NewGuid();
        
        var batch1 = new InventoryBatch
        {
            Id = Guid.NewGuid(),
            MaterialId = materialId1,
            InitialWeightGrams = 1000m,
            RemainingWeightGrams = 800m,
            Status = BatchStatus.Active,
            Location = "Cabinet A",
            LowStockThresholdGrams = 100m,
            ReceivedAt = DateTime.UtcNow
        };
        var batch2 = new InventoryBatch
        {
            Id = Guid.NewGuid(),
            MaterialId = materialId2,
            InitialWeightGrams = 500m,
            RemainingWeightGrams = 500m,
            Status = BatchStatus.Active,
            Location = "Cabinet B",
            LowStockThresholdGrams = 50m,
            ReceivedAt = DateTime.UtcNow
        };
        
        _context.InventoryBatches.AddRange(batch1, batch2);
        await _context.SaveChangesAsync();

        var controller = new InventoryController(_context, _materialClientMock.Object, _loggerMock.Object);

        // Act
        var result = await controller.GetStatus(materialId1, null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var summaries = Assert.IsAssignableFrom<IEnumerable<MaterialStatusSummary>>(okResult.Value);
        var summaryList = summaries.ToList();
        
        Assert.Single(summaryList);
        Assert.Equal(materialId1, summaryList[0].MaterialId);
    }

    /// <summary>
    /// Verifies that GetStatus filters by status correctly.
    /// </summary>
    [Fact]
    public async Task GetStatus_WithStatusFilter_ReturnsOnlyMatchingStatus()
    {
        // Arrange
        var materialId = Guid.NewGuid();
        
        var activeBatch = new InventoryBatch
        {
            Id = Guid.NewGuid(),
            MaterialId = materialId,
            InitialWeightGrams = 1000m,
            RemainingWeightGrams = 800m,
            Status = BatchStatus.Active,
            Location = "Cabinet A",
            LowStockThresholdGrams = 100m,
            ReceivedAt = DateTime.UtcNow.AddDays(-1)
        };
        var depletedBatch = new InventoryBatch
        {
            Id = Guid.NewGuid(),
            MaterialId = materialId,
            InitialWeightGrams = 500m,
            RemainingWeightGrams = 0m,
            Status = BatchStatus.Depleted,
            Location = "Cabinet B",
            LowStockThresholdGrams = 50m,
            ReceivedAt = DateTime.UtcNow
        };
        
        _context.InventoryBatches.AddRange(activeBatch, depletedBatch);
        await _context.SaveChangesAsync();

        var controller = new InventoryController(_context, _materialClientMock.Object, _loggerMock.Object);

        // Act
        var result = await controller.GetStatus(null, "Active", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var summaries = Assert.IsAssignableFrom<IEnumerable<MaterialStatusSummary>>(okResult.Value);
        var summaryList = summaries.ToList();
        
        Assert.Single(summaryList);
        Assert.Equal(1, summaryList[0].ActiveBatches);
        Assert.Equal(800m, summaryList[0].TotalRemainingGrams);
    }

    /// <summary>
    /// Verifies that GetStatus returns empty list when no batches exist.
    /// </summary>
    [Fact]
    public async Task GetStatus_NoBatches_ReturnsEmptyList()
    {
        // Arrange
        var controller = new InventoryController(_context, _materialClientMock.Object, _loggerMock.Object);

        // Act
        var result = await controller.GetStatus(null, null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var summaries = Assert.IsAssignableFrom<IEnumerable<MaterialStatusSummary>>(okResult.Value);
        Assert.Empty(summaries);
    }

    /// <summary>
    /// Verifies that GetStatus detects low stock correctly.
    /// </summary>
    [Fact]
    public async Task GetStatus_WithLowStockBatch_DetectsLowStockAlert()
    {
        // Arrange
        var materialId = Guid.NewGuid();
        
        var normalBatch = new InventoryBatch
        {
            Id = Guid.NewGuid(),
            MaterialId = materialId,
            InitialWeightGrams = 1000m,
            RemainingWeightGrams = 500m,
            Status = BatchStatus.Active,
            Location = "Cabinet A",
            LowStockThresholdGrams = 100m,
            ReceivedAt = DateTime.UtcNow
        };
        var lowStockBatch = new InventoryBatch
        {
            Id = Guid.NewGuid(),
            MaterialId = materialId,
            InitialWeightGrams = 200m,
            RemainingWeightGrams = 50m, // Below 100g threshold
            Status = BatchStatus.Active,
            Location = "Cabinet B",
            LowStockThresholdGrams = 100m,
            ReceivedAt = DateTime.UtcNow
        };
        
        _context.InventoryBatches.AddRange(normalBatch, lowStockBatch);
        await _context.SaveChangesAsync();

        var controller = new InventoryController(_context, _materialClientMock.Object, _loggerMock.Object);

        // Act
        var result = await controller.GetStatus(null, null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var summaries = Assert.IsAssignableFrom<IEnumerable<MaterialStatusSummary>>(okResult.Value);
        var summaryList = summaries.ToList();
        
        Assert.Single(summaryList);
        Assert.True(summaryList[0].HasLowStockAlert);
    }
}
