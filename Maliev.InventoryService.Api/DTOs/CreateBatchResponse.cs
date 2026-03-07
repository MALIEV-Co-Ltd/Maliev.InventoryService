namespace Maliev.InventoryService.Api.DTOs;

/// <summary>
/// Response containing the details of a newly created material batch.
/// </summary>
public record CreateBatchResponse
{
    /// <summary>Gets the unique identifier of the batch.</summary>
    public Guid Id { get; init; }
    /// <summary>Gets the unique identifier of the material.</summary>
    public Guid MaterialId { get; init; }
    /// <summary>Gets the initial weight in grams.</summary>
    public decimal InitialWeightGrams { get; init; }
    /// <summary>Gets the current remaining weight in grams.</summary>
    public decimal RemainingWeightGrams { get; init; }
    /// <summary>Gets the current status of the batch (e.g., Active, Depleted).</summary>
    public string Status { get; init; } = string.Empty;
    /// <summary>Gets the storage location.</summary>
    public string Location { get; init; } = string.Empty;
    /// <summary>Gets the low stock alert threshold.</summary>
    public decimal LowStockThresholdGrams { get; init; }
    /// <summary>Gets the timestamp when the batch was received.</summary>
    public DateTimeOffset ReceivedAt { get; init; }
}
