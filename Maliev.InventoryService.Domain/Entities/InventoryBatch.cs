using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Maliev.InventoryService.Domain.Entities;

/// <summary>
/// Represents a batch of material in the inventory (e.g., a spool of filament or a bottle of resin).
/// </summary>
public class InventoryBatch
{
    /// <summary>
    /// Gets or sets the unique identifier for the batch.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the ID of the material this batch contains.
    /// </summary>
    [Required]
    public Guid MaterialId { get; set; }

    /// <summary>
    /// Gets or sets the initial weight of the batch in grams.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal InitialWeightGrams { get; set; }

    /// <summary>
    /// Gets or sets the current remaining weight of the batch in grams.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal RemainingWeightGrams { get; set; }

    /// <summary>
    /// Gets or sets the batch status (e.g., Active, Depleted).
    /// </summary>
    [Required]
    public BatchStatus Status { get; set; } = BatchStatus.Active;

    /// <summary>
    /// Gets or sets the physical location of the batch in the workshop.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the threshold in grams at which a low stock alert is triggered.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal LowStockThresholdGrams { get; set; } = 100m;

    /// <summary>
    /// Gets or sets a value indicating whether a low stock alert has already been sent.
    /// </summary>
    [Required]
    public bool HasAlerted { get; set; } = false;

    /// <summary>
    /// Gets or sets the timestamp when the batch was received.
    /// </summary>
    [Required]
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optimistic concurrency token (PostgreSQL xmin).
    /// </summary>
    [Timestamp]
    public uint RowVersion { get; set; }
}
