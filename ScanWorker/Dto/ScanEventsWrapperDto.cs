using System.Text.Json;

namespace ScanWorker.Dto;

public record ScanEventsWrapperDto
{
    public List<JsonElement> ScanEvents { get; init; } = [];
}
