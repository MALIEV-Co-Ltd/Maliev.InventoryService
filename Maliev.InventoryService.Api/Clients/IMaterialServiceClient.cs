using Maliev.InventoryService.Api.DTOs;

namespace Maliev.InventoryService.Api.Clients;

/// <summary>
/// Client for interacting with the Material Service.
/// </summary>
public interface IMaterialServiceClient
{
    /// <summary>
    /// Gets material details from the Material Service.
    /// </summary>
    /// <param name="materialId">The unique identifier of the material.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The material details if found, otherwise null.</returns>
    Task<MaterialDto?> GetMaterialAsync(Guid materialId, CancellationToken cancellationToken = default);
}
