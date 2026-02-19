using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScanWorker.Data.Models;

namespace ScanWorker.Data.Configurations;

public class UserConfig : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.UserId);
        builder.Property(u => u.CarrierId).IsRequired().HasMaxLength(100);
        builder.Property(u => u.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
    }
}