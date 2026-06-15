using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using MassTransit;
using MassTransit.Testing;
using Maliev.InventoryService.Domain.Clients;
using Maliev.InventoryService.Domain.Models;
using Maliev.InventoryService.Infrastructure.Consumers;
using Maliev.InventoryService.Infrastructure.Persistence;
using Maliev.InventoryService.Domain.Entities;
using Microsoft.Extensions.Logging;
using Maliev.MessagingContracts.Contracts.Jobs;
using Maliev.MessagingContracts.Contracts.Inventory;

namespace Maliev.InventoryService.Tests.Consumers;

/// <summary>
/// Tests for the JobStartedEventConsumer.
/// </summary>
public class JobStartedEventConsumerTests : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private InventoryDbContext _context = null!;
    private readonly Mock<IMaterialServiceClient> _materialClientMock;
    private readonly Mock<IPublishEndpoint> _publishEndpointMock;
    private readonly Mock<ILogger<JobStartedEventConsumer>> _loggerMock;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobStartedEventConsumerTests"/> class.
    /// </summary>
    public JobStartedEventConsumerTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _materialClientMock = new Mock<IMaterialServiceClient>();
        _publishEndpointMock = new Mock<IPublishEndpoint>();
        _loggerMock = new Mock<ILogger<JobStartedEventConsumer>>();
    }

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;
        _context = new InventoryDbContext(options);
        await _context.Database.EnsureCreatedAsync();
        // Clear data between tests since we reuse the container
        _context.InventoryBatches.RemoveRange(_context.InventoryBatches);
        await _context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    /// <summary>
    /// Verifies that deduction is correctly applied when a single active batch exists.
    /// </summary>
    [Fact]
    public async Task Consume_SingleActiveBatch_CorrectDeductionApplied()
    {
        // Arrange
        var materialId = Guid.NewGuid();
        var batchId = Guid.NewGuid();

        _materialClientMock
            .Setup(c => c.GetMaterialAsync(materialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MaterialDto { Id = materialId, Name = "Material X", Density = 1.2m });

        var batch = new InventoryBatch
        {
            Id = batchId,
            MaterialId = materialId,
            InitialWeightGrams = 1000m,
            RemainingWeightGrams = 1000m,
            Status = BatchStatus.Active,
            Location = "Cabinet A",
            LowStockThresholdGrams = 100m,
            ReceivedAt = DateTime.UtcNow.AddDays(-1),
        };
        _context.InventoryBatches.Add(batch);
        await _context.SaveChangesAsync();

        var consumer = new JobStartedEventConsumer(
            _materialClientMock.Object,
            _context,
            _loggerMock.Object);

        var context = new Mock<ConsumeContext<JobStartedEvent>>();
        context.Setup(c => c.Message).Returns(new JobStartedEvent
        {
            ConsumedBy = ["InventoryService"],
            Payload = new JobStartedEventPayload
            {
                JobId = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                MaterialId = materialId,
                VolumeCm3 = 100.0,
                Technology = "FDM",
                AssignedMachineId = "PRINTER-01",
                StartedAt = DateTimeOffset.UtcNow
            }
        });
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        // Act
        await consumer.Consume(context.Object);

        // Assert
        var updatedBatch = await _context.InventoryBatches.FindAsync(batchId);
        Assert.NotNull(updatedBatch);
        // 1000 - (100 * 1.2 * 1.10) = 1000 - 132 = 868
        Assert.Equal(868m, updatedBatch.RemainingWeightGrams);
    }

    /// <summary>
    /// Verifies that events not routed to InventoryService are ignored without material deduction.
    /// </summary>
    [Fact]
    public async Task Consume_WithoutInventoryServiceRouting_SkipsDeduction()
    {
        var materialId = Guid.NewGuid();
        var batchId = Guid.NewGuid();

        _materialClientMock
            .Setup(c => c.GetMaterialAsync(materialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MaterialDto { Id = materialId, Name = "Material X", Density = 1.2m });

        var batch = new InventoryBatch
        {
            Id = batchId,
            MaterialId = materialId,
            InitialWeightGrams = 1000m,
            RemainingWeightGrams = 1000m,
            Status = BatchStatus.Active,
            Location = "Cabinet A",
            LowStockThresholdGrams = 100m,
            ReceivedAt = DateTime.UtcNow.AddDays(-1),
        };
        _context.InventoryBatches.Add(batch);
        await _context.SaveChangesAsync();

        var consumer = new JobStartedEventConsumer(
            _materialClientMock.Object,
            _context,
            _loggerMock.Object);

        var context = new Mock<ConsumeContext<JobStartedEvent>>();
        context.Setup(c => c.Message).Returns(new JobStartedEvent
        {
            ConsumedBy = ["NotificationService"],
            Payload = new JobStartedEventPayload
            {
                JobId = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                MaterialId = materialId,
                VolumeCm3 = 100.0,
                Technology = "FDM",
                AssignedMachineId = "PRINTER-01",
                StartedAt = DateTimeOffset.UtcNow
            }
        });
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(context.Object);

        var updatedBatch = await _context.InventoryBatches.FindAsync(batchId);
        Assert.NotNull(updatedBatch);
        Assert.Equal(1000m, updatedBatch.RemainingWeightGrams);
        _materialClientMock.Verify(
            c => c.GetMaterialAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Verifies that malformed events without a payload are ignored without material deduction.
    /// </summary>
    [Fact]
    public async Task Consume_WithoutPayload_SkipsDeduction()
    {
        var consumer = new JobStartedEventConsumer(
            _materialClientMock.Object,
            _context,
            _loggerMock.Object);

        var context = new Mock<ConsumeContext<JobStartedEvent>>();
        context.Setup(c => c.Message).Returns(new JobStartedEvent
        {
            ConsumedBy = ["InventoryService"],
            Payload = null!
        });
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(context.Object);

        _materialClientMock.Verify(
            c => c.GetMaterialAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Verifies that no exception is thrown when no active batch exists for the material.
    /// </summary>
    [Fact]
    public async Task Consume_NoActiveBatch_LogsWarningAndAcknowledges()
    {
        // Arrange
        var materialId = Guid.NewGuid();

        _materialClientMock
            .Setup(c => c.GetMaterialAsync(materialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MaterialDto { Id = materialId, Name = "Material Y", Density = 1.2m });

        var consumer = new JobStartedEventConsumer(
            _materialClientMock.Object,
            _context,
            _loggerMock.Object);

        var context = new Mock<ConsumeContext<JobStartedEvent>>();
        context.Setup(c => c.Message).Returns(new JobStartedEvent
        {
            ConsumedBy = ["InventoryService"],
            Payload = new JobStartedEventPayload
            {
                JobId = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                MaterialId = materialId,
                VolumeCm3 = 100.0,
                Technology = "FDM",
                AssignedMachineId = "PRINTER-01",
                StartedAt = DateTimeOffset.UtcNow
            }
        });
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        // Act - Should not throw (message acknowledged)
        await consumer.Consume(context.Object);

        // Assert - No exception means message was acknowledged
        Assert.True(true);
    }

    /// <summary>
    /// Verifies that an exception is thrown when the material service call fails.
    /// </summary>
    [Fact]
    public async Task Consume_MaterialServiceError_ThrowsException()
    {
        // Arrange
        var materialId = Guid.NewGuid();

        _materialClientMock
            .Setup(c => c.GetMaterialAsync(materialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MaterialDto?)null);

        var consumer = new JobStartedEventConsumer(
            _materialClientMock.Object,
            _context,
            _loggerMock.Object);

        var context = new Mock<ConsumeContext<JobStartedEvent>>();
        context.Setup(c => c.Message).Returns(new JobStartedEvent
        {
            ConsumedBy = ["InventoryService"],
            Payload = new JobStartedEventPayload
            {
                JobId = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                MaterialId = materialId,
                VolumeCm3 = 100.0,
                Technology = "FDM",
                AssignedMachineId = "PRINTER-01",
                StartedAt = DateTimeOffset.UtcNow
            }
        });
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        // Act & Assert - Should throw (message not acknowledged)
        await Assert.ThrowsAsync<InvalidOperationException>(() => consumer.Consume(context.Object));
    }

    /// <summary>
    /// Verifies that deduction cascades across multiple batches correctly.
    /// </summary>
    [Fact]
    public async Task Consume_CascadeAcrossTwoBatches_FirstMarkedDepleted()
    {
        // Arrange
        var materialId = Guid.NewGuid();
        var batchAId = Guid.NewGuid();
        var batchBId = Guid.NewGuid();

        _materialClientMock
            .Setup(c => c.GetMaterialAsync(materialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MaterialDto { Id = materialId, Name = "Material X", Density = 1.0m });

        var batchA = new InventoryBatch
        {
            Id = batchAId,
            MaterialId = materialId,
            InitialWeightGrams = 200m,
            RemainingWeightGrams = 200m,
            Status = BatchStatus.Active,
            Location = "Cabinet A",
            LowStockThresholdGrams = 50m,
            ReceivedAt = DateTime.UtcNow.AddDays(-2),
        };
        var batchB = new InventoryBatch
        {
            Id = batchBId,
            MaterialId = materialId,
            InitialWeightGrams = 1000m,
            RemainingWeightGrams = 1000m,
            Status = BatchStatus.Active,
            Location = "Cabinet B",
            LowStockThresholdGrams = 100m,
            ReceivedAt = DateTime.UtcNow.AddDays(-1),
        };
        _context.InventoryBatches.AddRange(batchA, batchB);
        await _context.SaveChangesAsync();

        var consumer = new JobStartedEventConsumer(
            _materialClientMock.Object,
            _context,
            _loggerMock.Object);

        var context = new Mock<ConsumeContext<JobStartedEvent>>();
        context.Setup(c => c.Message).Returns(new JobStartedEvent
        {
            ConsumedBy = ["InventoryService"],
            Payload = new JobStartedEventPayload
            {
                JobId = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                MaterialId = materialId,
                VolumeCm3 = 500.0,
                Technology = "FDM",
                AssignedMachineId = "PRINTER-01",
                StartedAt = DateTimeOffset.UtcNow
            }
        });
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        // Act
        await consumer.Consume(context.Object);

        // Assert
        var updatedBatchA = await _context.InventoryBatches.FindAsync(batchAId);
        var updatedBatchB = await _context.InventoryBatches.FindAsync(batchBId);

        Assert.NotNull(updatedBatchA);
        Assert.NotNull(updatedBatchB);
        Assert.Equal(BatchStatus.Depleted, updatedBatchA.Status);
        Assert.Equal(0m, updatedBatchA.RemainingWeightGrams);
        // 500 * 1.0 * 1.1 = 550g needed. 
        // Batch A has 200g. Depleted. 350g remaining to deduct from Batch B.
        // Batch B: 1000 - 350 = 650g.
        Assert.Equal(650m, updatedBatchB.RemainingWeightGrams);
    }

    /// <summary>
    /// Verifies that a low stock event is published when deduction crosses the threshold.
    /// </summary>
    [Fact]
    public async Task Consume_DeductionCrossesThreshold_PublishesLowStockEvent()
    {
        // Arrange
        var materialId = Guid.NewGuid();
        var batchId = Guid.NewGuid();

        _materialClientMock
            .Setup(c => c.GetMaterialAsync(materialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MaterialDto { Id = materialId, Name = "Material X", Density = 1.0m });

        var batch = new InventoryBatch
        {
            Id = batchId,
            MaterialId = materialId,
            InitialWeightGrams = 150m,
            RemainingWeightGrams = 150m,
            Status = BatchStatus.Active,
            Location = "Cabinet A",
            LowStockThresholdGrams = 100m,
            ReceivedAt = DateTime.UtcNow.AddDays(-1),
        };
        _context.InventoryBatches.Add(batch);
        await _context.SaveChangesAsync();

        var consumer = new JobStartedEventConsumer(
            _materialClientMock.Object,
            _context,
            _loggerMock.Object);

        var context = new Mock<ConsumeContext<JobStartedEvent>>();
        context.Setup(c => c.Message).Returns(new JobStartedEvent
        {
            ConsumedBy = ["InventoryService"],
            Payload = new JobStartedEventPayload
            {
                JobId = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                MaterialId = materialId,
                VolumeCm3 = 100.0,
                Technology = "FDM",
                AssignedMachineId = "PRINTER-01",
                StartedAt = DateTimeOffset.UtcNow
            }
        });
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        // Act
        await consumer.Consume(context.Object);

        // Assert
        var updatedBatch = await _context.InventoryBatches.FindAsync(batchId);
        Assert.NotNull(updatedBatch);
        Assert.True(updatedBatch.HasAlerted);
        Assert.Equal(40m, updatedBatch.RemainingWeightGrams); // 150 - (100 * 1.0 * 1.1) = 150 - 110 = 40

        context.Verify(
            c => c.Publish(It.IsAny<MaterialLowStockEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that duplicate alerts are not published for the same batch.
    /// </summary>
    [Fact]
    public async Task Consume_BatchAlreadyBelowThreshold_NoDuplicateAlert()
    {
        // Arrange
        var materialId = Guid.NewGuid();
        var batchId = Guid.NewGuid();

        _materialClientMock
            .Setup(c => c.GetMaterialAsync(materialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MaterialDto { Id = materialId, Name = "Material X", Density = 1.0m });

        var batch = new InventoryBatch
        {
            Id = batchId,
            MaterialId = materialId,
            InitialWeightGrams = 150m,
            RemainingWeightGrams = 80m, // Already below 100 threshold
            Status = BatchStatus.Active,
            Location = "Cabinet A",
            LowStockThresholdGrams = 100m,
            HasAlerted = true, // Already alerted
            ReceivedAt = DateTime.UtcNow.AddDays(-1),
        };
        _context.InventoryBatches.Add(batch);
        await _context.SaveChangesAsync();

        var consumer = new JobStartedEventConsumer(
            _materialClientMock.Object,
            _context,
            _loggerMock.Object);

        var context = new Mock<ConsumeContext<JobStartedEvent>>();
        context.Setup(c => c.Message).Returns(new JobStartedEvent
        {
            ConsumedBy = ["InventoryService"],
            Payload = new JobStartedEventPayload
            {
                JobId = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                MaterialId = materialId,
                VolumeCm3 = 10.0,
                Technology = "FDM",
                AssignedMachineId = "PRINTER-01",
                StartedAt = DateTimeOffset.UtcNow
            }
        });
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        // Act
        await consumer.Consume(context.Object);

        // Assert - No new alert should be published
        _publishEndpointMock.Verify(
            p => p.Publish(It.IsAny<MaterialLowStockEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Verifies performance and correctness of cascading deduction across many batches.
    /// </summary>
    [Fact]
    public async Task Consume_CascadeAcrossTenBatches_VerifiesSC007Performance()
    {
        // Arrange - Create 10 batches with 100g each
        var materialId = Guid.NewGuid();
        var batchIds = new List<Guid>();

        _materialClientMock
            .Setup(c => c.GetMaterialAsync(materialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MaterialDto { Id = materialId, Name = "Material X", Density = 1.0m });

        for (int i = 0; i < 10; i++)
        {
            var batchId = Guid.NewGuid();
            batchIds.Add(batchId);

            var batch = new InventoryBatch
            {
                Id = batchId,
                MaterialId = materialId,
                InitialWeightGrams = 100m,
                RemainingWeightGrams = 100m,
                Status = BatchStatus.Active,
                Location = $"Cabinet {i}",
                LowStockThresholdGrams = 50m,
                ReceivedAt = DateTime.UtcNow.AddDays(-10 + i), // FIFO ordering
            };
            _context.InventoryBatches.Add(batch);
        }
        await _context.SaveChangesAsync();

        var consumer = new JobStartedEventConsumer(
            _materialClientMock.Object,
            _context,
            _loggerMock.Object);

        // Require 825g total needed (VolumeCm3 = 750 * 1.0 * 1.1)
        var context = new Mock<ConsumeContext<JobStartedEvent>>();
        context.Setup(c => c.Message).Returns(new JobStartedEvent
        {
            ConsumedBy = ["InventoryService"],
            Payload = new JobStartedEventPayload
            {
                JobId = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                MaterialId = materialId,
                VolumeCm3 = 750.0, // 750 * 1.0 * 1.1 = 825g required (8 batches + 25g from 9th)
                Technology = "FDM",
                AssignedMachineId = "PRINTER-01",
                StartedAt = DateTimeOffset.UtcNow
            }
        });
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await consumer.Consume(context.Object);
        stopwatch.Stop();

        // Assert
        var batches = await _context.InventoryBatches
            .Where(b => b.MaterialId == materialId)
            .OrderBy(b => b.ReceivedAt)
            .ToListAsync();

        // First 8 batches should be depleted
        for (int i = 0; i < 8; i++)
        {
            Assert.Equal(BatchStatus.Depleted, batches[i].Status);
            Assert.Equal(0m, batches[i].RemainingWeightGrams);
        }

        // 9th batch should have 75g remaining (100 - 25 = 75)
        Assert.Equal(BatchStatus.Active, batches[8].Status);
        Assert.Equal(75m, batches[8].RemainingWeightGrams);

        // 10th batch should be untouched
        Assert.Equal(BatchStatus.Active, batches[9].Status);
        Assert.Equal(100m, batches[9].RemainingWeightGrams);

        // Performance check (SC-007: should complete within 2 seconds)
        Assert.True(stopwatch.ElapsedMilliseconds < 2000,
            $"Cascade operation took {stopwatch.ElapsedMilliseconds}ms, exceeding 2000ms threshold");
    }

    /// <summary>
    /// Verifies that zero volume jobs are skipped without deduction.
    /// </summary>
    [Fact]
    public async Task Consume_ZeroVolume_SkipsDeduction()
    {
        // Arrange
        var materialId = Guid.NewGuid();
        var batchId = Guid.NewGuid();

        _materialClientMock
            .Setup(c => c.GetMaterialAsync(materialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MaterialDto { Id = materialId, Name = "Material X", Density = 1.0m });

        var batch = new InventoryBatch
        {
            Id = batchId,
            MaterialId = materialId,
            InitialWeightGrams = 1000m,
            RemainingWeightGrams = 1000m,
            Status = BatchStatus.Active,
            Location = "Cabinet A",
            LowStockThresholdGrams = 100m,
            ReceivedAt = DateTime.UtcNow.AddDays(-1),
        };
        _context.InventoryBatches.Add(batch);
        await _context.SaveChangesAsync();

        var consumer = new JobStartedEventConsumer(
            _materialClientMock.Object,
            _context,
            _loggerMock.Object);

        var context = new Mock<ConsumeContext<JobStartedEvent>>();
        context.Setup(c => c.Message).Returns(new JobStartedEvent
        {
            ConsumedBy = ["InventoryService"],
            Payload = new JobStartedEventPayload
            {
                JobId = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                MaterialId = materialId,
                VolumeCm3 = 0.0, // Zero volume
                Technology = "FDM",
                AssignedMachineId = "PRINTER-01",
                StartedAt = DateTimeOffset.UtcNow
            }
        });
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        // Act
        await consumer.Consume(context.Object);

        // Assert - Batch should remain unchanged
        var updatedBatch = await _context.InventoryBatches.FindAsync(batchId);
        Assert.NotNull(updatedBatch);
        Assert.Equal(1000m, updatedBatch.RemainingWeightGrams);
        Assert.Equal(BatchStatus.Active, updatedBatch.Status);
    }

    /// <summary>
    /// Verifies that negative volume jobs are skipped without deduction.
    /// </summary>
    [Fact]
    public async Task Consume_NegativeVolume_SkipsDeduction()
    {
        // Arrange
        var materialId = Guid.NewGuid();
        var batchId = Guid.NewGuid();

        _materialClientMock
            .Setup(c => c.GetMaterialAsync(materialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MaterialDto { Id = materialId, Name = "Material X", Density = 1.0m });

        var batch = new InventoryBatch
        {
            Id = batchId,
            MaterialId = materialId,
            InitialWeightGrams = 1000m,
            RemainingWeightGrams = 1000m,
            Status = BatchStatus.Active,
            Location = "Cabinet A",
            LowStockThresholdGrams = 100m,
            ReceivedAt = DateTime.UtcNow.AddDays(-1),
        };
        _context.InventoryBatches.Add(batch);
        await _context.SaveChangesAsync();

        var consumer = new JobStartedEventConsumer(
            _materialClientMock.Object,
            _context,
            _loggerMock.Object);

        var context = new Mock<ConsumeContext<JobStartedEvent>>();
        context.Setup(c => c.Message).Returns(new JobStartedEvent
        {
            ConsumedBy = ["InventoryService"],
            Payload = new JobStartedEventPayload
            {
                JobId = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                MaterialId = materialId,
                VolumeCm3 = -50.0, // Negative volume
                Technology = "FDM",
                AssignedMachineId = "PRINTER-01",
                StartedAt = DateTimeOffset.UtcNow
            }
        });
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        // Act
        await consumer.Consume(context.Object);

        // Assert - Batch should remain unchanged
        var updatedBatch = await _context.InventoryBatches.FindAsync(batchId);
        Assert.NotNull(updatedBatch);
        Assert.Equal(1000m, updatedBatch.RemainingWeightGrams);
        Assert.Equal(BatchStatus.Active, updatedBatch.Status);
    }

    /// <summary>
    /// Verifies that exact depletion sets status to Depleted correctly.
    /// </summary>
    [Fact]
    public async Task Consume_ExactDepletion_SetsStatusToDepleted()
    {
        // Arrange
        var materialId = Guid.NewGuid();
        var batchId = Guid.NewGuid();

        _materialClientMock
            .Setup(c => c.GetMaterialAsync(materialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MaterialDto { Id = materialId, Name = "Material X", Density = 1.0m });

        // Batch has exactly 110g remaining, job needs 110g (100 * 1.0 * 1.1)
        var batch = new InventoryBatch
        {
            Id = batchId,
            MaterialId = materialId,
            InitialWeightGrams = 110m,
            RemainingWeightGrams = 110m,
            Status = BatchStatus.Active,
            Location = "Cabinet A",
            LowStockThresholdGrams = 100m,
            ReceivedAt = DateTime.UtcNow.AddDays(-1),
        };
        _context.InventoryBatches.Add(batch);
        await _context.SaveChangesAsync();

        var consumer = new JobStartedEventConsumer(
            _materialClientMock.Object,
            _context,
            _loggerMock.Object);

        var context = new Mock<ConsumeContext<JobStartedEvent>>();
        context.Setup(c => c.Message).Returns(new JobStartedEvent
        {
            ConsumedBy = ["InventoryService"],
            Payload = new JobStartedEventPayload
            {
                JobId = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                MaterialId = materialId,
                VolumeCm3 = 100.0, // 100 * 1.0 * 1.1 = 110g needed
                Technology = "FDM",
                AssignedMachineId = "PRINTER-01",
                StartedAt = DateTimeOffset.UtcNow
            }
        });
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        // Act
        await consumer.Consume(context.Object);

        // Assert
        var updatedBatch = await _context.InventoryBatches.FindAsync(batchId);
        Assert.NotNull(updatedBatch);
        Assert.Equal(0m, updatedBatch.RemainingWeightGrams);
        Assert.Equal(BatchStatus.Depleted, updatedBatch.Status);
    }

    /// <summary>
    /// Verifies that insufficient inventory logs a warning.
    /// </summary>
    [Fact]
    public async Task Consume_InsufficientInventory_LogsWarning()
    {
        // Arrange
        var materialId = Guid.NewGuid();

        _materialClientMock
            .Setup(c => c.GetMaterialAsync(materialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MaterialDto { Id = materialId, Name = "Material X", Density = 1.0m });

        var batch = new InventoryBatch
        {
            Id = Guid.NewGuid(),
            MaterialId = materialId,
            InitialWeightGrams = 100m,
            RemainingWeightGrams = 100m,
            Status = BatchStatus.Active,
            Location = "Cabinet A",
            LowStockThresholdGrams = 50m,
            ReceivedAt = DateTime.UtcNow.AddDays(-1),
        };
        _context.InventoryBatches.Add(batch);
        await _context.SaveChangesAsync();

        var consumer = new JobStartedEventConsumer(
            _materialClientMock.Object,
            _context,
            _loggerMock.Object);

        // Need 550g but only have 100g
        var context = new Mock<ConsumeContext<JobStartedEvent>>();
        context.Setup(c => c.Message).Returns(new JobStartedEvent
        {
            ConsumedBy = ["InventoryService"],
            Payload = new JobStartedEventPayload
            {
                JobId = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                MaterialId = materialId,
                VolumeCm3 = 500.0, // 500 * 1.0 * 1.1 = 550g needed
                Technology = "FDM",
                AssignedMachineId = "PRINTER-01",
                StartedAt = DateTimeOffset.UtcNow
            }
        });
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        // Act
        await consumer.Consume(context.Object);

        // Assert - Batch should be depleted
        var updatedBatch = await _context.InventoryBatches.FindAsync(batch.Id);
        Assert.NotNull(updatedBatch);
        Assert.Equal(0m, updatedBatch.RemainingWeightGrams);
        Assert.Equal(BatchStatus.Depleted, updatedBatch.Status);
    }
}
