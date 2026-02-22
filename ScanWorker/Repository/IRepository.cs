namespace ScanWorker.Repository;
public interface IRepository<T> where T : class
{
    void Update(T entity);
    T Add(T entity);
    Task SaveChangesAsync(CancellationToken ct = default);
}