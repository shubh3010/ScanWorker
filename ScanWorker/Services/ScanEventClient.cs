using System.Net.Http.Json;
using System.Text.Json;
using ScanWorker.Constants;
using ScanWorker.Dtos;
using ScanWorker.Interface;

namespace ScanWorker.Services;

public class ScanEventClient : IScanEventClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ScanEventClient> _logger;

    public ScanEventClient(HttpClient httpClient, ILogger<ScanEventClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ScanEventResponseDto>> GetScanEventsAsync(long fromEventId, int limit, CancellationToken ct)
    {
        try
        {
            _logger.LogDebug("Fetching scan events from EventId {FromEventId} with limit {Limit}", fromEventId, limit);

            var result = await _httpClient.GetFromJsonAsync<List<ScanEventResponseDto>>(
                $"{ApiConstants.ScanEventsEndpoint}?FromEventId={fromEventId}&Limit={limit}", ct);

            var events = result ?? (IReadOnlyList<ScanEventResponseDto>)[];

            _logger.LogInformation("Retrieved {Count} scan events starting from EventId {FromEventId}", events.Count, fromEventId);

            return events;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed while fetching scan events from EventId {FromEventId}. StatusCode: {StatusCode}",
                fromEventId, ex.StatusCode);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize scan events response from EventId {FromEventId}", fromEventId);
            throw;
        }
    }
}