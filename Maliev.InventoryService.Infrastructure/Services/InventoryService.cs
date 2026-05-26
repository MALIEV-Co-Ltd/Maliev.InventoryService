using System.Globalization;

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
        var trackingCode = await GenerateTrackingCodeAsync(cancellationToken);
        var batch = new InventoryBatch
        {
            Id = Guid.NewGuid(),
            MaterialId = request.MaterialId,
            TrackingCode = trackingCode,
            QrPayload = BuildQrPayload(trackingCode),
            InitialWeightGrams = request.InitialWeightGrams,
            RemainingWeightGrams = request.InitialWeightGrams,
            InitialQuantity = request.InitialWeightGrams,
            RemainingQuantity = request.InitialWeightGrams,
            QuantityUnit = "g",
            FormFactor = "Spool",
            Status = BatchStatus.Active,
            Location = request.Location,
            LowStockThresholdGrams = request.LowStockThresholdGrams ?? 100m,
            LowStockThresholdQuantity = request.LowStockThresholdGrams ?? 100m,
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
    public async Task<InventoryItemResult> CreateInventoryItemAsync(
        CreateInventoryItemRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.InitialQuantity <= 0)
        {
            throw new InvalidOperationException("Initial quantity must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(request.Location))
        {
            throw new InvalidOperationException("Location is required.");
        }

        var quantityUnit = NormalizeUnit(request.QuantityUnit);
        var formFactor = NormalizeFormFactor(request.FormFactor);
        var trackingCode = await GenerateTrackingCodeAsync(cancellationToken);
        var initialWeightGrams = ConvertToGrams(request.InitialQuantity, quantityUnit);

        var batch = new InventoryBatch
        {
            Id = Guid.NewGuid(),
            MaterialId = request.MaterialId,
            TrackingCode = trackingCode,
            QrPayload = BuildQrPayload(trackingCode),
            InitialWeightGrams = initialWeightGrams,
            RemainingWeightGrams = initialWeightGrams,
            InitialQuantity = request.InitialQuantity,
            RemainingQuantity = request.InitialQuantity,
            QuantityUnit = quantityUnit,
            FormFactor = formFactor,
            Status = BatchStatus.Active,
            Location = request.Location.Trim(),
            LowStockThresholdGrams = ConvertToGrams(request.LowStockThresholdQuantity ?? 0m, quantityUnit),
            LowStockThresholdQuantity = request.LowStockThresholdQuantity,
            HasAlerted = false,
            SupplierId = request.SupplierId,
            PurchaseOrderId = request.PurchaseOrderId,
            LotNumber = TrimOrNull(request.LotNumber),
            ManufacturerSku = TrimOrNull(request.ManufacturerSku),
            Color = TrimOrNull(request.Color),
            MaterialGrade = TrimOrNull(request.MaterialGrade),
            LengthMm = request.LengthMm,
            WidthMm = request.WidthMm,
            HeightMm = request.HeightMm,
            DiameterMm = request.DiameterMm,
            ThicknessMm = request.ThicknessMm,
            ReceivedBy = TrimOrNull(request.ReceivedBy),
            ReceivedAt = DateTimeOffset.UtcNow
        };

        _context.InventoryBatches.Add(batch);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created physical inventory item {TrackingCode} for material {MaterialId}",
            batch.TrackingCode,
            batch.MaterialId);

        return ToInventoryItemResult(batch);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<InventoryItemResult>> ListInventoryItemsAsync(
        Guid? materialId,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var query = _context.InventoryBatches.AsNoTracking().AsQueryable();

        if (materialId.HasValue)
        {
            query = query.Where(batch => batch.MaterialId == materialId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<BatchStatus>(status, ignoreCase: true, out var statusFilter))
        {
            query = query.Where(batch => batch.Status == statusFilter);
        }

        var items = await query
            .OrderByDescending(batch => batch.ReceivedAt)
            .ThenBy(batch => batch.TrackingCode)
            .ToListAsync(cancellationToken);

        return items.Select(ToInventoryItemResult).ToList();
    }

    /// <inheritdoc />
    public async Task<InventoryItemResult?> GetInventoryItemByTrackingCodeAsync(
        string trackingCode,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeTrackingCode(trackingCode);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        var item = await _context.InventoryBatches
            .AsNoTracking()
            .FirstOrDefaultAsync(batch => batch.TrackingCode == normalized, cancellationToken);

        return item is null ? null : ToInventoryItemResult(item);
    }

    /// <inheritdoc />
    public async Task<InventoryItemResult?> ConsumeInventoryItemAsync(
        string trackingCode,
        ConsumeInventoryItemRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.QuantityConsumed <= 0)
        {
            throw new InvalidOperationException("Quantity consumed must be greater than zero.");
        }

        var normalized = NormalizeTrackingCode(trackingCode);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        var item = await _context.InventoryBatches
            .FirstOrDefaultAsync(batch => batch.TrackingCode == normalized, cancellationToken);

        if (item is null)
        {
            return null;
        }

        if (item.RemainingQuantity < request.QuantityConsumed)
        {
            throw new InvalidOperationException("Quantity consumed exceeds remaining item quantity.");
        }

        item.RemainingQuantity -= request.QuantityConsumed;
        item.RemainingWeightGrams = Math.Max(
            0m,
            item.RemainingWeightGrams - ConvertToGrams(request.QuantityConsumed, item.QuantityUnit));

        if (item.RemainingQuantity <= 0)
        {
            item.Status = BatchStatus.Depleted;
            item.RemainingQuantity = 0m;
            item.RemainingWeightGrams = 0m;
        }

        _context.InventoryConsumptionEvents.Add(new InventoryConsumptionEvent
        {
            Id = Guid.NewGuid(),
            InventoryBatchId = item.Id,
            JobId = request.JobId,
            OrderItemId = request.OrderItemId,
            OperatorId = TrimOrNull(request.OperatorId),
            MachineId = TrimOrNull(request.MachineId),
            QuantityConsumed = request.QuantityConsumed,
            RemainingQuantityAfter = item.RemainingQuantity,
            Notes = TrimOrNull(request.Notes),
            ConsumedAt = DateTimeOffset.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Consumed {Quantity} {Unit} from inventory item {TrackingCode}",
            request.QuantityConsumed,
            item.QuantityUnit,
            item.TrackingCode);

        return ToInventoryItemResult(item);
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

    private async Task<string> GenerateTrackingCodeAsync(CancellationToken cancellationToken)
    {
        var year = DateTimeOffset.UtcNow.ToString("yy", CultureInfo.InvariantCulture);
        var prefix = $"INV-{year}-";
        var next = await _context.InventoryBatches
            .CountAsync(batch => batch.TrackingCode.StartsWith(prefix), cancellationToken) + 1;

        for (var attempt = 0; attempt < 100; attempt++)
        {
            var candidate = string.Create(
                CultureInfo.InvariantCulture,
                $"{prefix}{next + attempt:000000}");

            var exists = await _context.InventoryBatches
                .AnyAsync(batch => batch.TrackingCode == candidate, cancellationToken);

            if (!exists)
            {
                return candidate;
            }
        }

        return $"{prefix}{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
    }

    private static InventoryItemResult ToInventoryItemResult(InventoryBatch batch)
    {
        return new InventoryItemResult
        {
            Id = batch.Id,
            MaterialId = batch.MaterialId,
            TrackingCode = batch.TrackingCode,
            QrPayload = batch.QrPayload,
            InitialQuantity = batch.InitialQuantity,
            RemainingQuantity = batch.RemainingQuantity,
            QuantityUnit = batch.QuantityUnit,
            InitialWeightGrams = batch.InitialWeightGrams,
            RemainingWeightGrams = batch.RemainingWeightGrams,
            Status = batch.Status.ToString(),
            Location = batch.Location,
            FormFactor = batch.FormFactor,
            LowStockThresholdQuantity = batch.LowStockThresholdQuantity,
            SupplierId = batch.SupplierId,
            PurchaseOrderId = batch.PurchaseOrderId,
            LotNumber = batch.LotNumber,
            ManufacturerSku = batch.ManufacturerSku,
            Color = batch.Color,
            MaterialGrade = batch.MaterialGrade,
            LengthMm = batch.LengthMm,
            WidthMm = batch.WidthMm,
            HeightMm = batch.HeightMm,
            DiameterMm = batch.DiameterMm,
            ThicknessMm = batch.ThicknessMm,
            ReceivedBy = batch.ReceivedBy,
            ReceivedAt = batch.ReceivedAt
        };
    }

    private static string NormalizeUnit(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? "pcs" : normalized;
    }

    private static string NormalizeFormFactor(string value)
    {
        var normalized = value.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? "Other" : normalized;
    }

    private static string NormalizeTrackingCode(string value)
    {
        var trimmed = value.Trim();
        var slashIndex = trimmed.LastIndexOf("/", StringComparison.Ordinal);
        if (slashIndex >= 0 && slashIndex < trimmed.Length - 1)
        {
            trimmed = trimmed[(slashIndex + 1)..];
        }

        return trimmed.ToUpperInvariant();
    }

    private static decimal ConvertToGrams(decimal quantity, string quantityUnit)
    {
        return quantityUnit.Trim().ToLowerInvariant() switch
        {
            "g" or "gram" or "grams" => quantity,
            "kg" or "kilogram" or "kilograms" => quantity * 1000m,
            _ => 0m
        };
    }

    private static string BuildQrPayload(string trackingCode)
    {
        return $"/mfg/inventory/items/{trackingCode}";
    }

    private static string? TrimOrNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
