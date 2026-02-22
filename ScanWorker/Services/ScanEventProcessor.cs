using ScanWorker.Data.Models;
using ScanWorker.Dto;
using ScanWorker.Interface;
using ScanWorker.Repository;

namespace ScanWorker.Services;

public class ScanEventProcessorService(
    IScanEventClient scanEventClient,
    IEventProcessingStateRepository eventProcessingStateRepository,
    IParcelRepository parcelRepository,
    IScanEventRepository scanEventRepository,
    ILogger<ScanEventProcessorService> logger)
    : IScanEventProcessor
{
    private const int BatchSize = 100;

    public async Task<bool> ProcessBatchAsync(CancellationToken ct = default)
    {
        var lastProcessedEvent = await GetLastProcessedEventIdAsync(ct);
        
        // Fetch next batch starting from the event after the last processed one
        var events = await scanEventClient.GetScanEventsAsync(lastProcessedEvent.LastProcessedEventId + 1, BatchSize, ct);

        if (events.Count == 0)
        {
            logger.LogDebug("No new events found after EventId {LastEventId}", lastProcessedEvent.LastProcessedEventId);
            return false;
        }

        logger.LogInformation("Processing {Count} events starting from EventId {FromEventId}", events.Count, lastProcessedEvent.LastProcessedEventId + 1);

        var orderedEvents = events.OrderBy(e => e.EventId).ToList();

        // Pre-fetch existing event IDs in bulk to avoid per-event DB lookups
        var existingEventIds = await scanEventRepository.GetExistingEventIdsAsync(orderedEvents.Select(e => e.EventId).ToHashSet(), ct);

        foreach (var scanEvent in orderedEvents)
        {
            // Advance cursor even for duplicates so we don't re-fetch them next batch
            if (existingEventIds.Contains(scanEvent.EventId))
            {
                logger.LogDebug("Skipping duplicate EventId {EventId} — already processed", scanEvent.EventId);
                lastProcessedEvent.LastProcessedEventId = scanEvent.EventId;
                continue;
            }

            try
            {
                await ProcessSingleEventAsync(scanEvent, ct);
                lastProcessedEvent.LastProcessedEventId = scanEvent.EventId;
                lastProcessedEvent.UpdatedAt = DateTime.UtcNow;
                // Persist cursor after each event so progress is not lost on failure
                await eventProcessingStateRepository.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                // Stop the batch at the first failure to preserve event ordering
                logger.LogError(ex, "Failed to process EventId {EventId} — stopping batch at this point", scanEvent.EventId);
                break;
            }
        }
        
        return true;
    }

    private async Task ProcessSingleEventAsync(ScanEventResponseDto scanEvent, CancellationToken ct)
    {
        await UpsertParcelAsync(scanEvent, ct);
        AddScanEvent(scanEvent);

        logger.LogDebug("Processed EventId {EventId} for ParcelId {ParcelId}",
            scanEvent.EventId, scanEvent.ParcelId);
    }

    private async Task UpsertParcelAsync(ScanEventResponseDto scanEvent, CancellationToken ct)
    {
        var existingParcel = await parcelRepository.GetByParcelIdAsync(scanEvent.ParcelId, ct);

        if (existingParcel is null)
        {
            var parcel = new Parcel
            {
                ParcelId = scanEvent.ParcelId
                // can be extended with more parcel properties if the API provides them in future
            };

            parcelRepository.Add(parcel);
            logger.LogDebug("Created Parcel {ParcelId}", scanEvent.ParcelId);
        }
        else
        {
            existingParcel.UpdatedAt = DateTime.UtcNow;

            logger.LogDebug("Updated Parcel {ParcelId}", scanEvent.ParcelId);
        }
    }

    private void AddScanEvent(ScanEventResponseDto scanEvent)
    {
        scanEventRepository.Add(new ScanEvent
        {
            EventId = scanEvent.EventId,
            ParcelId = scanEvent.ParcelId,
            Type = scanEvent.Type,
            CreatedDateTimeUtc = scanEvent.CreatedDateTimeUtc,
            StatusCode = scanEvent.StatusCode,
            RunId = scanEvent.User.RunId,
            UserId = scanEvent.User.UserId,
            DeviceId = scanEvent.Device.DeviceId,
            DeviceTransactionId = scanEvent.Device.DeviceTransactionId
        });
    }

    private async Task<EventProcessingState> GetLastProcessedEventIdAsync(CancellationToken ct)
    {
        var state = await eventProcessingStateRepository.GetAsync(ct);
        if (state is null)
        {
            // First run — seed the cursor starting from the beginning
            state = new EventProcessingState
            {
                LastProcessedEventId = 0,
                UpdatedAt = DateTime.UtcNow
            };
            eventProcessingStateRepository.Add(state);
            await eventProcessingStateRepository.SaveChangesAsync(ct);
        }
        return state;
    }
}