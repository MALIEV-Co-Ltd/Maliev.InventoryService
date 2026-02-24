using Microsoft.EntityFrameworkCore;
using Maliev.InventoryService.Data.Entities;

namespace Maliev.InventoryService.Data;

/// <summary>
/// Database context for the Inventory service.
/// </summary>
public class InventoryDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryDbContext"/> class.
    /// </summary>
    /// <param name="options">The options for this context.</param>
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) 
        : base(options) { }

    /// <summary>
    /// Gets or sets the inventory batches.
    /// </summary>
    public DbSet<InventoryBatch> InventoryBatches { get; set; } = null!;

    /// <summary>
    /// Configures the model that was discovered by convention from the entity types.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryBatch>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.MaterialId)
                .IsRequired();
            
            entity.Property(e => e.InitialWeightGrams)
                .HasPrecision(18, 2)
                .IsRequired();
            
            entity.Property(e => e.RemainingWeightGrams)
                .HasPrecision(18, 2)
                .IsRequired();
            
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasDefaultValue(BatchStatus.Active)
                .IsRequired();
            
            entity.Property(e => e.Location)
                .HasMaxLength(200)
                .IsRequired();
            
            entity.Property(e => e.LowStockThresholdGrams)
                .HasPrecision(18, 2)
                .HasDefaultValue(100m)
                .IsRequired();
            
            entity.Property(e => e.HasAlerted)
                .HasDefaultValue(false)
                .IsRequired();
            
            entity.Property(e => e.ReceivedAt)
                .IsRequired();
            
            entity.Property(e => e.RowVersion)
                .IsConcurrencyToken()
                .ValueGeneratedNever()
                .IsRequired();

            
            // Indexes
            entity.HasIndex(e => e.MaterialId)
                .HasDatabaseName("IX_InventoryBatches_MaterialId");
            
            entity.HasIndex(e => e.Status)
                .HasDatabaseName("IX_InventoryBatches_Status");
            
            entity.HasIndex(e => e.ReceivedAt)
                .HasDatabaseName("IX_InventoryBatches_ReceivedAt");
            
            // Composite index for FIFO selection
            entity.HasIndex(e => new { e.MaterialId, e.Status, e.ReceivedAt, e.Id })
                .HasDatabaseName("IX_InventoryBatches_FIFO");
        });
    }

    /// <summary>
    /// Synchronous SaveChanges with RowVersion increment.
    /// </summary>
    public override int SaveChanges()
    {
        HandleRowVersion();
        return base.SaveChanges();
    }

    /// <summary>
    /// Asynchronous SaveChangesAsync with RowVersion increment.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        HandleRowVersion();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void HandleRowVersion()
    {
        var entries = ChangeTracker.Entries<InventoryBatch>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(i => i.RowVersion).CurrentValue = BitConverter.GetBytes(1L);
            }
            else if (entry.State == EntityState.Modified)
            {
                var originalVersion = entry.Property(i => i.RowVersion).OriginalValue;

                if (originalVersion == null || originalVersion.Length < 8)
                {
                    entry.Property(i => i.RowVersion).CurrentValue = BitConverter.GetBytes(1L);
                }
                else
                {
                    var versionNumber = BitConverter.ToInt64(originalVersion, 0);
                    versionNumber++;
                    entry.Property(i => i.RowVersion).CurrentValue = BitConverter.GetBytes(versionNumber);
                }
            }
        }
    }
}
