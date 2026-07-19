using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Maliev.InventoryService.Infrastructure.Persistence;

/// <summary>
/// Factory for creating <see cref="InventoryDbContext"/> instances at design time.
/// </summary>
public class InventoryDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext>
{
    /// <summary>
    /// Creates a new instance of the <see cref="InventoryDbContext"/> class.
    /// </summary>
    /// <param name="args">Arguments provided by the design-time tool.</param>
    /// <returns>A new instance of the <see cref="InventoryDbContext"/> class.</returns>
    public InventoryDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<InventoryDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=inventory;Username=postgres;Password=postgres");

        return new InventoryDbContext(optionsBuilder.Options);
    }
}
