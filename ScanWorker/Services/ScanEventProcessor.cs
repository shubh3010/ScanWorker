using ScanWorker.Constants;
using ScanWorker.Data.Models;
using ScanWorker.Dtos;
using ScanWorker.Interface;
using ScanWorker.Repository;

namespace ScanWorker.Services;

public class ScanEventProcessorService(
    IScanEventClient scanEventClient,
    IEventProcessingStateRepository eventProcessingStateRepository,
    IUserRepository userRepository,
    IParcelRepository parcelRepository,
    IScanEventRepository scanEventRepository,
    ILogger<ScanEventProcessorService> logger)
    : IScanEventProcessor
{
    private const int BatchSize = 100;

    private async Task<EventProcessingState> GetLastProcessedEventIdAsync(CancellationToken ct)
    {
        var state = await eventProcessingStateRepository.GetAsync(ct);
        return state ?? new EventProcessingState();
    }

    public async Task<bool> ProcessBatchAsync(CancellationToken ct = default)
    {
        var lastProcessedEvent = await GetLastProcessedEventIdAsync(ct);
        
        var events = await scanEventClient.GetScanEventsAsync(lastProcessedEvent.LastProcessedEventId + 1, BatchSize, ct);

        if (events.Count == 0)
        {
            logger.LogDebug("No new events found after EventId {LastEventId}", lastProcessedEvent.LastProcessedEventId);
            return false;
        }

        logger.LogInformation("Processing {Count} events starting from EventId {FromEventId}",
            events.Count, lastProcessedEvent.LastProcessedEventId + 1);

        var orderedEvents = events.OrderBy(e => e.EventId).ToList();

        var existingEventIds = await scanEventRepository.GetExistingEventIdsAsync(orderedEvents.Select(e => e.EventId), ct);

        foreach (var scanEvent in orderedEvents)
        {
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
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process EventId {EventId} — stopping batch at this point", scanEvent.EventId);
                break;
            }
        }

        lastProcessedEvent.UpdatedAt = DateTime.UtcNow;
        await eventProcessingStateRepository.SaveChangesAsync(ct);
        
        return true;
    }

    private async Task ProcessSingleEventAsync(ScanEventResponseDto scanEvent, CancellationToken ct)
    {

        // Step 1: Upsert User (no FK dependency)
        await AddUserAsync(scanEvent.User, ct);

        // Step 2: Upsert Parcel (depends on User)
        await UpsertParcelAsync(scanEvent, ct);

        // Step 3: Insert ScanEvent (depends on User and Parcel)
        AddScanEvent(scanEvent, ct);

        logger.LogDebug("Processed EventId {EventId} for ParcelId {ParcelId}",
            scanEvent.EventId, scanEvent.ParcelId);
    }

    private async Task AddUserAsync(ScanEventUserDto userDto, CancellationToken ct)
    {
        var userExists = await userRepository.ExistsByUserIdAsync(userDto.UserId, ct);

        if (!userExists)
        {
            userRepository.Add(new User
            {
                UserId = userDto.UserId,
                CarrierId = userDto.CarrierId ?? string.Empty
            });
            
            logger.LogDebug("Created User {UserId}", userDto.UserId);
        }
    }

    private async Task UpsertParcelAsync(ScanEventResponseDto scanEvent, CancellationToken ct)
    {
        var existingParcel = await parcelRepository.GetByParcelIdAsync(scanEvent.ParcelId, ct);

        if (existingParcel is null)
        {
            var parcel = new Parcel
            {
                ParcelId = scanEvent.ParcelId,
                LastEventId = scanEvent.EventId,
                LastEventType = scanEvent.Type,
                LastEventStatusCode = scanEvent.StatusCode,
                LastEventCreatedDateTimeUtc = scanEvent.CreatedDateTimeUtc,
                LastRunId = scanEvent.User.RunId,
                UserId = scanEvent.User.UserId
            };

            SetParcelTimestamps(parcel, scanEvent.Type, scanEvent.CreatedDateTimeUtc);

            parcelRepository.Add(parcel);
            logger.LogDebug("Created Parcel {ParcelId}", scanEvent.ParcelId);
        }
        else
        {
            existingParcel.LastEventId = scanEvent.EventId;
            existingParcel.LastEventType = scanEvent.Type;
            existingParcel.LastEventStatusCode = scanEvent.StatusCode;
            existingParcel.LastEventCreatedDateTimeUtc = scanEvent.CreatedDateTimeUtc;
            existingParcel.LastRunId = scanEvent.User.RunId;
            existingParcel.UserId = scanEvent.User.UserId;
            existingParcel.UpdatedAt = DateTime.UtcNow;

            SetParcelTimestamps(existingParcel, scanEvent.Type, scanEvent.CreatedDateTimeUtc);

            logger.LogDebug("Updated Parcel {ParcelId}", scanEvent.ParcelId);
        }
    }

    private static void SetParcelTimestamps(Parcel parcel, string eventType, DateTime createdDateTimeUtc)
    {
        switch (eventType.ToUpperInvariant())
        {
            case EventTypeConstants.Pickup:
                parcel.PickupDateTimeUtc = createdDateTimeUtc;
                break;
            case EventTypeConstants.Delivery:
                parcel.DeliveryDateTimeUtc = createdDateTimeUtc;
                break;
        }
    }

    private void AddScanEvent(ScanEventResponseDto scanEvent, CancellationToken ct)
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
}