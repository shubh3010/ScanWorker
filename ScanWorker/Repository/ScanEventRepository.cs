using Microsoft.EntityFrameworkCore;
using Repository;
using ScanWorker.Data.Models;
using ScanWorker.Repository;

namespace ScanWorker.Respository;

public class ScanEventRepository(ScanWorkerContext context) : Repository<ScanEvents>(context), IScanEventRepository
{
    public async Task<ScanEvents?> GetByEventIdAsync(long eventId, CancellationToken ct = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EventId == eventId, ct);
    }
}

