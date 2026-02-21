using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScanWorker.Constants;
using ScanWorker.Data.Models;

namespace ScanWorker.Data.Configurations;

public class ScanEventsConfig : IEntityTypeConfiguration<ScanEvent>
{
    public void Configure(EntityTypeBuilder<ScanEvent> builder)
    {
            builder.ToTable("ScanEvents");
            builder.HasKey(e => e.EventId);
            builder.Property(e => e.EventId).ValueGeneratedNever();
            
            builder.Property(e => e.Type).IsRequired().HasMaxLength(DatabaseConstants.EventTypeMaxLength);
            builder.Property(e => e.StatusCode).HasMaxLength(DatabaseConstants.StatusCodeMaxLength);
            builder.Property(e => e.UserId).HasMaxLength(DatabaseConstants.UserIdMaxLength);
            builder.Property(e => e.RunId).HasMaxLength(DatabaseConstants.RunIdMaxLength);
            builder.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            // Indexes
            builder.HasIndex(e => e.ParcelId)
                .HasDatabaseName("IX_ScanEvents_ParcelId");

            builder.HasIndex(e => e.CreatedDateTimeUtc)
                .HasDatabaseName("IX_ScanEvents_CreatedDateTimeUtc");

            builder.HasIndex(e => new { e.ParcelId, e.Type })
                .HasDatabaseName("IX_ScanEvents_ParcelId_Type");
    }
}