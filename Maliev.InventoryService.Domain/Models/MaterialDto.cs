namespace Maliev.InventoryService.Domain.Models;

/// <summary>
/// Data transfer object for material information from the Material Service.
/// </summary>
public record MaterialDto
{
    /// <summary>Gets the unique identifier of the material.</summary>
    public Guid Id { get; init; }
    /// <summary>Gets the display name of the material.</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>Gets the density of the material in g/cm³.</summary>
    public decimal Density { get; init; }
}
