namespace ScanWorker.Dtos;

public record ScanEventResponseDto
{
    public required long EventId { get; init; }

    public required int ParcelId { get; init; }

    public required string Type { get; init; }

    public required DateTime CreatedDateTimeUtc { get; init; }

    public string? StatusCode { get; init; }

    public required ScanEventDeviceDto Device { get; init; }

    public required ScanEventUserDto User { get; init; }
}