using ScanWorker.Data.Models;
using ScanWorker.Respository;

namespace ScanWorker.Repository;

public interface IEventProcessingStateRepository : IRepository<EventProcessingState>
{
    Task<EventProcessingState?> GetAsync(CancellationToken ct = default);
}

