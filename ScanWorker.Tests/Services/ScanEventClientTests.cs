using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ScanWorker.Dtos;
using ScanWorker.Enums;
using ScanWorker.Services;
using ScanWorker.Tests.Helpers;

namespace ScanWorker.Tests.Services;

public class ScanEventClientTests
{
    private readonly Mock<ILogger<ScanEventClient>> _loggerMock = new();
    private const string BaseUrl = "https://localhost:5001/";

    private ScanEventClient CreateClient(MockHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(BaseUrl)
        };

        return new ScanEventClient(httpClient, _loggerMock.Object);
    }

    private static List<ScanEventResponseDto> CreateSampleEvents()
    {
        return
        [
            new ScanEventResponseDto
            {
                EventId = 100,
                ParcelId = 5002,
                Type = EventType.Pickup,
                CreatedDateTimeUtc = DateTime.UtcNow,
                Device = new ScanEventDeviceDto
                {
                    DeviceId = 103,
                    DeviceTransactionId = "83269"
                },
                User = new ScanEventUserDto
                {
                    UserId = "NC1001",
                    CarrierId = "NC",
                    RunId = "100"
                }
            }
        ];
    }

    [Fact]
    public async Task GetScanEventsAsync_ReturnsEvents_WhenApiReturnsValidResponse()
    {
        // Arrange
        var expectedEvents = CreateSampleEvents();
        var handler = MockHttpMessageHandler.WithJsonResponse(expectedEvents);
        var client = CreateClient(handler);

        // Act
        var result = await client.GetScanEventsAsync(1, 100, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].EventId.Should().Be(100);
        result[0].ParcelId.Should().Be(5002);
        result[0].Type.Should().Be(EventType.Pickup);
        result[0].User.UserId.Should().Be("NC1001");
    }

    [Fact]
    public async Task GetScanEventsAsync_ReturnsEmptyList_WhenApiReturnsNull()
    {
        // Arrange
        var handler = MockHttpMessageHandler.WithJsonResponse<List<ScanEventResponseDto>?>(null);
        var client = CreateClient(handler);

        // Act
        var result = await client.GetScanEventsAsync(1, 100, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetScanEventsAsync_ReturnsEmptyList_WhenApiReturnsEmptyArray()
    {
        // Arrange
        var handler = MockHttpMessageHandler.WithJsonResponse(new List<ScanEventResponseDto>());
        var client = CreateClient(handler);

        // Act
        var result = await client.GetScanEventsAsync(1, 100, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetScanEventsAsync_ThrowsHttpRequestException_WhenApiReturns500()
    {
        // Arrange
        var handler = MockHttpMessageHandler.WithStatusCode(HttpStatusCode.InternalServerError);
        var client = CreateClient(handler);

        // Act
        var act = () => client.GetScanEventsAsync(1, 100, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetScanEventsAsync_ThrowsHttpRequestException_WhenApiReturns404()
    {
        // Arrange
        var handler = MockHttpMessageHandler.WithStatusCode(HttpStatusCode.NotFound);
        var client = CreateClient(handler);

        // Act
        var act = () => client.GetScanEventsAsync(1, 100, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetScanEventsAsync_ThrowsJsonException_WhenApiReturnsInvalidJson()
    {
        // Arrange
        var handler = MockHttpMessageHandler.WithInvalidJson();
        var client = CreateClient(handler);

        // Act
        var act = () => client.GetScanEventsAsync(1, 100, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<JsonException>();
    }

    [Fact]
    public async Task GetScanEventsAsync_ThrowsOperationCanceledException_WhenCancelled()
    {
        // Arrange
        var handler = MockHttpMessageHandler.WithJsonResponse(CreateSampleEvents());
        var client = CreateClient(handler);
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var act = () => client.GetScanEventsAsync(1, 100, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}

