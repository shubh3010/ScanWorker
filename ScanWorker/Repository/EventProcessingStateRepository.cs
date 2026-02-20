using Microsoft.EntityFrameworkCore;
using Repository;
using ScanWorker.Data.Models;
using ScanWorker.Respository;

namespace ScanWorker.Repository;

public class EventProcessingStateRepository(ScanWorkerContext context)
    : Repository<EventProcessingState>(context), IEventProcessingStateRepository
{
    public async Task<EventProcessingState> GetAsync(CancellationToken ct = default)
    {
        return await _dbSet.FirstAsync(ct);
    }
}

