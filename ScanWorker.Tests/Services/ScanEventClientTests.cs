using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ScanWorker.Services;
using ScanWorker.Tests.Helpers;

namespace ScanWorker.Tests.Services;

public class ScanEventClientTests
{
    private readonly Mock<ILogger<ScanEventClient>> _loggerMock = new();
    private const string BaseUrl = "https://localhost:5001/";

    private ScanEventClient CreateClient(MockHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
        return new ScanEventClient(httpClient, _loggerMock.Object);
    }

    // Builds a minimal valid API response matching the real shape: {"ScanEvents":[...]}
    private static string SingleEventJson(long eventId = 100, int parcelId = 5002, string type = "PICKUP") => $$"""
        {
            "ScanEvents": [
                {
                    "EventId": {{eventId}},
                    "ParcelId": {{parcelId}},
                    "Type": "{{type}}",
                    "CreatedDateTimeUtc": "2026-02-21T00:00:00Z",
                    "StatusCode": "",
                    "Device": { "DeviceId": 103, "DeviceTransactionId": 83269 },
                    "User": { "UserId": "NC1001", "CarrierId": "NC", "RunId": "100" }
                }
            ]
        }
        """;

    [Fact]
    public async Task GetScanEventsAsync_ReturnsEvents_WhenApiReturnsValidResponse()
    {
        var handler = MockHttpMessageHandler.WithRawJson(SingleEventJson());
        var client = CreateClient(handler);

        var result = await client.GetScanEventsAsync(1, 100, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].EventId.Should().Be(100);
        result[0].ParcelId.Should().Be(5002);
        result[0].Type.Should().Be("PICKUP");
        result[0].User.UserId.Should().Be("NC1001");
    }

    [Fact]
    public async Task GetScanEventsAsync_MapsAllFields_Correctly()
    {
        var handler = MockHttpMessageHandler.WithRawJson(SingleEventJson());
        var client = CreateClient(handler);

        var result = await client.GetScanEventsAsync(1, 100, CancellationToken.None);

        var ev = result[0];
        ev.EventId.Should().Be(100);
        ev.ParcelId.Should().Be(5002);
        ev.Type.Should().Be("PICKUP");
        ev.Device.DeviceId.Should().Be(103);
        ev.Device.DeviceTransactionId.Should().Be(83269);
        ev.User.UserId.Should().Be("NC1001");
        ev.User.CarrierId.Should().Be("NC");
        ev.User.RunId.Should().Be("100");
    }

    [Fact]
    public async Task GetScanEventsAsync_ReturnsEmptyList_WhenApiReturnsNull()
    {
        // "null" body — GetFromJsonAsync returns null, which the client maps to empty
        var handler = MockHttpMessageHandler.WithRawJson("null");
        var client = CreateClient(handler);

        var result = await client.GetScanEventsAsync(1, 100, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetScanEventsAsync_ReturnsEmptyList_WhenScanEventsArrayIsEmpty()
    {
        var handler = MockHttpMessageHandler.WithRawJson("""{"ScanEvents":[]}""");
        var client = CreateClient(handler);

        var result = await client.GetScanEventsAsync(1, 100, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetScanEventsAsync_ThrowsHttpRequestException_WhenApiReturns500()
    {
        var handler = MockHttpMessageHandler.WithStatusCode(HttpStatusCode.InternalServerError);
        var client = CreateClient(handler);

        var act = () => client.GetScanEventsAsync(1, 100, CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetScanEventsAsync_ThrowsHttpRequestException_WhenApiReturns404()
    {
        var handler = MockHttpMessageHandler.WithStatusCode(HttpStatusCode.NotFound);
        var client = CreateClient(handler);

        var act = () => client.GetScanEventsAsync(1, 100, CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetScanEventsAsync_ThrowsJsonException_WhenApiReturnsInvalidJson()
    {
        var handler = MockHttpMessageHandler.WithInvalidJson();
        var client = CreateClient(handler);

        var act = () => client.GetScanEventsAsync(1, 100, CancellationToken.None);

        await act.Should().ThrowAsync<JsonException>();
    }

    [Fact]
    public async Task GetScanEventsAsync_SkipsMalformedEvent_AndReturnsRemainingValidOnes()
    {
        // The middle element is missing all required fields, so Deserialize<ScanEventResponseDto>
        // will throw JsonException — it should be skipped and the other two returned.
        var json = """
        {
            "ScanEvents": [
                {
                    "EventId": 100,
                    "ParcelId": 5002,
                    "Type": "PICKUP",
                    "CreatedDateTimeUtc": "2026-02-21T00:00:00Z",
                    "Device": { "DeviceId": 103, "DeviceTransactionId": 83269 },
                    "User": { "UserId": "NC1001", "CarrierId": "NC", "RunId": "100" }
                },
                { "bad_field": true },
                {
                    "EventId": 102,
                    "ParcelId": 5003,
                    "Type": "DELIVERY",
                    "CreatedDateTimeUtc": "2026-02-21T01:00:00Z",
                    "Device": { "DeviceId": 104, "DeviceTransactionId": 83270 },
                    "User": { "UserId": "NC1002", "CarrierId": "NC", "RunId": "101" }
                }
            ]
        }
        """;
        var handler = MockHttpMessageHandler.WithRawJson(json);
        var client = CreateClient(handler);

        var result = await client.GetScanEventsAsync(1, 100, CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].EventId.Should().Be(100);
        result[1].EventId.Should().Be(102);
    }

    [Fact]
    public async Task GetScanEventsAsync_ReturnsAllValidEvents_WhenMultipleEventsReturned()
    {
        var json = """
        {
            "ScanEvents": [
                {
                    "EventId": 1, "ParcelId": 10, "Type": "PICKUP",
                    "CreatedDateTimeUtc": "2026-02-21T00:00:00Z",
                    "Device": { "DeviceId": 1, "DeviceTransactionId": 1 },
                    "User": { "UserId": "U1", "CarrierId": "NC", "RunId": "1" }
                },
                {
                    "EventId": 2, "ParcelId": 10, "Type": "STATUS",
                    "CreatedDateTimeUtc": "2026-02-21T00:01:00Z",
                    "Device": { "DeviceId": 1, "DeviceTransactionId": 2 },
                    "User": { "UserId": "U1", "CarrierId": "NC", "RunId": "1" }
                },
                {
                    "EventId": 3, "ParcelId": 10, "Type": "DELIVERY",
                    "CreatedDateTimeUtc": "2026-02-21T00:02:00Z",
                    "Device": { "DeviceId": 1, "DeviceTransactionId": 3 },
                    "User": { "UserId": "U1", "CarrierId": "NC", "RunId": "1" }
                }
            ]
        }
        """;
        var handler = MockHttpMessageHandler.WithRawJson(json);
        var client = CreateClient(handler);

        var result = await client.GetScanEventsAsync(1, 100, CancellationToken.None);

        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetScanEventsAsync_ThrowsOperationCanceledException_WhenCancelled()
    {
        var handler = MockHttpMessageHandler.WithRawJson(SingleEventJson());
        var client = CreateClient(handler);
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = () => client.GetScanEventsAsync(1, 100, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}

