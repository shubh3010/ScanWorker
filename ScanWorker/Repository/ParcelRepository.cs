using Microsoft.EntityFrameworkCore;
using Repository;
using ScanWorker.Data.Models;
using ScanWorker.Respository;

namespace ScanWorker.Repository;

public class ParcelRepository(ScanWorkerContext context) : Repository<Parcels>(context), IParcelRepository
{
    public async Task<Parcels?> GetByParcelIdAsync(int parcelId, CancellationToken ct = default)
    {
        return await _dbSet
            .Where(p => p.ParcelId == parcelId)
            .Include(p => p.User)
            .FirstOrDefaultAsync(ct);
    }
}

