using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TcellxFreedom.Domain.Entities;

namespace TcellxFreedom.Infrastructure.Data.Configurations;

public sealed class UserTcellPassConfiguration : IEntityTypeConfiguration<UserTcellPass>
{
    public void Configure(EntityTypeBuilder<UserTcellPass> builder)
    {
        builder.ToTable("UserTcellPasses");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.UserId).HasMaxLength(450).IsRequired();
        builder.Property(p => p.Tier).HasConversion<int>();

        builder.HasIndex(p => p.UserId).IsUnique();
        builder.HasIndex(p => p.TotalXp);

        builder.HasMany(p => p.DailyTasks)
            .WithOne()
            .HasForeignKey(t => t.UserTcellPassId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
