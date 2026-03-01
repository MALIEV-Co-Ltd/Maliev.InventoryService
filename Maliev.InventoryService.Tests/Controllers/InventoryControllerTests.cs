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
}
