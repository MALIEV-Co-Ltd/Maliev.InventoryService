using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Maliev.InventoryService.Domain.Clients;
using Maliev.InventoryService.Domain.Models;
using Maliev.InventoryService.Api.Controllers;
using Maliev.InventoryService.Api.DTOs;
using Maliev.InventoryService.Application.Abstractions;
using Maliev.InventoryService.Application.Models;
using Microsoft.Extensions.Logging;
using AppCreateBatchRequest = Maliev.InventoryService.Application.Models.CreateBatchRequest;
using ApiCreateBatchRequest = Maliev.InventoryService.Api.DTOs.CreateBatchRequest;
using AppConsumeInventoryItemRequest = Maliev.InventoryService.Application.Models.ConsumeInventoryItemRequest;
using AppCreateInventoryItemRequest = Maliev.InventoryService.Application.Models.CreateInventoryItemRequest;
using ApiConsumeInventoryItemRequest = Maliev.InventoryService.Api.DTOs.ConsumeInventoryItemRequest;
using ApiCreateInventoryItemRequest = Maliev.InventoryService.Api.DTOs.CreateInventoryItemRequest;
using ApiInventoryItemResponse = Maliev.InventoryService.Api.DTOs.InventoryItemResponse;

namespace Maliev.InventoryService.Tests.Controllers;

/// <summary>
/// Tests for the InventoryController.
/// </summary>
public class InventoryControllerTests
{
    private readonly Mock<IInventoryService> _inventoryServiceMock;
    private readonly Mock<IMaterialServiceClient> _materialClientMock;
    private readonly Mock<ILogger<InventoryController>> _loggerMock;
    private readonly InventoryController _controller;

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryControllerTests"/> class.
    /// </summary>
    public InventoryControllerTests()
    {
        _inventoryServiceMock = new Mock<IInventoryService>();
        _materialClientMock = new Mock<IMaterialServiceClient>();
        _loggerMock = new Mock<ILogger<InventoryController>>();
        _controller = new InventoryController(
            _inventoryServiceMock.Object,
            _materialClientMock.Object,
            _loggerMock.Object);
    }

    /// <summary>
    /// Verifies that a batch is correctly created when a valid material ID is provided.
    /// </summary>
    [Fact]
    public async Task CreateBatch_WithValidMaterial_Returns201Created()
    {
        var materialId = Guid.NewGuid();
        var batchId = Guid.NewGuid();

        _materialClientMock
            .Setup(c => c.GetMaterialAsync(materialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MaterialDto { Id = materialId, Name = "Test Material", Density = 1.2m });

        _inventoryServiceMock
            .Setup(s => s.CreateBatchAsync(It.IsAny<AppCreateBatchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateBatchResult
            {
                Id = batchId,
                MaterialId = materialId,
                InitialWeightGrams = 1000m,
                RemainingWeightGrams = 1000m,
                Status = "Active",
                Location = "Cabinet A",
                LowStockThresholdGrams = 100m,
                ReceivedAt = DateTime.UtcNow
            });

        var request = new ApiCreateBatchRequest
        {
            MaterialId = materialId,
            InitialWeightGrams = 1000m,
            Location = "Cabinet A",
            LowStockThresholdGrams = 100m
        };

        var result = await _controller.CreateBatch(request, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<CreateBatchResponse>(createdResult.Value);
        Assert.Equal(materialId, response.MaterialId);
        Assert.Equal(1000m, response.InitialWeightGrams);
        Assert.Equal(1000m, response.RemainingWeightGrams);
        Assert.Equal("Active", response.Status);
    }

    /// <summary>
    /// Verifies that a physical inventory item can be registered for a valid material.
    /// </summary>
    [Fact]
    public async Task CreateItem_WithValidMaterial_Returns201Created()
    {
        var materialId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        _materialClientMock
            .Setup(c => c.GetMaterialAsync(materialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MaterialDto { Id = materialId, Name = "Delrin POM", Density = 1.41m });

        _inventoryServiceMock
            .Setup(s => s.CreateInventoryItemAsync(It.IsAny<AppCreateInventoryItemRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InventoryItemResult
            {
                Id = itemId,
                MaterialId = materialId,
                TrackingCode = "INV-26-000001",
                QrPayload = "/mfg/inventory/items/INV-26-000001",
                InitialQuantity = 1m,
                RemainingQuantity = 1m,
                QuantityUnit = "pcs",
                InitialWeightGrams = 0m,
                RemainingWeightGrams = 0m,
                Status = "Active",
                Location = "Rack A3",
                FormFactor = "Block",
                LengthMm = 100m,
                WidthMm = 100m,
                HeightMm = 50m,
                ReceivedAt = DateTimeOffset.UtcNow
            });

        var request = new ApiCreateInventoryItemRequest
        {
            MaterialId = materialId,
            InitialQuantity = 1m,
            QuantityUnit = "pcs",
            Location = "Rack A3",
            FormFactor = "Block",
            LengthMm = 100m,
            WidthMm = 100m,
            HeightMm = 50m
        };

        var result = await _controller.CreateItem(request, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<ApiInventoryItemResponse>(createdResult.Value);
        Assert.Equal("INV-26-000001", response.TrackingCode);
        Assert.Equal("/mfg/inventory/items/INV-26-000001", response.QrPayload);
        Assert.Equal("Block", response.FormFactor);
    }

    /// <summary>
    /// Verifies that consuming a stock item returns the updated item state.
    /// </summary>
    [Fact]
    public async Task ConsumeItem_WithValidTrackingCode_ReturnsUpdatedItem()
    {
        var itemId = Guid.NewGuid();
        var materialId = Guid.NewGuid();
        var jobId = Guid.NewGuid();

        _inventoryServiceMock
            .Setup(s => s.ConsumeInventoryItemAsync(
                "INV-26-000001",
                It.Is<AppConsumeInventoryItemRequest>(request => request.JobId == jobId && request.QuantityConsumed == 125m),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InventoryItemResult
            {
                Id = itemId,
                MaterialId = materialId,
                TrackingCode = "INV-26-000001",
                QrPayload = "/mfg/inventory/items/INV-26-000001",
                InitialQuantity = 1000m,
                RemainingQuantity = 875m,
                QuantityUnit = "g",
                InitialWeightGrams = 1000m,
                RemainingWeightGrams = 875m,
                Status = "Active",
                Location = "Printer rack",
                FormFactor = "Spool",
                ReceivedAt = DateTimeOffset.UtcNow
            });

        var result = await _controller.ConsumeItem(
            "INV-26-000001",
            new ApiConsumeInventoryItemRequest
            {
                JobId = jobId,
                QuantityConsumed = 125m,
                OperatorId = "operator-1"
            },
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiInventoryItemResponse>(okResult.Value);
        Assert.Equal(875m, response.RemainingQuantity);
        Assert.Equal("INV-26-000001", response.TrackingCode);
    }

    /// <summary>
    /// Verifies that a 404 response is returned when an invalid material ID is provided.
    /// </summary>
    [Fact]
    public async Task CreateBatch_WithInvalidMaterial_Returns404NotFound()
    {
        var materialId = Guid.NewGuid();

        _materialClientMock
            .Setup(c => c.GetMaterialAsync(materialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MaterialDto?)null);

        var request = new ApiCreateBatchRequest
        {
            MaterialId = materialId,
            InitialWeightGrams = 1000m,
            Location = "Cabinet A"
        };

        var result = await _controller.CreateBatch(request, CancellationToken.None);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }

    /// <summary>
    /// Verifies that the low stock threshold defaults to 100g if not specified.
    /// </summary>
    [Fact]
    public async Task CreateBatch_WithoutThreshold_CallsServiceWithDefault()
    {
        var materialId = Guid.NewGuid();
        var batchId = Guid.NewGuid();

        _materialClientMock
            .Setup(c => c.GetMaterialAsync(materialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MaterialDto { Id = materialId, Name = "Test Material", Density = 1.2m });

        _inventoryServiceMock
            .Setup(s => s.CreateBatchAsync(It.IsAny<AppCreateBatchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateBatchResult
            {
                Id = batchId,
                MaterialId = materialId,
                InitialWeightGrams = 1000m,
                RemainingWeightGrams = 1000m,
                Status = "Active",
                Location = "Cabinet A",
                LowStockThresholdGrams = 100m,
                ReceivedAt = DateTime.UtcNow
            });

        var request = new ApiCreateBatchRequest
        {
            MaterialId = materialId,
            InitialWeightGrams = 1000m,
            Location = "Cabinet A",
            LowStockThresholdGrams = null
        };

        var result = await _controller.CreateBatch(request, CancellationToken.None);

        _inventoryServiceMock.Verify(
            s => s.CreateBatchAsync(
                It.Is<AppCreateBatchRequest>(r => r.LowStockThresholdGrams == null),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that GetStatus returns all active batches when no filters are applied.
    /// </summary>
    [Fact]
    public async Task GetStatus_NoFilters_ReturnsAllActiveBatches()
    {
        var materialId1 = Guid.NewGuid();
        var materialId2 = Guid.NewGuid();

        _inventoryServiceMock
            .Setup(s => s.GetStatusAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MaterialStatusSummaryResult>
            {
                new() { MaterialId = materialId1, ActiveBatches = 2, TotalRemainingGrams = 1300m, LowestBatchGrams = 500m, HasLowStockAlert = false },
                new() { MaterialId = materialId2, ActiveBatches = 1, TotalRemainingGrams = 1000m, LowestBatchGrams = 1000m, HasLowStockAlert = false }
            });

        var result = await _controller.GetStatus(null, null, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var summaries = Assert.IsAssignableFrom<IEnumerable<MaterialStatusSummary>>(okResult.Value);
        var summaryList = summaries.ToList();

        Assert.Equal(2, summaryList.Count);
    }

    /// <summary>
    /// Verifies that GetStatus filters by material ID correctly.
    /// </summary>
    [Fact]
    public async Task GetStatus_WithMaterialIdFilter_ReturnsOnlyFilteredMaterial()
    {
        var materialId1 = Guid.NewGuid();

        _inventoryServiceMock
            .Setup(s => s.GetStatusAsync(materialId1, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MaterialStatusSummaryResult>
            {
                new() { MaterialId = materialId1, ActiveBatches = 1, TotalRemainingGrams = 800m, LowestBatchGrams = 800m, HasLowStockAlert = false }
            });

        var result = await _controller.GetStatus(materialId1, null, CancellationToken.None);

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
        _inventoryServiceMock
            .Setup(s => s.GetStatusAsync(null, "Active", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MaterialStatusSummaryResult>
            {
                new() { MaterialId = Guid.NewGuid(), ActiveBatches = 1, TotalRemainingGrams = 800m, LowestBatchGrams = 800m, HasLowStockAlert = false }
            });

        var result = await _controller.GetStatus(null, "Active", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var summaries = Assert.IsAssignableFrom<IEnumerable<MaterialStatusSummary>>(okResult.Value);
        var summaryList = summaries.ToList();

        Assert.Single(summaryList);
    }

    /// <summary>
    /// Verifies that GetStatus returns empty list when no batches exist.
    /// </summary>
    [Fact]
    public async Task GetStatus_NoBatches_ReturnsEmptyList()
    {
        _inventoryServiceMock
            .Setup(s => s.GetStatusAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MaterialStatusSummaryResult>());

        var result = await _controller.GetStatus(null, null, CancellationToken.None);

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
        _inventoryServiceMock
            .Setup(s => s.GetStatusAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MaterialStatusSummaryResult>
            {
                new() { MaterialId = Guid.NewGuid(), ActiveBatches = 2, TotalRemainingGrams = 550m, LowestBatchGrams = 50m, HasLowStockAlert = true }
            });

        var result = await _controller.GetStatus(null, null, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var summaries = Assert.IsAssignableFrom<IEnumerable<MaterialStatusSummary>>(okResult.Value);
        var summaryList = summaries.ToList();

        Assert.Single(summaryList);
        Assert.True(summaryList[0].HasLowStockAlert);
    }
}
