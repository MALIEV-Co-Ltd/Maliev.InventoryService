namespace Maliev.InventoryService.Application.Models;

/// <summary>
/// Response containing the details of a newly created material batch.
/// </summary>
public record CreateBatchResult
{
    /// <summary>Gets the unique identifier of the batch.</summary>
    public required Guid Id { get; init; }
    /// <summary>Gets the unique identifier of the material.</summary>
    public required Guid MaterialId { get; init; }
    /// <summary>Gets the initial weight in grams.</summary>
    public required decimal InitialWeightGrams { get; init; }
    /// <summary>Gets the current remaining weight in grams.</summary>
    public required decimal RemainingWeightGrams { get; init; }
    /// <summary>Gets the current status of the batch (e.g., Active, Depleted).</summary>
    public required string Status { get; init; }
    /// <summary>Gets the storage location.</summary>
    public required string Location { get; init; }
    /// <summary>Gets the low stock alert threshold.</summary>
    public required decimal LowStockThresholdGrams { get; init; }
    /// <summary>Gets the timestamp when the batch was received.</summary>
    public required DateTimeOffset ReceivedAt { get; init; }
}
