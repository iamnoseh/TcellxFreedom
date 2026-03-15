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
                logger.LogInformation("Мигратсияҳо бомуваффақият татбиқ шуданд");
            }
            else
            {
                logger.LogInformation("Базаи додаҳо аллакай навсозӣ шудааст. Мигратсияҳои интизоршаванда вуҷуд надоранд.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Хатогӣ ҳангоми татбиқи мигратсияҳо: {Message}", ex.Message);
            throw;
        }

        return app;
    }
}
