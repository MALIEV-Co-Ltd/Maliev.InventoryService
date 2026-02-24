using System.ComponentModel.DataAnnotations;

namespace Maliev.InventoryService.Api.DTOs;

/// <summary>
/// Request to register a new material batch.
/// </summary>
public record CreateBatchRequest
{
    /// <summary>Gets the unique identifier of the material.</summary>
    [Required]
    public Guid MaterialId { get; init; }
    
    /// <summary>Gets the initial weight of the batch in grams.</summary>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Initial weight must be greater than 0")]
    public decimal InitialWeightGrams { get; init; }
    
    /// <summary>Gets the physical location where the batch is stored.</summary>
    [Required]
    [MaxLength(200, ErrorMessage = "Location must be 200 characters or less")]
    public string Location { get; init; } = string.Empty;
    
    /// <summary>Gets the weight threshold in grams for low stock alerts.</summary>
    [Range(0, double.MaxValue, ErrorMessage = "Threshold must be non-negative")]
    public decimal? LowStockThresholdGrams { get; init; }
}
