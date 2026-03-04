using Xunit;
using Maliev.InventoryService.Api.Authorization;

namespace Maliev.InventoryService.Tests.Authorization;

/// <summary>
/// Tests for InventoryPermissions.
/// </summary>
public class InventoryPermissionsTests
{
    /// <summary>
    /// Verifies StockRead permission exists.
    /// </summary>
    [Fact]
    public void StockRead_IsDefined()
    {
        Assert.NotNull(InventoryPermissions.StockRead);
        Assert.Equal("inventory.stock.read", InventoryPermissions.StockRead);
    }

    /// <summary>
    /// Verifies StockWrite permission exists.
    /// </summary>
    [Fact]
    public void StockWrite_IsDefined()
    {
        Assert.NotNull(InventoryPermissions.StockWrite);
        Assert.Equal("inventory.stock.write", InventoryPermissions.StockWrite);
    }

    /// <summary>
    /// Verifies All permissions contains all defined permissions.
    /// </summary>
    [Fact]
    public void All_ContainsAllPermissions()
    {
        var all = InventoryPermissions.All;
        Assert.Contains(InventoryPermissions.StockRead, all);
        Assert.Contains(InventoryPermissions.StockWrite, all);
    }
}
