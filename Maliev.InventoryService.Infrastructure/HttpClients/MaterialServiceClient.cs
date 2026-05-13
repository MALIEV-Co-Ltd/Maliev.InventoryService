using System.Net.Http.Json;
using Maliev.InventoryService.Domain.Clients;
using Maliev.InventoryService.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Maliev.InventoryService.Infrastructure.HttpClients;

/// <summary>
/// Implementation of the material service client.
/// </summary>
public class MaterialServiceClient : IMaterialServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MaterialServiceClient> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MaterialServiceClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="logger">The logger.</param>
    public MaterialServiceClient(HttpClient httpClient, ILogger<MaterialServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<MaterialDto?> GetMaterialAsync(Guid materialId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/material/v1/materials/{materialId}", cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Material {MaterialId} not found in Material Service", materialId);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var material = await response.Content.ReadFromJsonAsync<MaterialDto>(cancellationToken: cancellationToken);
            return material;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to retrieve material {MaterialId} from Material Service", materialId);
            throw;
        }
    }
}
