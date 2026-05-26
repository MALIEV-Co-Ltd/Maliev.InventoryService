using System.ComponentModel.DataAnnotations;

namespace Maliev.InventoryService.Api.DTOs;

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
    [MaxLength(120)]
    public string? OperatorId { get; init; }

    /// <summary>Gets the optional machine identifier associated with the consumption.</summary>
    [MaxLength(120)]
    public string? MachineId { get; init; }

    /// <summary>Gets the quantity consumed in the item native unit.</summary>
    [Range(0.01, double.MaxValue, ErrorMessage = "Quantity consumed must be greater than 0")]
    public decimal QuantityConsumed { get; init; }

    /// <summary>Gets optional consumption notes.</summary>
    [MaxLength(500)]
    public string? Notes { get; init; }
}
