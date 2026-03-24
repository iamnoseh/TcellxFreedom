using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TcellxFreedom.Domain.Entities;

namespace TcellxFreedom.Infrastructure.Data.Configurations;

public sealed class PlanTaskConfiguration : IEntityTypeConfiguration<PlanTask>
{
    public void Configure(EntityTypeBuilder<PlanTask> builder)
    {
        builder.ToTable("PlanTasks");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title).HasMaxLength(300).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(1000);
        builder.Property(t => t.AiRationale).HasMaxLength(500);
        builder.Property(t => t.Status).HasConversion<int>();
        builder.Property(t => t.Recurrence).HasConversion<int>();

        builder.HasIndex(t => t.PlanId);
        builder.HasIndex(t => new { t.PlanId, t.ScheduledAt });
    }
}
