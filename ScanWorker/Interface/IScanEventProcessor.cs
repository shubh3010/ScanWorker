
namespace ScanWorker.Interface;

public interface IScanEventProcessor
{
    /// <summary>
    /// Fetches and processes the next batch of scan events starting after the given event ID.
    /// Returns the last processed event ID, or null if no events were found.
    /// </summary>
    Task<bool> ProcessBatchAsync(CancellationToken ct);
}