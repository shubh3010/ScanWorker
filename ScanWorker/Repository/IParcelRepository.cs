using ScanWorker.Data.Models;

namespace ScanWorker.Repository;

public interface IParcelRepository : IRepository<Parcels>
{
    Task<Parcels?> GetByParcelIdAsync(int parcelId, CancellationToken ct = default);
}

