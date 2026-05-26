using System.ComponentModel.DataAnnotations;

namespace Maliev.InventoryService.Api.DTOs;

/// <summary>
/// Request to register one physical, QR-tracked inventory item.
/// </summary>
public record CreateInventoryItemRequest
{
    /// <summary>Gets the material identifier this item belongs to.</summary>
    [Required]
    public Guid MaterialId { get; init; }

    /// <summary>Gets the received quantity in the native unit.</summary>
    [Range(0.01, double.MaxValue, ErrorMessage = "Initial quantity must be greater than 0")]
    public decimal InitialQuantity { get; init; }

    /// <summary>Gets the native quantity unit, such as g, kg, pcs, ml, mm3, or m.</summary>
    [Required]
    [MaxLength(16)]
    public string QuantityUnit { get; init; } = "g";

    /// <summary>Gets the physical storage location.</summary>
    [Required]
    [MaxLength(200)]
    public string Location { get; init; } = string.Empty;

    /// <summary>Gets the item form factor, such as Block, Spool, Sheet, Rod, Bottle, Bag, Piece, or Other.</summary>
    [Required]
    [MaxLength(32)]
    public string FormFactor { get; init; } = "Other";

    /// <summary>Gets the optional native low-stock threshold.</summary>
    [Range(0, double.MaxValue)]
    public decimal? LowStockThresholdQuantity { get; init; }

    /// <summary>Gets the optional supplier identifier.</summary>
    public Guid? SupplierId { get; init; }

    /// <summary>Gets the optional purchase order identifier.</summary>
    public Guid? PurchaseOrderId { get; init; }

    /// <summary>Gets the optional lot number.</summary>
    [MaxLength(100)]
    public string? LotNumber { get; init; }

    /// <summary>Gets the optional manufacturer SKU.</summary>
    [MaxLength(100)]
    public string? ManufacturerSku { get; init; }

    /// <summary>Gets the optional item color.</summary>
    [MaxLength(80)]
    public string? Color { get; init; }

    /// <summary>Gets the optional material grade.</summary>
    [MaxLength(120)]
    public string? MaterialGrade { get; init; }

    /// <summary>Gets the optional length in millimeters.</summary>
    [Range(0, double.MaxValue)]
    public decimal? LengthMm { get; init; }

    /// <summary>Gets the optional width in millimeters.</summary>
    [Range(0, double.MaxValue)]
    public decimal? WidthMm { get; init; }

    /// <summary>Gets the optional height in millimeters.</summary>
    [Range(0, double.MaxValue)]
    public decimal? HeightMm { get; init; }

    /// <summary>Gets the optional diameter in millimeters.</summary>
    [Range(0, double.MaxValue)]
    public decimal? DiameterMm { get; init; }

    /// <summary>Gets the optional thickness in millimeters.</summary>
    [Range(0, double.MaxValue)]
    public decimal? ThicknessMm { get; init; }

    /// <summary>Gets the employee or integration that received this item.</summary>
    [MaxLength(120)]
    public string? ReceivedBy { get; init; }
}
