using Microsoft.EntityFrameworkCore;
using Repository;
using ScanWorker.Data.Models;
using ScanWorker.Repository;

namespace ScanWorker.Respository;

public class ScanEventRepository(ScanWorkerContext context) : Repository<ScanEvent>(context), IScanEventRepository
{
    public async Task<ScanEvent?> GetByEventIdAsync(long eventId, CancellationToken ct = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EventId == eventId, ct);
    }

    public async Task<HashSet<long>> GetExistingEventIdsAsync(IEnumerable<long> eventIds, CancellationToken ct = default)
    {
        var ids = eventIds.ToList();
        
        var existing = await _dbSet
            .AsNoTracking()
            .Where(e => ids.Contains(e.EventId))
            .Select(e => e.EventId)
            .ToListAsync(ct);

        return existing.ToHashSet();
    }
}

