using MassTransit;
using Maliev.InventoryService.Domain.Clients;
using Maliev.InventoryService.Infrastructure.Persistence;
using Maliev.InventoryService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Maliev.MessagingContracts.Contracts.Jobs;
using Maliev.MessagingContracts.Contracts.Inventory;
using Maliev.MessagingContracts;

namespace Maliev.InventoryService.Api.Consumers;

/// <summary>
/// Consumes JobStartedEvent to deduct material inventory.
/// </summary>
public class JobStartedEventConsumer : IConsumer<JobStartedEvent>
{
    private const decimal WasteBufferFactor = 1.10m;
    private readonly IMaterialServiceClient _materialClient;
    private readonly InventoryDbContext _context;
    private readonly ILogger<JobStartedEventConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobStartedEventConsumer"/> class.
    /// </summary>
    /// <param name="materialClient">The material service client.</param>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger.</param>
    public JobStartedEventConsumer(
        IMaterialServiceClient materialClient,
        InventoryDbContext context,
        ILogger<JobStartedEventConsumer> logger)
    {
        _materialClient = materialClient;
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<JobStartedEvent> context)
    {
        var message = context.Message.Payload;
        
        // T026: Handle zero/negative volume
        if (message.VolumeCm3 <= 0)
        {
            _logger.LogInformation("Skipping deduction for job {JobId}: zero or negative volume", message.JobId);
            return;
        }
        
        // Get material density from Material Service
        var material = await _materialClient.GetMaterialAsync(message.MaterialId, context.CancellationToken);
        if (material == null)
        {
            // T028: Material Service returns error - throw to not ack message
            _logger.LogError("Material {MaterialId} not found in Material Service for job {JobId}", 
                message.MaterialId, message.JobId);
            throw new InvalidOperationException($"Material {message.MaterialId} not found");
        }
        
        // T024: Calculate required grams with 10% waste buffer
        var requiredGrams = (decimal)message.VolumeCm3 * material.Density * WasteBufferFactor;
        
        // T023: Get active batches in FIFO order (with Id tiebreaker for same timestamp)
        var batches = await _context.InventoryBatches
            .Where(b => b.MaterialId == message.MaterialId && b.Status == BatchStatus.Active)
            .OrderBy(b => b.ReceivedAt)
            .ThenBy(b => b.Id)
            .ToListAsync(context.CancellationToken);
        
        // T027: Handle no active batches
        if (batches.Count == 0)
        {
            _logger.LogWarning("No active batches for material {MaterialId} - job {JobId} processed without deduction", 
                message.MaterialId, message.JobId);
            return;
        }
        
        // Execute deduction with retry for concurrency
        List<MaterialLowStockEvent> alertsToPublish = await ExecuteDeductionWithRetry(
            batches,
            requiredGrams,
            context.CancellationToken);
        
        // T041: Publish low stock alerts after successful save
        foreach (var alert in alertsToPublish)
        {
            await context.Publish(alert, context.CancellationToken);
            _logger.LogInformation("Published MaterialLowStockEvent for material {MaterialId}", message.MaterialId);
        }
    }
    
    private async Task<List<MaterialLowStockEvent>> ExecuteDeductionWithRetry(
        List<InventoryBatch> batches,
        decimal requiredGrams,
        CancellationToken cancellationToken)
    {
        const int maxRetries = 3;
        var currentRetry = 0;
        var materialId = batches[0].MaterialId;
        
        while (currentRetry < maxRetries)
        {
            try
            {
                var remainingToDeduct = requiredGrams;
                var alertsToPublish = new List<MaterialLowStockEvent>();
                
                // T031, T033: Cascade across batches
                foreach (var batch in batches)
                {
                    if (remainingToDeduct <= 0)
                        break;
                    
                    if (batch.RemainingWeightGrams >= remainingToDeduct)
                    {
                        // Batch can cover full deduction
                        batch.RemainingWeightGrams -= remainingToDeduct;
                        
                        // T025a: Handle exact depletion
                        if (batch.RemainingWeightGrams == 0)
                        {
                            batch.Status = BatchStatus.Depleted;
                            _logger.LogInformation("Batch {BatchId} depleted during job", batch.Id);
                        }
                        
                        remainingToDeduct = 0;
                    }
                    else
                    {
                        // T032: Batch insufficient, deplete and continue to next
                        remainingToDeduct -= batch.RemainingWeightGrams;
                        batch.RemainingWeightGrams = 0;
                        batch.Status = BatchStatus.Depleted;
                        _logger.LogInformation("Batch {BatchId} depleted during cascade", batch.Id);
                    }
                    
                    // T039, T040: Check if batch crossed threshold
                    if (batch.Status == BatchStatus.Active &&
                        !batch.HasAlerted &&
                        batch.RemainingWeightGrams < batch.LowStockThresholdGrams)
                    {
                        alertsToPublish.Add(new MaterialLowStockEvent(
                            MessageId: Guid.NewGuid(),
                            MessageName: nameof(MaterialLowStockEvent),
                            MessageType: MessageType.Event,
                            MessageVersion: "1.0.0",
                            PublishedBy: "inventory-service",
                            ConsumedBy: Array.Empty<string>(),
                            CorrelationId: Guid.NewGuid(),
                            CausationId: null,
                            OccurredAtUtc: DateTimeOffset.UtcNow,
                            IsPublic: false,
                            Payload: new MaterialLowStockEventPayload(
                                MaterialId: batch.MaterialId,
                                BatchId: batch.Id,
                                RemainingWeightGrams: (double)batch.RemainingWeightGrams,
                                LowStockThresholdGrams: (double)batch.LowStockThresholdGrams,
                                OccurredAt: DateTimeOffset.UtcNow)));

                        // T042: Mark as alerted
                        batch.HasAlerted = true;
                    }
                }
                
                // T037: Log warning if inventory insufficient
                if (remainingToDeduct > 0)
                {
                    _logger.LogWarning(
                        "Insufficient inventory for material - {RemainingGrams}g still required after depleting all batches", 
                        remainingToDeduct);
                }
                
                await _context.SaveChangesAsync(cancellationToken);
                return alertsToPublish;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // T035: Handle optimistic concurrency conflict
                currentRetry++;
                _logger.LogWarning(ex, "Concurrency conflict on attempt {Attempt}/{MaxRetries}", currentRetry, maxRetries);
                
                if (currentRetry >= maxRetries)
                    throw;
                
                // Clear change tracker and reload
                _context.ChangeTracker.Clear();
                
                // Exponential backoff
                await Task.Delay(TimeSpan.FromMilliseconds(100 * Math.Pow(2, currentRetry)), cancellationToken);
                
                // Reload batches
                var reloadedBatches = await _context.InventoryBatches
                    .Where(b => b.MaterialId == materialId && b.Status == BatchStatus.Active)
                    .OrderBy(b => b.ReceivedAt)
                    .ThenBy(b => b.Id)
                    .ToListAsync(cancellationToken);

                if (reloadedBatches.Count == 0)
                {
                    _logger.LogWarning("All batches for material {MaterialId} were depleted during retry, aborting deduction.", materialId);
                    return new List<MaterialLowStockEvent>();
                }
                batches = reloadedBatches;
            }
        }
        
        return new List<MaterialLowStockEvent>();
    }
}
