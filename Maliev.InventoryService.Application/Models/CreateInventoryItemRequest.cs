namespace Maliev.InventoryService.Application.Models;

/// <summary>
/// Request to register one physical, QR-tracked stock item.
/// </summary>
public record CreateInventoryItemRequest
{
    /// <summary>Gets the unique identifier of the material.</summary>
    public required Guid MaterialId { get; init; }

    /// <summary>Gets the originally received quantity in the native unit.</summary>
    public required decimal InitialQuantity { get; init; }

    /// <summary>Gets the native quantity unit, such as g, kg, pcs, ml, mm3, or m.</summary>
    public required string QuantityUnit { get; init; }

    /// <summary>Gets the physical storage location.</summary>
    public required string Location { get; init; }

    /// <summary>Gets the physical form factor, such as Block, Spool, Sheet, Rod, Bottle, Bag, Piece, or Other.</summary>
    public required string FormFactor { get; init; }

    /// <summary>Gets the optional native low-stock threshold.</summary>
    public decimal? LowStockThresholdQuantity { get; init; }

    /// <summary>Gets the optional supplier identifier.</summary>
    public Guid? SupplierId { get; init; }

    /// <summary>Gets the optional purchase order identifier.</summary>
    public Guid? PurchaseOrderId { get; init; }

    /// <summary>Gets the optional lot number.</summary>
    public string? LotNumber { get; init; }

    /// <summary>Gets the optional manufacturer SKU.</summary>
    public string? ManufacturerSku { get; init; }

    /// <summary>Gets the optional item color.</summary>
    public string? Color { get; init; }

    /// <summary>Gets the optional material grade.</summary>
    public string? MaterialGrade { get; init; }

    /// <summary>Gets the optional item length in millimeters.</summary>
    public decimal? LengthMm { get; init; }

    /// <summary>Gets the optional item width in millimeters.</summary>
    public decimal? WidthMm { get; init; }

    /// <summary>Gets the optional item height in millimeters.</summary>
    public decimal? HeightMm { get; init; }

    /// <summary>Gets the optional item diameter in millimeters.</summary>
    public decimal? DiameterMm { get; init; }

    /// <summary>Gets the optional item thickness in millimeters.</summary>
    public decimal? ThicknessMm { get; init; }

    /// <summary>Gets the employee or integration that received this item.</summary>
    public string? ReceivedBy { get; init; }
}
