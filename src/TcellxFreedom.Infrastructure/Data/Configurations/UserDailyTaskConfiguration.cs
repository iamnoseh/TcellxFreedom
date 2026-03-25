using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TcellxFreedom.Domain.Entities;

namespace TcellxFreedom.Infrastructure.Data.Configurations;

public sealed class UserDailyTaskConfiguration : IEntityTypeConfiguration<UserDailyTask>
{
    public void Configure(EntityTypeBuilder<UserDailyTask> builder)
    {
        builder.ToTable("UserDailyTasks");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.UserId).HasMaxLength(450).IsRequired();
        builder.Property(t => t.Status).HasConversion<int>();
        builder.Property(t => t.AssignedDate).HasColumnType("date");

        builder.HasIndex(t => new { t.UserId, t.AssignedDate });
        builder.HasIndex(t => new { t.AssignedDate, t.Status });

        builder.HasOne(t => t.Template)
            .WithMany()
            .HasForeignKey(t => t.PassTaskTemplateId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
