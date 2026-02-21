using Microsoft.EntityFrameworkCore;
using ScanWorker.Data;
using ScanWorker.Data.Models;

namespace ScanWorker.Repository;

public class EventProcessingStateRepository(ScanWorkerContext context)
    : Repository<EventProcessingState>(context), IEventProcessingStateRepository
{
    public async Task<EventProcessingState?> GetAsync(CancellationToken ct = default)
    {
        return await _dbSet.SingleOrDefaultAsync(ct);
    }
}

