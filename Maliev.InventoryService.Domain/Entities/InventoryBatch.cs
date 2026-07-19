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
    /// Gets or sets the short human-readable tracking code printed on QR labels.
    /// </summary>
    [Required]
    [MaxLength(32)]
    public string TrackingCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the QR payload used to navigate to this physical inventory item.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string QrPayload { get; set; } = string.Empty;

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
    /// Gets or sets the originally received quantity in the item's native unit.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,3)")]
    public decimal InitialQuantity { get; set; }

    /// <summary>
    /// Gets or sets the remaining quantity in the item's native unit.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,3)")]
    public decimal RemainingQuantity { get; set; }

    /// <summary>
    /// Gets or sets the native quantity unit, such as g, kg, pcs, ml, mm3, or m.
    /// </summary>
    [Required]
    [MaxLength(16)]
    public string QuantityUnit { get; set; } = "g";

    /// <summary>
    /// Gets or sets the physical form factor, such as Block, Spool, Sheet, Rod, Bottle, Bag, Piece, or Other.
    /// </summary>
    [Required]
    [MaxLength(32)]
    public string FormFactor { get; set; } = "Spool";

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
    /// Gets or sets the native quantity threshold at which a low stock alert is triggered.
    /// </summary>
    [Column(TypeName = "decimal(18,3)")]
    public decimal? LowStockThresholdQuantity { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a low stock alert has already been sent.
    /// </summary>
    [Required]
    public bool HasAlerted { get; set; } = false;

    /// <summary>
    /// Gets or sets the optional supplier identifier that provided this stock item.
    /// </summary>
    public Guid? SupplierId { get; set; }

    /// <summary>
    /// Gets or sets the optional purchase order identifier that received this stock item.
    /// </summary>
    public Guid? PurchaseOrderId { get; set; }

    /// <summary>
    /// Gets or sets the supplier or manufacturer lot number.
    /// </summary>
    [MaxLength(100)]
    public string? LotNumber { get; set; }

    /// <summary>
    /// Gets or sets the supplier or manufacturer SKU.
    /// </summary>
    [MaxLength(100)]
    public string? ManufacturerSku { get; set; }

    /// <summary>
    /// Gets or sets the material color for this stock item.
    /// </summary>
    [MaxLength(80)]
    public string? Color { get; set; }

    /// <summary>
    /// Gets or sets the material grade for this stock item.
    /// </summary>
    [MaxLength(120)]
    public string? MaterialGrade { get; set; }

    /// <summary>
    /// Gets or sets the item length in millimeters, when applicable.
    /// </summary>
    [Column(TypeName = "decimal(18,3)")]
    public decimal? LengthMm { get; set; }

    /// <summary>
    /// Gets or sets the item width in millimeters, when applicable.
    /// </summary>
    [Column(TypeName = "decimal(18,3)")]
    public decimal? WidthMm { get; set; }

    /// <summary>
    /// Gets or sets the item height in millimeters, when applicable.
    /// </summary>
    [Column(TypeName = "decimal(18,3)")]
    public decimal? HeightMm { get; set; }

    /// <summary>
    /// Gets or sets the item diameter in millimeters, when applicable.
    /// </summary>
    [Column(TypeName = "decimal(18,3)")]
    public decimal? DiameterMm { get; set; }

    /// <summary>
    /// Gets or sets the item thickness in millimeters, when applicable.
    /// </summary>
    [Column(TypeName = "decimal(18,3)")]
    public decimal? ThicknessMm { get; set; }

    /// <summary>
    /// Gets or sets the employee or integration that received this item.
    /// </summary>
    [MaxLength(120)]
    public string? ReceivedBy { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the batch was received.
    /// </summary>
    [Required]
    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the item consumption audit events.
    /// </summary>
    public ICollection<InventoryConsumptionEvent> ConsumptionEvents { get; set; } = new List<InventoryConsumptionEvent>();
}
