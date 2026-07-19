namespace Maliev.InventoryService.Application.Authorization;

/// <summary>
/// Provides access to predefined roles for the Inventory Service.
/// </summary>
public static class InventoryPredefinedRoles
{
    public const string Admin = "roles.inventory.admin";
    public const string Operator = "roles.inventory.operator";
    public const string Viewer = "roles.inventory.viewer";

    public static readonly IReadOnlyList<(string RoleId, string Description, string[] Permissions)> All = new List<(string, string, string[])>
    {
        (
            Admin,
            "Inventory Administrator with full access",
            new[]
            {
                InventoryPermissions.StockRead,
                InventoryPermissions.StockWrite,
                InventoryPermissions.BatchRead,
                InventoryPermissions.BatchWrite,
                InventoryPermissions.TransferCreate,
                InventoryPermissions.TransferApprove,
                InventoryPermissions.AlertManage,
            }
        ),
        (
            Operator,
            "Inventory Operator with stock and transfer access",
            new[]
            {
                InventoryPermissions.StockRead,
                InventoryPermissions.StockWrite,
                InventoryPermissions.BatchRead,
                InventoryPermissions.BatchWrite,
                InventoryPermissions.TransferCreate,
                InventoryPermissions.AlertManage,
            }
        ),
        (
            Viewer,
            "Inventory Viewer with read-only access",
            new[]
            {
                InventoryPermissions.StockRead,
                InventoryPermissions.BatchRead,
            }
        ),
    };
}
