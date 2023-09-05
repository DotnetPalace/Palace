using Microsoft.EntityFrameworkCore;

namespace Palace.Server.Extensions;

public static class SqliteMigrationExtensions
{
    public static async Task StartMigration(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Services.PalaceDbContext>();
        await context.Database.MigrateAsync(); 
    }
}
