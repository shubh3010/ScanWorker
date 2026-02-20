using System.Net.Http.Json;
using System.Text.Json;
using ScanWorker.Constants;
using ScanWorker.Dtos;
using ScanWorker.Interface;

namespace ScanWorker.Services;

public class ScanEventClient(HttpClient httpClient, ILogger<ScanEventClient> logger) : IScanEventClient
{
    public async Task<IReadOnlyList<ScanEventResponseDto>> GetScanEventsAsync(long fromEventId, int limit, CancellationToken ct)
    {
        try
        {
            logger.LogDebug("Fetching scan events from EventId {FromEventId} with limit {Limit}", fromEventId, limit);

            var result = await httpClient.GetFromJsonAsync<List<ScanEventResponseDto>>(
                $"{ApiConstants.ScanEventsEndpoint}?FromEventId={fromEventId}&Limit={limit}", ct);

            var events = result ?? (IReadOnlyList<ScanEventResponseDto>)[];

            logger.LogInformation("Retrieved {Count} scan events starting from EventId {FromEventId}", events.Count, fromEventId);

            return events;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP request failed while fetching scan events from EventId {FromEventId}. StatusCode: {StatusCode}",
                fromEventId, ex.StatusCode);
            throw;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize scan events response from EventId {FromEventId}", fromEventId);
            throw;
        }
    }
}