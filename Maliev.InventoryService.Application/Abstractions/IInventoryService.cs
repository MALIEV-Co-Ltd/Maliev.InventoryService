using Maliev.InventoryService.Application.Models;

namespace Maliev.InventoryService.Application.Abstractions;

/// <summary>
/// Defines application-level operations for managing material inventory.
/// </summary>
public interface IInventoryService
{
    /// <summary>
    /// Creates a new material batch in the inventory.
    /// </summary>
    /// <param name="request">The batch creation request.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The created batch result.</returns>
    Task<CreateBatchResult> CreateBatchAsync(CreateBatchRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new physical inventory item with QR label metadata.
    /// </summary>
    /// <param name="request">The item creation request.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The created item result.</returns>
    Task<InventoryItemResult> CreateInventoryItemAsync(CreateInventoryItemRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists physical inventory items.
    /// </summary>
    /// <param name="materialId">Optional material identifier filter.</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The matching item results.</returns>
    Task<IReadOnlyList<InventoryItemResult>> ListInventoryItemsAsync(
        Guid? materialId,
        string? status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets one physical inventory item by its short tracking code.
    /// </summary>
    /// <param name="trackingCode">The tracking code from the QR label.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The item result when found; otherwise null.</returns>
    Task<InventoryItemResult?> GetInventoryItemByTrackingCodeAsync(string trackingCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consumes material from one exact physical inventory item.
    /// </summary>
    /// <param name="trackingCode">The tracking code from the QR label.</param>
    /// <param name="request">The consumption request.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The updated item result when found; otherwise null.</returns>
    Task<InventoryItemResult?> ConsumeInventoryItemAsync(
        string trackingCode,
        ConsumeInventoryItemRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status of material batches.
    /// </summary>
    /// <param name="materialId">Optional material identifier filter.</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A collection of material status summaries.</returns>
    Task<IReadOnlyList<MaterialStatusSummaryResult>> GetStatusAsync(
        Guid? materialId,
        string? status,
        CancellationToken cancellationToken = default);
}
