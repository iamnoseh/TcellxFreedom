using Microsoft.EntityFrameworkCore;
using TcellxFreedom.Infrastructure.Data;

namespace TcellxFreedom.API.Extensions;

public static class MigrationExtensions
{
    public static async Task<IApplicationBuilder> ApplyMigrationsAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();

            if (pendingMigrations.Any())
            {
                await context.Database.MigrateAsync();
                logger.LogInformation("Миграции успешно применены");
            }
            else
            {
                logger.LogInformation("База данных уже обновлена. Ожидающих миграций не обнаружено.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при применении миграций: {Message}", ex.Message);
            throw;
        }

        return app;
    }
}
