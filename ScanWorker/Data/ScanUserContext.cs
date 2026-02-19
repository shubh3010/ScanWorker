using Microsoft.EntityFrameworkCore;
using ScanWorker.Data.Configurations;
using ScanWorker.Data.Models;

namespace Repository;

public class ScanWorkerContext : DbContext
{
    public ScanWorkerContext(DbContextOptions<ScanWorkerContext> options) : base(options) { }
    
    public DbSet<EventProcessingState> ScanUsers => Set<EventProcessingState>();
    public DbSet<Parcels> Parcels => Set<Parcels>();
    public DbSet<ScanEvents> ScanEvents => Set<ScanEvents>();
    public DbSet<User> Users => Set<User>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new EventProcessingStateConfig());
        modelBuilder.ApplyConfiguration(new ParcelsConfig());
        modelBuilder.ApplyConfiguration(new ScanEventsConfig());
        modelBuilder.ApplyConfiguration(new UserConfig());
    }
}