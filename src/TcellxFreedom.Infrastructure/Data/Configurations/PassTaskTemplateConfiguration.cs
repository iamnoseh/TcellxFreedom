using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TcellxFreedom.Domain.Entities;

namespace TcellxFreedom.Infrastructure.Data.Configurations;

public sealed class PassTaskTemplateConfiguration : IEntityTypeConfiguration<PassTaskTemplate>
{
    public void Configure(EntityTypeBuilder<PassTaskTemplate> builder)
    {
        builder.ToTable("PassTaskTemplates");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title).HasMaxLength(300).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(1000).IsRequired();
        builder.Property(t => t.Category).HasConversion<int>();

        builder.HasIndex(t => t.DayNumber);
        builder.HasIndex(t => new { t.DayNumber, t.SortOrder });
    }
}
