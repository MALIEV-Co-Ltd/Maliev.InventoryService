namespace Maliev.InventoryService.Application.Models;

/// <summary>
/// Request to consume quantity from one exact physical inventory item.
/// </summary>
public record ConsumeInventoryItemRequest
{
    /// <summary>Gets the optional job identifier associated with the consumption.</summary>
    public Guid? JobId { get; init; }

    /// <summary>Gets the optional order item identifier associated with the consumption.</summary>
    public Guid? OrderItemId { get; init; }

    /// <summary>Gets the optional operator identifier that scanned or consumed the item.</summary>
    public string? OperatorId { get; init; }

    /// <summary>Gets the optional machine identifier associated with the consumption.</summary>
    public string? MachineId { get; init; }

    /// <summary>Gets the quantity consumed in the inventory item's native unit.</summary>
    public required decimal QuantityConsumed { get; init; }

    /// <summary>Gets optional consumption notes.</summary>
    public string? Notes { get; init; }
}
