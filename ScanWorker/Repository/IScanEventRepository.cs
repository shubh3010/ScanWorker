using ScanWorker.Data.Models;

namespace ScanWorker.Repository;

public interface IScanEventRepository : IRepository<ScanEvents>
{
    Task<ScanEvents?> GetByEventIdAsync(long eventId, CancellationToken ct = default);
    Task<HashSet<long>> GetExistingEventIdsAsync(IEnumerable<long> eventIds, CancellationToken ct = default);
}

