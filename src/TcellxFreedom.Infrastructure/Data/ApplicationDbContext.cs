using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TcellxFreedom.Domain.Entities;
using TcellxFreedom.Infrastructure.Data.Configurations;
using TcellxFreedom.Infrastructure.Identity;

namespace TcellxFreedom.Infrastructure.Data;

public sealed class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Plan> Plans { get; set; }
    public DbSet<PlanTask> PlanTasks { get; set; }
    public DbSet<TaskNotification> TaskNotifications { get; set; }
    public DbSet<UserTaskStatistic> UserTaskStatistics { get; set; }
    public DbSet<UserTcellPass> UserTcellPasses { get; set; }
    public DbSet<PassTaskTemplate> PassTaskTemplates { get; set; }
    public DbSet<UserDailyTask> UserDailyTasks { get; set; }
    public DbSet<LevelReward> LevelRewards { get; set; }
    public DbSet<UserLevelReward> UserLevelRewards { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.Balance)
                .HasPrecision(18, 2)
                .HasDefaultValue(0);

            entity.Property(u => u.FirstName)
                .HasMaxLength(50);

            entity.Property(u => u.LastName)
                .HasMaxLength(50);

            entity.Property(u => u.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(u => u.PhoneNumber)
                .IsUnique();
        });

        builder.ApplyConfiguration(new PlanConfiguration());
        builder.ApplyConfiguration(new PlanTaskConfiguration());
        builder.ApplyConfiguration(new TaskNotificationConfiguration());
        builder.ApplyConfiguration(new UserTaskStatisticConfiguration());
        builder.ApplyConfiguration(new UserTcellPassConfiguration());
        builder.ApplyConfiguration(new PassTaskTemplateConfiguration());
        builder.ApplyConfiguration(new UserDailyTaskConfiguration());
        builder.ApplyConfiguration(new LevelRewardConfiguration());
        builder.ApplyConfiguration(new UserLevelRewardConfiguration());
    }
}
