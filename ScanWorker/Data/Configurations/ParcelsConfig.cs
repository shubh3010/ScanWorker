using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScanWorker.Constants;
using ScanWorker.Data.Models;

namespace ScanWorker.Data.Configurations;

public class ParcelsConfig : IEntityTypeConfiguration<Parcel>
{
    public void Configure(EntityTypeBuilder<Parcel> builder)
    {
        builder.ToTable("Parcels");
        builder.HasKey(p => p.ParcelId);

        builder.Property(p => p.UserId).IsRequired().HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(p => p.LastRunId).HasMaxLength(DatabaseConstants.RunIdMaxLength);
        builder.Property(p => p.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

        // Relationships
        builder.HasOne(p => p.User)
            .WithMany(u => u.Parcels)
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(p => p.ScanEvents)
            .WithOne(s => s.Parcel)
            .HasForeignKey(f => f.ParcelId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}