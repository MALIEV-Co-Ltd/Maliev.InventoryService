using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Maliev.InventoryService.Domain.Entities;

/// <summary>
/// Represents a consumption audit event for one exact physical inventory item.
/// </summary>
public class InventoryConsumptionEvent
{
    /// <summary>
    /// Gets or sets the unique consumption event identifier.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the physical inventory item identifier.
    /// </summary>
    [Required]
    public Guid InventoryBatchId { get; set; }

    /// <summary>
    /// Gets or sets the job identifier that consumed or reserved material.
    /// </summary>
    public Guid? JobId { get; set; }

    /// <summary>
    /// Gets or sets the order item identifier associated with the consumption.
    /// </summary>
    public Guid? OrderItemId { get; set; }

    /// <summary>
    /// Gets or sets the operator identifier that performed the scan or consumption.
    /// </summary>
    [MaxLength(120)]
    public string? OperatorId { get; set; }

    /// <summary>
    /// Gets or sets the machine identifier associated with the consumption.
    /// </summary>
    [MaxLength(120)]
    public string? MachineId { get; set; }

    /// <summary>
    /// Gets or sets the consumed quantity in the inventory item's native unit.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,3)")]
    public decimal QuantityConsumed { get; set; }

    /// <summary>
    /// Gets or sets the remaining item quantity after this consumption event.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,3)")]
    public decimal RemainingQuantityAfter { get; set; }

    /// <summary>
    /// Gets or sets optional consumption notes.
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the item was consumed.
    /// </summary>
    [Required]
    public DateTimeOffset ConsumedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the physical inventory item navigation property.
    /// </summary>
    public InventoryBatch InventoryBatch { get; set; } = null!;
}
