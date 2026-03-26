namespace Maliev.InventoryService.Application.Authorization;

/// <summary>
/// Defines the permissions for the Inventory Service.
/// </summary>
public static class InventoryPermissions
{
    public const string StockRead = "inventory.stock.read";
    public const string StockWrite = "inventory.stock.write";

    public const string BatchRead = "inventory.batches.read";
    public const string BatchWrite = "inventory.batches.write";

    public const string TransferCreate = "inventory.transfers.create";
    public const string TransferApprove = "inventory.transfers.approve";

    public const string AlertManage = "inventory.alerts.manage";

    public static readonly IReadOnlyDictionary<string, string> AllWithDescriptions = new Dictionary<string, string>
    {
        { StockRead, "Read stock data" },
        { StockWrite, "Write stock data" },
        { BatchRead, "Read batch data" },
        { BatchWrite, "Write batch data" },
        { TransferCreate, "Create inventory transfers" },
        { TransferApprove, "Approve inventory transfers" },
        { AlertManage, "Manage inventory alerts" },
    };

    public static string[] All => AllWithDescriptions.Keys.ToArray();
}
