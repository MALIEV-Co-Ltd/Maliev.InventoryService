namespace Maliev.InventoryService.Application.Models;

/// <summary>
/// Summary of inventory status for a specific material.
/// </summary>
public record MaterialStatusSummaryResult
{
    /// <summary>Gets the unique identifier of the material.</summary>
    public required Guid MaterialId { get; init; }
    /// <summary>Gets the number of active (not depleted) batches.</summary>
    public required int ActiveBatches { get; init; }
    /// <summary>Gets the total remaining weight across all active batches in grams.</summary>
    public required decimal TotalRemainingGrams { get; init; }
    /// <summary>Gets the weight of the batch with the least amount remaining.</summary>
    public required decimal LowestBatchGrams { get; init; }
    /// <summary>Gets a value indicating whether any active batch is below its low stock threshold.</summary>
    public required bool HasLowStockAlert { get; init; }
}
