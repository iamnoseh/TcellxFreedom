using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TcellxFreedom.Domain.Entities;

namespace TcellxFreedom.Infrastructure.Data.Configurations;

public sealed class UserLevelRewardConfiguration : IEntityTypeConfiguration<UserLevelReward>
{
    public void Configure(EntityTypeBuilder<UserLevelReward> builder)
    {
        builder.ToTable("UserLevelRewards");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.UserId).HasMaxLength(450).IsRequired();
        builder.Property(r => r.Status).HasConversion<int>();

        builder.HasIndex(r => r.UserId);
        builder.HasIndex(r => new { r.UserId, r.LevelRewardId }).IsUnique();

        builder.HasOne(r => r.Reward)
            .WithMany()
            .HasForeignKey(r => r.LevelRewardId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
