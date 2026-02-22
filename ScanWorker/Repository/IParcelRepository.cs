using ScanWorker.Data.Models;

namespace ScanWorker.Repository;

public interface IParcelRepository : IRepository<Parcel>
{
    Task<Parcel?> GetByParcelIdAsync(int parcelId, CancellationToken ct = default);
}

