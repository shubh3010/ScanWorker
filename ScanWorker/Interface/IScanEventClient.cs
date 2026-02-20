using ScanWorker.Dtos;
using ScanWorker.Interface;

namespace ScanWorker.Interface;

public interface IScanEventClient
{
    Task<IReadOnlyList<ScanEventResponseDto>> GetScanEventsAsync(long fromEventId, int limit, CancellationToken ct);
}