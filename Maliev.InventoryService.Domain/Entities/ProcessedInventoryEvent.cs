using System.ComponentModel.DataAnnotations;

namespace Maliev.InventoryService.Domain.Entities;

/// <summary>
/// Records an inventory event that has already mutated stock.
/// </summary>
public class ProcessedInventoryEvent
{
    /// <summary>
    /// Gets or sets the unique processed event identifier.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the upstream message identifier.
    /// </summary>
    [Required]
    public Guid MessageId { get; set; }

    /// <summary>
    /// Gets or sets the production job identifier from the upstream event.
    /// </summary>
    public Guid? JobId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the event was processed.
    /// </summary>
    [Required]
    public DateTimeOffset ProcessedAt { get; set; } = DateTimeOffset.UtcNow;
}
