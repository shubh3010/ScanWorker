namespace ScanWorker.Dto;

public record ScanEventDeviceDto
{
    public required int DeviceId { get; init; } 
    public required int DeviceTransactionId { get; init; }
}