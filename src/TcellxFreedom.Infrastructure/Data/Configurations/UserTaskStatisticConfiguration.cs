using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TcellxFreedom.Domain.Entities;

namespace TcellxFreedom.Infrastructure.Data.Configurations;

public sealed class UserTaskStatisticConfiguration : IEntityTypeConfiguration<UserTaskStatistic>
{
    public void Configure(EntityTypeBuilder<UserTaskStatistic> builder)
    {
        builder.ToTable("UserTaskStatistics");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.UserId).HasMaxLength(450).IsRequired();
        builder.Property(s => s.CompletionRate).HasPrecision(5, 2);
        builder.Property(s => s.AiImprovementSuggestions).HasColumnType("text");

        builder.HasIndex(s => new { s.UserId, s.WeekStartDate }).IsUnique();
    }
}
