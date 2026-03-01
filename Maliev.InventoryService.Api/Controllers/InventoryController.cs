using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Maliev.InventoryService.Api.Authorization;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.InventoryService.Infrastructure.Persistence;
using Maliev.InventoryService.Domain.Entities;
using Maliev.InventoryService.Domain.Clients;
using Maliev.InventoryService.Api.DTOs;

namespace Maliev.InventoryService.Api.Controllers;

/// <summary>
/// Controller for managing material inventory.
/// </summary>
[ApiController]
[Route("inventory/v1/stock")]
[RequirePermission(InventoryPermissions.StockRead)]
public class InventoryController : ControllerBase
{
    private readonly InventoryDbContext _context;
    private readonly IMaterialServiceClient _materialClient;
    private readonly ILogger<InventoryController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryController"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="materialClient">The material service client.</param>
    /// <param name="logger">The logger.</param>
    public InventoryController(
        InventoryDbContext context,
        IMaterialServiceClient materialClient,
        ILogger<InventoryController> logger)
    {
        _context = context;
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
        // Validate material exists in Material Service
        var material = await _materialClient.GetMaterialAsync(request.MaterialId, cancellationToken);
        if (material == null)
        {
            _logger.LogWarning("Material {MaterialId} not found", request.MaterialId);
            return NotFound(new { error = $"Material {request.MaterialId} not found." });
        }

        // Create batch with defaults
        var batch = new InventoryBatch
        {
            Id = Guid.NewGuid(),
            MaterialId = request.MaterialId,
            InitialWeightGrams = request.InitialWeightGrams,
            RemainingWeightGrams = request.InitialWeightGrams,
            Status = BatchStatus.Active,
            Location = request.Location,
            LowStockThresholdGrams = request.LowStockThresholdGrams ?? 100m,
            HasAlerted = false,
            ReceivedAt = DateTime.UtcNow
        };

        _context.InventoryBatches.Add(batch);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created batch {BatchId} for material {MaterialId}", batch.Id, batch.MaterialId);

        var response = new CreateBatchResponse
        {
            Id = batch.Id,
            MaterialId = batch.MaterialId,
            InitialWeightGrams = batch.InitialWeightGrams,
            RemainingWeightGrams = batch.RemainingWeightGrams,
            Status = batch.Status.ToString(),
            Location = batch.Location,
            LowStockThresholdGrams = batch.LowStockThresholdGrams,
            ReceivedAt = batch.ReceivedAt
        };

        return CreatedAtAction(nameof(GetStatus), new { materialId = batch.MaterialId }, response);
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
        var query = _context.InventoryBatches.AsQueryable();

        // Apply filters
        if (materialId.HasValue)
        {
            query = query.Where(b => b.MaterialId == materialId.Value);
        }

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<BatchStatus>(status, out var statusFilter))
        {
            query = query.Where(b => b.Status == statusFilter);
        }

        // Group by material and aggregate
        var summaries = await query
            .GroupBy(b => b.MaterialId)
            .Select(g => new MaterialStatusSummary
            {
                MaterialId = g.Key,
                ActiveBatches = g.Count(b => b.Status == BatchStatus.Active),
                TotalRemainingGrams = g.Sum(b => b.RemainingWeightGrams),
                LowestBatchGrams = g.Min(b => b.RemainingWeightGrams),
                HasLowStockAlert = g.Any(b => 
                    b.Status == BatchStatus.Active && 
                    b.RemainingWeightGrams < b.LowStockThresholdGrams)
            })
            .ToListAsync(cancellationToken);

        return Ok(summaries);
    }
}
