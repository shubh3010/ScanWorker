using System.Net.Http.Json;
using System.Text.Json;
using ScanWorker.Constants;
using ScanWorker.Dtos;
using ScanWorker.Interface;

namespace ScanWorker.Services;

public class ScanEventClient(HttpClient httpClient, ILogger<ScanEventClient> logger) : IScanEventClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<IReadOnlyList<ScanEventResponseDto>> GetScanEventsAsync(long fromEventId, int limit, CancellationToken ct)
    {
        try
        {
            logger.LogDebug("Fetching scan events from EventId {FromEventId} with limit {Limit}", fromEventId, limit);

            var jsonArray = await httpClient.GetFromJsonAsync<List<JsonElement>>(
                $"{ApiConstants.ScanEventsEndpoint}?FromEventId={fromEventId}&Limit={limit}", ct);

            if (jsonArray is null || jsonArray.Count == 0)
                return [];

            var events = DeserializeEvents(jsonArray);

            logger.LogInformation("Retrieved {Count}/{Total} scan events starting from EventId {FromEventId}",
                events.Count, jsonArray.Count, fromEventId);

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

    private List<ScanEventResponseDto> DeserializeEvents(List<JsonElement> jsonArray)
    {
        var events = new List<ScanEventResponseDto>();

        foreach (var element in jsonArray)
        {
            try
            {
                var scanEvent = element.Deserialize<ScanEventResponseDto>(JsonOptions);
                if (scanEvent is not null)
                    events.Add(scanEvent);
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Skipping malformed scan event: {RawJson}", element.GetRawText());
            }
        }

        return events;
    }
}