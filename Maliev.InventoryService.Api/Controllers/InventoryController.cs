using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Maliev.InventoryService.Api.Authorization;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.InventoryService.Application.Abstractions;
using Maliev.InventoryService.Domain.Clients;
using Maliev.InventoryService.Api.DTOs;
using CreateBatchRequestModel = Maliev.InventoryService.Application.Models.CreateBatchRequest;

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
}
