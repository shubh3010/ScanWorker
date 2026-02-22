using ScanWorker.Data.Models;

namespace ScanWorker.Repository;

public interface IEventProcessingStateRepository : IRepository<EventProcessingState>
{
    Task<EventProcessingState?> GetAsync(CancellationToken ct = default);
}

