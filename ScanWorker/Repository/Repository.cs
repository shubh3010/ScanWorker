using Microsoft.EntityFrameworkCore;
using Repository;

namespace ScanWorker.Repository;

public class Repository<T>(ScanWorkerContext context) : IRepository<T>
    where T : class
{
    protected readonly ScanWorkerContext _context = context;
    protected readonly DbSet<T> _dbSet = context.Set<T>();

    public virtual async Task SaveChangesAsync(CancellationToken ct = default) => 
        await _context.SaveChangesAsync(ct);
    
    public virtual void Update(T entity) => _dbSet.Update(entity);
    
    public T Add(T entity)
    {
        _dbSet.Add(entity);
        return entity;
    }
}