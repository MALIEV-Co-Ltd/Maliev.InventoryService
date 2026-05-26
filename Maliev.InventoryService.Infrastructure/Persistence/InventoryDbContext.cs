using Microsoft.EntityFrameworkCore;
using Maliev.InventoryService.Domain.Entities;

namespace Maliev.InventoryService.Infrastructure.Persistence;

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
    /// Gets or sets physical inventory item consumption audit events.
    /// </summary>
    public DbSet<InventoryConsumptionEvent> InventoryConsumptionEvents { get; set; } = null!;

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

            entity.Property(e => e.TrackingCode)
                .HasMaxLength(32)
                .IsRequired();

            entity.Property(e => e.QrPayload)
                .HasMaxLength(256)
                .IsRequired();

            entity.Property(e => e.InitialWeightGrams)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(e => e.RemainingWeightGrams)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(e => e.InitialQuantity)
                .HasPrecision(18, 3)
                .IsRequired();

            entity.Property(e => e.RemainingQuantity)
                .HasPrecision(18, 3)
                .IsRequired();

            entity.Property(e => e.QuantityUnit)
                .HasMaxLength(16)
                .HasDefaultValue("g")
                .IsRequired();

            entity.Property(e => e.FormFactor)
                .HasMaxLength(32)
                .HasDefaultValue("Spool")
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

            entity.Property(e => e.LowStockThresholdQuantity)
                .HasPrecision(18, 3);

            entity.Property(e => e.HasAlerted)
                .HasDefaultValue(false)
                .IsRequired();

            entity.Property(e => e.LotNumber)
                .HasMaxLength(100);

            entity.Property(e => e.ManufacturerSku)
                .HasMaxLength(100);

            entity.Property(e => e.Color)
                .HasMaxLength(80);

            entity.Property(e => e.MaterialGrade)
                .HasMaxLength(120);

            entity.Property(e => e.LengthMm)
                .HasPrecision(18, 3);

            entity.Property(e => e.WidthMm)
                .HasPrecision(18, 3);

            entity.Property(e => e.HeightMm)
                .HasPrecision(18, 3);

            entity.Property(e => e.DiameterMm)
                .HasPrecision(18, 3);

            entity.Property(e => e.ThicknessMm)
                .HasPrecision(18, 3);

            entity.Property(e => e.ReceivedBy)
                .HasMaxLength(120);

            entity.Property(e => e.ReceivedAt)
                .HasConversion<DateTimeOffset>()
                .IsRequired();

            // xmin for optimistic concurrency
            entity.Property<uint>("xmin")
                .HasColumnType("xid")
                .IsRowVersion();

            // Indexes
            entity.HasIndex(e => e.MaterialId)
                .HasDatabaseName("IX_InventoryBatches_MaterialId");

            entity.HasIndex(e => e.TrackingCode)
                .IsUnique()
                .HasDatabaseName("IX_InventoryBatches_TrackingCode");

            entity.HasIndex(e => e.Status)
                .HasDatabaseName("IX_InventoryBatches_Status");

            entity.HasIndex(e => e.ReceivedAt)
                .HasDatabaseName("IX_InventoryBatches_ReceivedAt");

            // Composite index for FIFO selection
            entity.HasIndex(e => new { e.MaterialId, e.Status, e.ReceivedAt, e.Id })
                .HasDatabaseName("IX_InventoryBatches_FIFO");
        });

        modelBuilder.Entity<InventoryConsumptionEvent>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.QuantityConsumed)
                .HasPrecision(18, 3)
                .IsRequired();

            entity.Property(e => e.RemainingQuantityAfter)
                .HasPrecision(18, 3)
                .IsRequired();

            entity.Property(e => e.OperatorId)
                .HasMaxLength(120);

            entity.Property(e => e.MachineId)
                .HasMaxLength(120);

            entity.Property(e => e.Notes)
                .HasMaxLength(500);

            entity.Property(e => e.ConsumedAt)
                .HasConversion<DateTimeOffset>()
                .IsRequired();

            entity.HasOne(e => e.InventoryBatch)
                .WithMany(b => b.ConsumptionEvents)
                .HasForeignKey(e => e.InventoryBatchId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.InventoryBatchId)
                .HasDatabaseName("IX_InventoryConsumptionEvents_InventoryBatchId");

            entity.HasIndex(e => e.JobId)
                .HasDatabaseName("IX_InventoryConsumptionEvents_JobId");

            entity.HasIndex(e => e.ConsumedAt)
                .HasDatabaseName("IX_InventoryConsumptionEvents_ConsumedAt");
        });
    }

    /// <summary>
    /// Synchronous SaveChanges.
    /// </summary>
    public override int SaveChanges()
    {
        PrepareInventoryItems();
        return base.SaveChanges();
    }

    /// <summary>
    /// Asynchronous SaveChangesAsync.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        PrepareInventoryItems();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void PrepareInventoryItems()
    {
        var addedBatches = ChangeTracker
            .Entries<InventoryBatch>()
            .Where(entry => entry.State == EntityState.Added)
            .Select(entry => entry.Entity)
            .ToList();

        foreach (var batch in addedBatches)
        {
            if (string.IsNullOrWhiteSpace(batch.TrackingCode))
            {
                batch.TrackingCode = GenerateFallbackTrackingCode();
            }

            if (string.IsNullOrWhiteSpace(batch.QrPayload))
            {
                batch.QrPayload = BuildQrPayload(batch.TrackingCode);
            }

            if (batch.InitialQuantity <= 0 && batch.InitialWeightGrams > 0)
            {
                batch.InitialQuantity = batch.InitialWeightGrams;
            }

            if (batch.RemainingQuantity <= 0 && batch.RemainingWeightGrams > 0)
            {
                batch.RemainingQuantity = batch.RemainingWeightGrams;
            }

            if (string.IsNullOrWhiteSpace(batch.QuantityUnit))
            {
                batch.QuantityUnit = "g";
            }

            if (string.IsNullOrWhiteSpace(batch.FormFactor))
            {
                batch.FormFactor = "Spool";
            }
        }
    }

    private static string GenerateFallbackTrackingCode()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        return $"INV-{DateTimeOffset.UtcNow:yy}-{suffix}";
    }

    private static string BuildQrPayload(string trackingCode)
    {
        return $"/mfg/inventory/items/{trackingCode}";
    }
}
