
namespace ScanWorker.Interface;

public interface IScanEventProcessor
{
    /// <summary>
    /// Fetches and processes the next batch of scan events.
    /// Returns true if events were processed, false if no events were found.
    /// </summary>
    Task<bool> ProcessBatchAsync(CancellationToken ct);
}