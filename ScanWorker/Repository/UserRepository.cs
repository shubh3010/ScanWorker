using Microsoft.EntityFrameworkCore;
using Repository;
using ScanWorker.Data.Models;

namespace ScanWorker.Repository;

public class UserRepository(ScanWorkerContext context) : Repository<User>(context), IUserRepository
{
    public async Task<bool> ExistsByUserIdAsync(string userId, CancellationToken ct = default)
    {
        return await _dbSet.AnyAsync(u => u.UserId == userId, ct);
    }
}

