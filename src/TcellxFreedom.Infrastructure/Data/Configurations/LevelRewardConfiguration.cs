using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TcellxFreedom.Domain.Entities;

namespace TcellxFreedom.Infrastructure.Data.Configurations;

public sealed class LevelRewardConfiguration : IEntityTypeConfiguration<LevelReward>
{
    public void Configure(EntityTypeBuilder<LevelReward> builder)
    {
        builder.ToTable("LevelRewards");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Tier).HasConversion<int>();
        builder.Property(r => r.RewardType).HasConversion<int>();
        builder.Property(r => r.RewardDescription).HasMaxLength(200).IsRequired();

        builder.HasIndex(r => new { r.Level, r.Tier }).IsUnique();
    }
}
