namespace Maliev.InventoryService.Api.Authorization;

/// <summary>
/// Defines the permissions for the Inventory Service.
/// </summary>
public static class InventoryPermissions
{
    /// <summary>Permission to read material stock levels.</summary>
    public const string StockRead = "inventory.stock.read";
    /// <summary>Permission to update material stock levels.</summary>
    public const string StockWrite = "inventory.stock.write";
    /// <summary>Permission to read material batch information.</summary>
    public const string BatchesRead = "inventory.batches.read";
    /// <summary>Permission to manage material batches.</summary>
    public const string BatchesWrite = "inventory.batches.write";

    /// <summary>
    /// Collection of all defined inventory permissions with descriptions.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> AllWithDescriptions = new Dictionary<string, string>
    {
        { StockRead, "Read material stock levels" },
        { StockWrite, "Update material stock levels" },
        { BatchesRead, "Read material batch information" },
        { BatchesWrite, "Manage material batches" }
    };

    /// <summary>
    /// Gets all defined permission codes.
    /// </summary>
    public static string[] All => AllWithDescriptions.Keys.ToArray();
}

/// <summary>
/// Provides access to predefined roles for the Inventory Service.
/// </summary>
public static class InventoryPredefinedRoles
{
    /// <summary>Role for administrators with full control.</summary>
    public const string Admin = "roles.inventory.admin";
    /// <summary>Role for inventory managers.</summary>
    public const string Manager = "roles.inventory.manager";
    /// <summary>Role for users with read-only access.</summary>
    public const string Viewer = "roles.inventory.viewer";

    /// <summary>
    /// Collection of all predefined roles for the Inventory Service.
    /// </summary>
    public static readonly IReadOnlyList<(string RoleId, string Description, string[] Permissions)> All = new List<(string, string, string[])>
    {
        (Admin, "Full administrative control over inventory", InventoryPermissions.All),
        (Manager, "Manage stock and batches", new[]
        {
            InventoryPermissions.StockRead,
            InventoryPermissions.StockWrite,
            InventoryPermissions.BatchesRead,
            InventoryPermissions.BatchesWrite
        }),
        (Viewer, "Read-only access to inventory", new[]
        {
            InventoryPermissions.StockRead,
            InventoryPermissions.BatchesRead
        })
    };
}
