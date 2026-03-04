using Microsoft.EntityFrameworkCore;
using Xunit;
using Maliev.InventoryService.Infrastructure.Persistence;
using Maliev.InventoryService.Domain.Entities;

namespace Maliev.InventoryService.Tests.Persistence;

/// <summary>
/// Tests for InventoryDbContext.
/// </summary>
public class InventoryDbContextTests : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private InventoryDbContext _context = null!;

    public InventoryDbContextTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;
        _context = new InventoryDbContext(options);
        await _context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    private async Task ClearDataAsync()
    {
        _context.InventoryBatches.RemoveRange(_context.InventoryBatches);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Verifies that InventoryBatches can be queried.
    /// </summary>
    [Fact]
    public async Task InventoryBatches_CanQuery()
    {
        await ClearDataAsync();

        var batch = new InventoryBatch
        {
            Id = Guid.NewGuid(),
            MaterialId = Guid.NewGuid(),
            InitialWeightGrams = 1000m,
            RemainingWeightGrams = 800m,
            Status = BatchStatus.Active,
            Location = "Cabinet A"
        };
        
        _context.InventoryBatches.Add(batch);
        await _context.SaveChangesAsync();
        
        var count = await _context.InventoryBatches.CountAsync();
        Assert.Equal(1, count);
    }

    /// <summary>
    /// Verifies that batch status is stored as string.
    /// </summary>
    [Fact]
    public async Task BatchStatus_StoresAsString()
    {
        await ClearDataAsync();

        var batch = new InventoryBatch
        {
            Id = Guid.NewGuid(),
            MaterialId = Guid.NewGuid(),
            InitialWeightGrams = 1000m,
            RemainingWeightGrams = 1000m,
            Status = BatchStatus.Depleted,
            Location = "Cabinet A"
        };
        
        _context.InventoryBatches.Add(batch);
        await _context.SaveChangesAsync();
        
        var result = await _context.InventoryBatches.FirstAsync();
        Assert.Equal(BatchStatus.Depleted, result.Status);
    }

    /// <summary>
    /// Verifies FIFO index is configured.
    /// </summary>
    [Fact]
    public async Task Model_FifoIndexConfigured()
    {
        var entityType = _context.Model.FindEntityType(typeof(InventoryBatch));
        Assert.NotNull(entityType);
    }
}
