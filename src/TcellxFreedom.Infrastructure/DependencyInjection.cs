using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TcellxFreedom.Application.Interfaces;
using TcellxFreedom.Domain.Interfaces;
using TcellxFreedom.Infrastructure.Configuration;
using TcellxFreedom.Infrastructure.Data;
using TcellxFreedom.Infrastructure.Extensions;
using TcellxFreedom.Infrastructure.Identity;
using TcellxFreedom.Infrastructure.Jobs;
using TcellxFreedom.Infrastructure.Repositories;
using TcellxFreedom.Infrastructure.Services;

namespace TcellxFreedom.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDatabase(configuration);
        services.AddIdentityServices();
        services.AddRepositories();
        services.AddApplicationServices(configuration);
        services.AddHangfireServices(configuration);
        services.AddOsonSms(configuration);
        return services;
    }

    private static void AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
    }

    private static void AddIdentityServices(this IServiceCollection services)
    {
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequiredLength = 1;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
            options.User.RequireUniqueEmail = false;
        })
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>();
    }

    private static void AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPlanRepository, PlanRepository>();
        services.AddScoped<IPlanTaskRepository, PlanTaskRepository>();
        services.AddScoped<ITaskNotificationRepository, TaskNotificationRepository>();
        services.AddScoped<IUserTaskStatisticRepository, UserTaskStatisticRepository>();
    }

    private static void AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Auth
        services.AddScoped<ISmsService, SmsService>();
        services.AddScoped<IOtpSender>(sp => sp.GetRequiredService<ISmsService>());
        services.AddScoped<IOtpVerifier>(sp => sp.GetRequiredService<ISmsService>());
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // Notifications
        services.Configure<NotificationSettings>(configuration.GetSection(NotificationSettings.SectionName));
        services.AddScoped<INotificationService, NotificationService>();

        // Gemini AI
        services.Configure<GeminiSettings>(configuration.GetSection(GeminiSettings.SectionName));
        // Retry logic is handled inside GeminiService.CallGeminiAsync (application-level).
        // Transport-level Polly retry caused 403s because StringContent streams were exhausted
        // on retry — each attempt now creates a new HttpRequestMessage + StringContent.
        services.AddHttpClient("Gemini", (sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<GeminiSettings>>().Value;
            client.BaseAddress = new Uri(settings.BaseUrl);
            // 300s covers 3 retries (15s+30s+60s delays) + up to 4 requests × ~30s each
            client.Timeout = TimeSpan.FromSeconds(300);
        });
        services.AddScoped<IGeminiService, GeminiService>();
    }

    private static void AddHangfireServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options =>
                options.UseNpgsqlConnection(configuration.GetConnectionString("DefaultConnection")!)));
        services.AddHangfireServer();
        services.AddScoped<INotificationScheduler, NotificationSchedulerService>();
        services.AddScoped<INotificationProcessor, NotificationProcessorJob>();
        services.AddScoped<RecurringTaskGeneratorJob>();
        services.AddScoped<WeeklyStatisticsCalculatorJob>();
    }
}
