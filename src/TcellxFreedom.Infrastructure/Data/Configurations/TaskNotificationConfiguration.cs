using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TcellxFreedom.Domain.Entities;

namespace TcellxFreedom.Infrastructure.Data.Configurations;

public sealed class TaskNotificationConfiguration : IEntityTypeConfiguration<TaskNotification>
{
    public void Configure(EntityTypeBuilder<TaskNotification> builder)
    {
        builder.ToTable("TaskNotifications");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.UserId).HasMaxLength(450).IsRequired();
        builder.Property(n => n.NotificationTitle).HasMaxLength(200).IsRequired();
        builder.Property(n => n.NotificationBody).HasMaxLength(500).IsRequired();
        builder.Property(n => n.HangfireJobId).HasMaxLength(100);
        builder.Property(n => n.Status).HasConversion<int>();

        builder.HasIndex(n => n.UserId);
        builder.HasIndex(n => n.ScheduledAt);
        builder.HasIndex(n => n.PlanTaskId).IsUnique();
    }
}
