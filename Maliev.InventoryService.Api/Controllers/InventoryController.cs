using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Maliev.InventoryService.Api.Authorization;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.InventoryService.Application.Abstractions;
using Maliev.InventoryService.Domain.Clients;
using Maliev.InventoryService.Api.DTOs;
using CreateBatchRequestModel = Maliev.InventoryService.Application.Models.CreateBatchRequest;
using ConsumeInventoryItemRequestModel = Maliev.InventoryService.Application.Models.ConsumeInventoryItemRequest;
using CreateInventoryItemRequestModel = Maliev.InventoryService.Application.Models.CreateInventoryItemRequest;
using InventoryItemResultModel = Maliev.InventoryService.Application.Models.InventoryItemResult;

namespace Maliev.InventoryService.Api.Controllers;

/// <summary>
/// Controller for managing material inventory.
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("inventory/v{version:apiVersion}/stock")]
[RequirePermission(InventoryPermissions.StockRead)]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly IMaterialServiceClient _materialClient;
    private readonly ILogger<InventoryController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryController"/> class.
    /// </summary>
    /// <param name="inventoryService">The inventory service.</param>
    /// <param name="materialClient">The material service client.</param>
    /// <param name="logger">The logger.</param>
    public InventoryController(
        IInventoryService inventoryService,
        IMaterialServiceClient materialClient,
        ILogger<InventoryController> logger)
    {
        _inventoryService = inventoryService;
        _materialClient = materialClient;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new material batch into the inventory.
    /// </summary>
    [HttpPost("batches")]
    [RequirePermission(InventoryPermissions.StockWrite)]
    [ProducesResponseType(typeof(CreateBatchResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateBatch([FromBody] CreateBatchRequest request, CancellationToken cancellationToken)
    {
        var material = await _materialClient.GetMaterialAsync(request.MaterialId, cancellationToken);
        if (material == null)
        {
            _logger.LogWarning("Material {MaterialId} not found", request.MaterialId);
            return NotFound(new { error = $"Material {request.MaterialId} not found." });
        }

        var appRequest = new CreateBatchRequestModel
        {
            MaterialId = request.MaterialId,
            InitialWeightGrams = request.InitialWeightGrams,
            Location = request.Location,
            LowStockThresholdGrams = request.LowStockThresholdGrams
        };

        var result = await _inventoryService.CreateBatchAsync(appRequest, cancellationToken);

        var response = new CreateBatchResponse
        {
            Id = result.Id,
            MaterialId = result.MaterialId,
            InitialWeightGrams = result.InitialWeightGrams,
            RemainingWeightGrams = result.RemainingWeightGrams,
            Status = result.Status,
            Location = result.Location,
            LowStockThresholdGrams = result.LowStockThresholdGrams,
            ReceivedAt = result.ReceivedAt
        };

        return CreatedAtAction(nameof(GetStatus), new { materialId = result.MaterialId }, response);
    }

    /// <summary>
    /// Registers one physical QR-tracked inventory item.
    /// </summary>
    [HttpPost("items")]
    [RequirePermission(InventoryPermissions.StockWrite)]
    [ProducesResponseType(typeof(InventoryItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateItem([FromBody] CreateInventoryItemRequest request, CancellationToken cancellationToken)
    {
        var material = await _materialClient.GetMaterialAsync(request.MaterialId, cancellationToken);
        if (material == null)
        {
            _logger.LogWarning("Material {MaterialId} not found", request.MaterialId);
            return NotFound(new { error = $"Material {request.MaterialId} not found." });
        }

        try
        {
            var result = await _inventoryService.CreateInventoryItemAsync(new CreateInventoryItemRequestModel
            {
                MaterialId = request.MaterialId,
                InitialQuantity = request.InitialQuantity,
                QuantityUnit = request.QuantityUnit,
                Location = request.Location,
                FormFactor = request.FormFactor,
                LowStockThresholdQuantity = request.LowStockThresholdQuantity,
                SupplierId = request.SupplierId,
                PurchaseOrderId = request.PurchaseOrderId,
                LotNumber = request.LotNumber,
                ManufacturerSku = request.ManufacturerSku,
                Color = request.Color,
                MaterialGrade = request.MaterialGrade,
                LengthMm = request.LengthMm,
                WidthMm = request.WidthMm,
                HeightMm = request.HeightMm,
                DiameterMm = request.DiameterMm,
                ThicknessMm = request.ThicknessMm,
                ReceivedBy = request.ReceivedBy
            }, cancellationToken);

            return CreatedAtAction(nameof(GetItem), new { trackingCode = result.TrackingCode }, ToItemResponse(result));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Lists physical QR-tracked inventory items.
    /// </summary>
    [HttpGet("items")]
    [ProducesResponseType(typeof(IEnumerable<InventoryItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListItems(
        [FromQuery] Guid? materialId = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var results = await _inventoryService.ListInventoryItemsAsync(materialId, status, cancellationToken);
        return Ok(results.Select(ToItemResponse));
    }

    /// <summary>
    /// Gets one physical QR-tracked inventory item by tracking code or QR payload.
    /// </summary>
    [HttpGet("items/{trackingCode}")]
    [ProducesResponseType(typeof(InventoryItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetItem(string trackingCode, CancellationToken cancellationToken)
    {
        var result = await _inventoryService.GetInventoryItemByTrackingCodeAsync(trackingCode, cancellationToken);
        return result is null ? NotFound() : Ok(ToItemResponse(result));
    }

    /// <summary>
    /// Consumes material from one exact physical inventory item.
    /// </summary>
    [HttpPost("items/{trackingCode}/consume")]
    [RequirePermission(InventoryPermissions.StockWrite)]
    [ProducesResponseType(typeof(InventoryItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConsumeItem(
        string trackingCode,
        [FromBody] ConsumeInventoryItemRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _inventoryService.ConsumeInventoryItemAsync(
                trackingCode,
                new ConsumeInventoryItemRequestModel
                {
                    JobId = request.JobId,
                    OrderItemId = request.OrderItemId,
                    OperatorId = request.OperatorId,
                    MachineId = request.MachineId,
                    QuantityConsumed = request.QuantityConsumed,
                    Notes = request.Notes
                },
                cancellationToken);

            return result is null ? NotFound() : Ok(ToItemResponse(result));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets the current status of material batches.
    /// </summary>
    [HttpGet("batches/status")]
    [ProducesResponseType(typeof(IEnumerable<MaterialStatusSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetStatus(
        [FromQuery] Guid? materialId = null,
        [FromQuery] string? status = "Active",
        CancellationToken cancellationToken = default)
    {
        var results = await _inventoryService.GetStatusAsync(materialId, status, cancellationToken);

        var summaries = results.Select(r => new MaterialStatusSummary
        {
            MaterialId = r.MaterialId,
            ActiveBatches = r.ActiveBatches,
            TotalRemainingGrams = r.TotalRemainingGrams,
            LowestBatchGrams = r.LowestBatchGrams,
            HasLowStockAlert = r.HasLowStockAlert
        });

        return Ok(summaries);
    }

    private static InventoryItemResponse ToItemResponse(InventoryItemResultModel result)
    {
        return new InventoryItemResponse
        {
            Id = result.Id,
            MaterialId = result.MaterialId,
            TrackingCode = result.TrackingCode,
            QrPayload = result.QrPayload,
            InitialQuantity = result.InitialQuantity,
            RemainingQuantity = result.RemainingQuantity,
            QuantityUnit = result.QuantityUnit,
            InitialWeightGrams = result.InitialWeightGrams,
            RemainingWeightGrams = result.RemainingWeightGrams,
            Status = result.Status,
            Location = result.Location,
            FormFactor = result.FormFactor,
            LowStockThresholdQuantity = result.LowStockThresholdQuantity,
            SupplierId = result.SupplierId,
            PurchaseOrderId = result.PurchaseOrderId,
            LotNumber = result.LotNumber,
            ManufacturerSku = result.ManufacturerSku,
            Color = result.Color,
            MaterialGrade = result.MaterialGrade,
            LengthMm = result.LengthMm,
            WidthMm = result.WidthMm,
            HeightMm = result.HeightMm,
            DiameterMm = result.DiameterMm,
            ThicknessMm = result.ThicknessMm,
            ReceivedBy = result.ReceivedBy,
            ReceivedAt = result.ReceivedAt
        };
    }
}
