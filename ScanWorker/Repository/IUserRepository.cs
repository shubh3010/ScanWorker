using ScanWorker.Data.Models;

namespace ScanWorker.Repository;

public interface IUserRepository : IRepository<User>
{
    Task<bool> ExistsByUserIdAsync(string userId, CancellationToken ct = default);
}

