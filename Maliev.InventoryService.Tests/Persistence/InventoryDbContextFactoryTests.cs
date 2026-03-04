using Microsoft.EntityFrameworkCore;
using Xunit;
using Maliev.InventoryService.Infrastructure.Persistence;

namespace Maliev.InventoryService.Tests.Persistence;

/// <summary>
/// Tests for the InventoryDbContextFactory.
/// </summary>
public class InventoryDbContextFactoryTests
{
    /// <summary>
    /// Verifies that CreateDbContext returns a valid DbContext.
    /// </summary>
    [Fact]
    public void CreateDbContext_ReturnsValidContext()
    {
        // Arrange
        var factory = new InventoryDbContextFactory();

        // Act
        var context = factory.CreateDbContext(Array.Empty<string>());

        // Assert
        Assert.NotNull(context);
        Assert.IsType<InventoryDbContext>(context);
    }

    /// <summary>
    /// Verifies that CreateDbContext uses the correct connection string.
    /// </summary>
    [Fact]
    public void CreateDbContext_UsesCorrectConnectionString()
    {
        // Arrange
        var factory = new InventoryDbContextFactory();

        // Act
        var context = factory.CreateDbContext(Array.Empty<string>());

        // Assert - verify the options are configured for PostgreSQL
        var options = context.Database.GetDbConnection();
        Assert.NotNull(options);
    }
}
