using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScanWorker.Data.Models;

namespace ScanWorker.Data.Configurations;

public class EventProcessingStateConfig: IEntityTypeConfiguration<EventProcessingState>
{
    public void Configure(EntityTypeBuilder<EventProcessingState> builder)
    {
        builder.ToTable("EventProcessingState");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.LastProcessedEventId).IsRequired();
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
    }
}