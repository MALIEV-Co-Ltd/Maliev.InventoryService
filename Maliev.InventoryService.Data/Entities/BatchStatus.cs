namespace Maliev.InventoryService.Data.Entities;

/// <summary>
/// Defines the status of a material batch.
/// </summary>
public enum BatchStatus
{
    /// <summary>
    /// The batch is active and can be used for jobs.
    /// </summary>
    Active = 0,

    /// <summary>
    /// The batch has been fully consumed.
    /// </summary>
    Depleted = 1
}
