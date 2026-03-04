namespace Maliev.InventoryService.Application.Models;

/// <summary>
/// Request to register a new material batch.
/// </summary>
public record CreateBatchRequest
{
    /// <summary>Gets the unique identifier of the material.</summary>
    public required Guid MaterialId { get; init; }
    
    /// <summary>Gets the initial weight of the batch in grams.</summary>
    public required decimal InitialWeightGrams { get; init; }
    
    /// <summary>Gets the physical location where the batch is stored.</summary>
    public required string Location { get; init; }
    
    /// <summary>Gets the weight threshold in grams for low stock alerts.</summary>
    public decimal? LowStockThresholdGrams { get; init; }
}
