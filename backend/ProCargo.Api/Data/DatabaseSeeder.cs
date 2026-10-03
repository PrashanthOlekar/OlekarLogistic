using Microsoft.EntityFrameworkCore;
using ProCargo.Api.Common;
using ProCargo.Api.Data.Entities;
using ProCargo.Api.Domain;

namespace ProCargo.Api.Data;

/// <summary>
/// Runs once at start-up. Creates the first admin user if there is none yet,
/// using Seed:AdminMobile and Seed:AdminName from appsettings.json.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task EnsureAdminAsync(WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProCargoDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        try
        {
            if (!await db.Database.CanConnectAsync())
            {
                app.Logger.LogWarning(
                    "Cannot reach SQL Server. Check ConnectionStrings:ProCargo and that database/ProCargo.sql has been run.");
                return;
            }

            bool adminExists = await db.Users.AnyAsync(user => user.Role == Roles.Admin);
            if (adminExists)
            {
                return;
            }

            string mobile = MobileNumber.Normalize(configuration["Seed:AdminMobile"]);
            string name = configuration["Seed:AdminName"] ?? "ProCargo Admin";

            db.Users.Add(new User { Role = Roles.Admin, FullName = name, Mobile = mobile, Status = UserStatus.Active });
            await db.SaveChangesAsync();

            app.Logger.LogInformation("Created the first admin user, mobile {Mobile}", mobile);
        }
        catch (Exception exception)
        {
            app.Logger.LogWarning(exception, "Skipped creating the admin user.");
        }
    }
}
