namespace Maliev.InventoryService.Api.DTOs;

/// <summary>
/// Response containing one physical QR-tracked inventory item.
/// </summary>
public record InventoryItemResponse
{
    /// <summary>Gets the item identifier.</summary>
    public Guid Id { get; init; }
    /// <summary>Gets the material identifier.</summary>
    public Guid MaterialId { get; init; }
    /// <summary>Gets the short tracking code printed on the item label.</summary>
    public string TrackingCode { get; init; } = string.Empty;
    /// <summary>Gets the QR payload that resolves this item.</summary>
    public string QrPayload { get; init; } = string.Empty;
    /// <summary>Gets the originally received quantity in the native unit.</summary>
    public decimal InitialQuantity { get; init; }
    /// <summary>Gets the remaining quantity in the native unit.</summary>
    public decimal RemainingQuantity { get; init; }
    /// <summary>Gets the native quantity unit.</summary>
    public string QuantityUnit { get; init; } = string.Empty;
    /// <summary>Gets the originally received weight in grams when available.</summary>
    public decimal InitialWeightGrams { get; init; }
    /// <summary>Gets the remaining weight in grams when available.</summary>
    public decimal RemainingWeightGrams { get; init; }
    /// <summary>Gets the current item lifecycle status.</summary>
    public string Status { get; init; } = string.Empty;
    /// <summary>Gets the physical storage location.</summary>
    public string Location { get; init; } = string.Empty;
    /// <summary>Gets the physical item form factor.</summary>
    public string FormFactor { get; init; } = string.Empty;
    /// <summary>Gets the native low-stock threshold.</summary>
    public decimal? LowStockThresholdQuantity { get; init; }
    /// <summary>Gets the optional supplier identifier.</summary>
    public Guid? SupplierId { get; init; }
    /// <summary>Gets the optional purchase order identifier.</summary>
    public Guid? PurchaseOrderId { get; init; }
    /// <summary>Gets the optional lot number.</summary>
    public string? LotNumber { get; init; }
    /// <summary>Gets the optional manufacturer SKU.</summary>
    public string? ManufacturerSku { get; init; }
    /// <summary>Gets the optional color.</summary>
    public string? Color { get; init; }
    /// <summary>Gets the optional material grade.</summary>
    public string? MaterialGrade { get; init; }
    /// <summary>Gets the optional length in millimeters.</summary>
    public decimal? LengthMm { get; init; }
    /// <summary>Gets the optional width in millimeters.</summary>
    public decimal? WidthMm { get; init; }
    /// <summary>Gets the optional height in millimeters.</summary>
    public decimal? HeightMm { get; init; }
    /// <summary>Gets the optional diameter in millimeters.</summary>
    public decimal? DiameterMm { get; init; }
    /// <summary>Gets the optional thickness in millimeters.</summary>
    public decimal? ThicknessMm { get; init; }
    /// <summary>Gets the employee or integration that received this item.</summary>
    public string? ReceivedBy { get; init; }
    /// <summary>Gets the timestamp when the item was received.</summary>
    public DateTimeOffset ReceivedAt { get; init; }
}
