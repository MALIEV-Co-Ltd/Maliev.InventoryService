namespace Maliev.InventoryService.Api.DTOs;

/// <summary>
/// Summary of inventory status for a specific material.
/// </summary>
public record MaterialStatusSummary
{
    /// <summary>Gets the unique identifier of the material.</summary>
    public Guid MaterialId { get; init; }
    /// <summary>Gets the number of active (not depleted) batches.</summary>
    public int ActiveBatches { get; init; }
    /// <summary>Gets the total remaining weight across all active batches in grams.</summary>
    public decimal TotalRemainingGrams { get; init; }
    /// <summary>Gets the weight of the batch with the least amount remaining.</summary>
    public decimal LowestBatchGrams { get; init; }
    /// <summary>Gets a value indicating whether any active batch is below its low stock threshold.</summary>
    public bool HasLowStockAlert { get; init; }
}
