using JewerlySystem.Modules.Auth.Application;
using JewerlySystem.Web.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JewerlySystem.Web.Extensions;

public static class DatabaseExtensions
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<JewerlyDbContext>();
            await db.Database.EnsureCreatedAsync();

            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            await sender.Send(new GetNavigationModulesQuery());
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "Could not run initial database migration and seeding on startup.");
        }
    }
}
