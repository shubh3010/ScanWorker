using Microsoft.EntityFrameworkCore;
using Repository;
using ScanWorker.Data.Models;

namespace ScanWorker.Repository;

public class ParcelRepository(ScanWorkerContext context) : Repository<Parcel>(context), IParcelRepository
{
    public async Task<Parcel?> GetByParcelIdAsync(int parcelId, CancellationToken ct = default)
    {
        return await _dbSet
            .Where(p => p.ParcelId == parcelId)
            .Include(p => p.User)
            .FirstOrDefaultAsync(ct);
    }
}

