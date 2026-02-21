using Microsoft.EntityFrameworkCore;
using ScanWorker.Data.Configurations;
using ScanWorker.Data.Models;

namespace ScanWorker.Data;

public class ScanWorkerContext(DbContextOptions<ScanWorkerContext> options) : DbContext(options)
{
    public DbSet<EventProcessingState> EventProcessingStates => Set<EventProcessingState>();
    public DbSet<Parcel> Parcels => Set<Parcel>();
    public DbSet<ScanEvent> ScanEvents => Set<ScanEvent>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new EventProcessingStateConfig());
        modelBuilder.ApplyConfiguration(new ParcelsConfig());
        modelBuilder.ApplyConfiguration(new ScanEventsConfig());
    }
}