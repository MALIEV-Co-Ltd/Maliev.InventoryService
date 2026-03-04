using Maliev.InventoryService.Application.Models;
using Maliev.InventoryService.Domain.Entities;

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
