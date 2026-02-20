using ScanWorker.Enums;

namespace ScanWorker.Dtos;

public record ScanEventResponseDto
{
    public required long EventId { get; init; }
    
    public required int ParcelId { get; init; }
    
    public required EventType Type { get; init; } // need to take care of this
    
    public required DateTime CreatedDateTimeUtc { get; init; }
    
    public StatusCode StatusCode { get; init; }
    
    public required ScanEventDeviceDto Device { get; init; }
    
    public required ScanEventUserDto User { get; init; }
}