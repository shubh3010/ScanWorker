namespace ScanWorker.Dtos;

public record ScanEventDeviceDto
{
    public required int DeviceId { get; init; }
    public required string DeviceTransactionId { get; init; }
}