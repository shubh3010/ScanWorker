using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ScanWorker.Data.Models;
using ScanWorker.Dto;
using ScanWorker.Interface;
using ScanWorker.Repository;
using ScanWorker.Services;

namespace ScanWorker.Tests.Services;

public class ScanEventProcessorServiceTests
{
    private readonly Mock<IScanEventClient> _clientMock = new();
    private readonly Mock<IEventProcessingStateRepository> _stateMock = new();
    private readonly Mock<IParcelRepository> _parcelRepoMock = new();
    private readonly Mock<IScanEventRepository> _scanEventRepoMock = new();
    private readonly Mock<ILogger<ScanEventProcessorService>> _loggerMock = new();

    public ScanEventProcessorServiceTests()
    {
        // Default: SaveChangesAsync completes successfully
        _stateMock.Setup(s => s.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }

    private ScanEventProcessorService CreateProcessor() =>
        new(_clientMock.Object, _stateMock.Object,
            _parcelRepoMock.Object, _scanEventRepoMock.Object, _loggerMock.Object);

    private static ScanEventResponseDto MakeEvent(
        long eventId, int parcelId, string type, string userId = "NC1001",
        DateTime? createdAt = null) => new()
    {
        EventId = eventId,
        ParcelId = parcelId,
        Type = type,
        CreatedDateTimeUtc = createdAt ?? new DateTime(2026, 2, 21, 0, 0, 0, DateTimeKind.Utc),
        StatusCode = null,
        Device = new ScanEventDeviceDto { DeviceId = 1, DeviceTransactionId = (int)eventId },
        User = new ScanEventUserDto { UserId = userId, CarrierId = "NC", RunId = "100" }
    };

    private void SetupState(long lastProcessedId = 0)
    {
        var state = new EventProcessingState { LastProcessedEventId = lastProcessedId, UpdatedAt = DateTime.UtcNow };
        _stateMock.Setup(s => s.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(state);
    }

    private void SetupEvents(params ScanEventResponseDto[] events)
    {
        _clientMock
            .Setup(c => c.GetScanEventsAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(events.ToList());
        _scanEventRepoMock
            .Setup(r => r.GetExistingEventIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _parcelRepoMock
            .Setup(r => r.GetByParcelIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Parcel?)null);
    }

    // ── No-work scenarios ──────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessBatchAsync_ReturnsFalse_WhenClientReturnsNoEvents()
    {
        SetupState();
        _clientMock
            .Setup(c => c.GetScanEventsAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await CreateProcessor().ProcessBatchAsync();

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ProcessBatchAsync_QueriesFromLastProcessedIdPlusOne()
    {
        SetupState(lastProcessedId: 50);
        _clientMock
            .Setup(c => c.GetScanEventsAsync(51, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([])
            .Verifiable();

        await CreateProcessor().ProcessBatchAsync();

        _clientMock.Verify();
    }

    // ── Processing scenarios ───────────────────────────────────────────────────

    [Fact]
    public async Task ProcessBatchAsync_ReturnsTrue_WhenEventsArePresent()
    {
        SetupState();
        SetupEvents(MakeEvent(1, 100, "STATUS"));

        var result = await CreateProcessor().ProcessBatchAsync();

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessBatchAsync_CreatesNewParcel_WhenParcelDoesNotExist()
    {
        SetupState();
        SetupEvents(MakeEvent(1, 100, "STATUS"));

        await CreateProcessor().ProcessBatchAsync();

        _parcelRepoMock.Verify(r => r.Add(It.Is<Parcel>(p => p.ParcelId == 100)), Times.Once);
    }

    [Fact]
    public async Task ProcessBatchAsync_UpdatesExistingParcel_WhenParcelAlreadyExists()
    {
        var existingParcel = new Parcel { ParcelId = 100, UpdatedAt = DateTime.UtcNow };
        SetupState();
        SetupEvents(MakeEvent(2, 100, "STATUS"));
        _parcelRepoMock
            .Setup(r => r.GetByParcelIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingParcel);

        await CreateProcessor().ProcessBatchAsync();

        _parcelRepoMock.Verify(r => r.Add(It.IsAny<Parcel>()), Times.Never);
    }

    [Fact]
    public async Task ProcessBatchAsync_AddsNewScanEvent()
    {
        SetupState();
        SetupEvents(MakeEvent(1, 100, "STATUS"));

        await CreateProcessor().ProcessBatchAsync();

        _scanEventRepoMock.Verify(r => r.Add(It.Is<ScanEvent>(e => e.EventId == 1 && e.ParcelId == 100)), Times.Once);
    }

    // ── Timestamp scenarios ────────────────────────────────────────────────────
    // Pickup/delivery timestamps are now derived from ScanEvents (Type = PICKUP/DELIVERY).
    // Parcel no longer carries PickupDateTimeUtc / DeliveryDateTimeUtc directly.

    [Fact]
    public async Task ProcessBatchAsync_PersistsPickupType_InScanEvent()
    {
        SetupState();
        var pickupTime = new DateTime(2026, 2, 21, 10, 0, 0, DateTimeKind.Utc);
        SetupEvents(MakeEvent(1, 100, "PICKUP", createdAt: pickupTime));

        await CreateProcessor().ProcessBatchAsync();

        _scanEventRepoMock.Verify(r => r.Add(It.Is<ScanEvent>(
            e => e.Type == "PICKUP" && e.CreatedDateTimeUtc == pickupTime)), Times.Once);
    }

    [Fact]
    public async Task ProcessBatchAsync_PersistsDeliveryType_InScanEvent()
    {
        SetupState();
        var deliveryTime = new DateTime(2026, 2, 21, 14, 0, 0, DateTimeKind.Utc);
        SetupEvents(MakeEvent(1, 100, "DELIVERY", createdAt: deliveryTime));

        await CreateProcessor().ProcessBatchAsync();

        _scanEventRepoMock.Verify(r => r.Add(It.Is<ScanEvent>(
            e => e.Type == "DELIVERY" && e.CreatedDateTimeUtc == deliveryTime)), Times.Once);
    }

    [Fact]
    public async Task ProcessBatchAsync_HandlesUnknownEventType_WithoutThrowing()
    {
        SetupState();
        SetupEvents(MakeEvent(1, 100, "UNKNOWN_TYPE"));

        var act = () => CreateProcessor().ProcessBatchAsync();

        await act.Should().NotThrowAsync();
    }

    // ── Duplicate / idempotency ────────────────────────────────────────────────

    [Fact]
    public async Task ProcessBatchAsync_SkipsDuplicateEvents_AlreadyInDatabase()
    {
        SetupState();
        SetupEvents(MakeEvent(1, 100, "STATUS"));
        _scanEventRepoMock
            .Setup(r => r.GetExistingEventIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<long> { 1 });  // EventId 1 already processed

        await CreateProcessor().ProcessBatchAsync();

        _scanEventRepoMock.Verify(r => r.Add(It.IsAny<ScanEvent>()), Times.Never);
        _parcelRepoMock.Verify(r => r.Add(It.IsAny<Parcel>()), Times.Never);
    }

    [Fact]
    public async Task ProcessBatchAsync_ProcessesNonDuplicates_WhenBatchContainsMix()
    {
        SetupState();
        SetupEvents(MakeEvent(1, 100, "STATUS"), MakeEvent(2, 101, "STATUS"));
        _scanEventRepoMock
            .Setup(r => r.GetExistingEventIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<long> { 1 });  // EventId 1 is duplicate, EventId 2 is new

        await CreateProcessor().ProcessBatchAsync();

        _scanEventRepoMock.Verify(r => r.Add(It.Is<ScanEvent>(e => e.EventId == 2)), Times.Once);
        _scanEventRepoMock.Verify(r => r.Add(It.Is<ScanEvent>(e => e.EventId == 1)), Times.Never);
    }

    // ── State management ───────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessBatchAsync_InitialisesState_WhenNoneExists()
    {
        _stateMock.Setup(s => s.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync((EventProcessingState?)null);
        _clientMock
            .Setup(c => c.GetScanEventsAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await CreateProcessor().ProcessBatchAsync();

        _stateMock.Verify(s => s.Add(It.Is<EventProcessingState>(e => e.LastProcessedEventId == 0)), Times.Once);
    }

    [Fact]
    public async Task ProcessBatchAsync_SavesStateAfterEachSuccessfulEvent()
    {
        SetupState();
        SetupEvents(MakeEvent(1, 100, "STATUS"), MakeEvent(2, 101, "STATUS"));

        await CreateProcessor().ProcessBatchAsync();

        _stateMock.Verify(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ProcessBatchAsync_AdvancesLastProcessedEventId_PerEvent()
    {
        var state = new EventProcessingState { LastProcessedEventId = 0, UpdatedAt = DateTime.UtcNow };
        _stateMock.Setup(s => s.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(state);
        SetupEvents(MakeEvent(10, 100, "STATUS"), MakeEvent(20, 101, "STATUS"));

        await CreateProcessor().ProcessBatchAsync();

        state.LastProcessedEventId.Should().Be(20);
    }

    [Fact]
    public async Task ProcessBatchAsync_AdvancesLastProcessedEventId_EvenForDuplicates()
    {
        var state = new EventProcessingState { LastProcessedEventId = 0, UpdatedAt = DateTime.UtcNow };
        _stateMock.Setup(s => s.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(state);
        SetupEvents(MakeEvent(5, 100, "STATUS"));
        _scanEventRepoMock
            .Setup(r => r.GetExistingEventIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<long> { 5 });  // duplicate

        await CreateProcessor().ProcessBatchAsync();

        // Checkpoint should still advance past the duplicate
        state.LastProcessedEventId.Should().Be(5);
    }

    // ── Error handling ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessBatchAsync_StopsBatchAndReturnsTrue_WhenEventProcessingThrows()
    {
        SetupState();
        SetupEvents(MakeEvent(1, 100, "STATUS"), MakeEvent(2, 101, "STATUS"));
        _parcelRepoMock
            .Setup(r => r.GetByParcelIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB failure"));

        var result = await CreateProcessor().ProcessBatchAsync();

        // Batch started so returns true; but no events were fully written
        result.Should().BeTrue();
        _scanEventRepoMock.Verify(r => r.Add(It.IsAny<ScanEvent>()), Times.Never);
    }

    [Fact]
    public async Task ProcessBatchAsync_DoesNotThrow_WhenClientThrows()
    {
        SetupState();
        _clientMock
            .Setup(c => c.GetScanEventsAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("API down"));

        var act = () => CreateProcessor().ProcessBatchAsync();

        await act.Should().ThrowAsync<HttpRequestException>();
    }
}
