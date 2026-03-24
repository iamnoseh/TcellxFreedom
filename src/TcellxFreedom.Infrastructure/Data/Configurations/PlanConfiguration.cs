using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TcellxFreedom.Domain.Entities;

namespace TcellxFreedom.Infrastructure.Data.Configurations;

public sealed class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("Plans");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.UserId).HasMaxLength(450).IsRequired();
        builder.Property(p => p.Title).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(1000);
        builder.Property(p => p.Status).HasConversion<int>();
        builder.Property(p => p.AiContext).HasColumnType("text");

        builder.HasIndex(p => p.UserId);

        builder.HasMany(p => p.Tasks)
            .WithOne()
            .HasForeignKey(t => t.PlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
