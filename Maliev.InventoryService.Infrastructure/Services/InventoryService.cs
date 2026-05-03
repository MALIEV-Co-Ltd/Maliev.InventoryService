using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Maliev.InventoryService.Application.Abstractions;
using Maliev.InventoryService.Application.Models;
using Maliev.InventoryService.Domain.Entities;
using Maliev.InventoryService.Infrastructure.Persistence;

namespace Maliev.InventoryService.Infrastructure.Services;

/// <summary>
/// Application service for managing material inventory operations.
/// </summary>
public class InventoryService : IInventoryService
{
    private readonly InventoryDbContext _context;
    private readonly ILogger<InventoryService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger.</param>
    public InventoryService(InventoryDbContext context, ILogger<InventoryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CreateBatchResult> CreateBatchAsync(CreateBatchRequest request, CancellationToken cancellationToken = default)
    {
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
            ReceivedAt = DateTimeOffset.UtcNow
        };

        _context.InventoryBatches.Add(batch);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created batch {BatchId} for material {MaterialId}", batch.Id, batch.MaterialId);

        return new CreateBatchResult
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
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MaterialStatusSummaryResult>> GetStatusAsync(
        Guid? materialId,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var query = _context.InventoryBatches.AsQueryable();

        if (materialId.HasValue)
        {
            query = query.Where(b => b.MaterialId == materialId.Value);
        }

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<BatchStatus>(status, out var statusFilter))
        {
            query = query.Where(b => b.Status == statusFilter);
        }

        var summaries = await query
            .GroupBy(b => b.MaterialId)
            .Select(g => new MaterialStatusSummaryResult
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

        return summaries;
    }
}
