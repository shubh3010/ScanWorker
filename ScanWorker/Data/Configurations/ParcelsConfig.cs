using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScanWorker.Data.Models;

namespace ScanWorker.Data.Configurations;

public class ParcelsConfig : IEntityTypeConfiguration<Parcel>
{
    public void Configure(EntityTypeBuilder<Parcel> builder)
    {
        builder.ToTable("Parcels");
        builder.HasKey(p => p.ParcelId);
        builder.Property(p => p.ParcelId).ValueGeneratedNever();

        builder.Property(p => p.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

        // Relationships

        builder.HasMany(p => p.ScanEvents)
            .WithOne(s => s.Parcel)
            .HasForeignKey(f => f.ParcelId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}