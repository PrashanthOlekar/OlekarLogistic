using Microsoft.Extensions.Options;
using ProCargo.API.Configuration;
using ProCargo.API.ErrorHandling;
using ProCargo.API.Middleware;
using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Domain.Common;

namespace ProCargo.API.Extensions;

public static class WebApplicationExtensions
{
    /// <summary>The request pipeline, in order.</summary>
    public static WebApplication UseProCargoPipeline(this WebApplication app)
    {
        app.UseForwardedHeaders();
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseMiddleware<SecurityHeadersMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            // Swagger UI at /swagger, reading the OpenAPI document at /openapi/v1.json
            app.MapOpenApi().AllowAnonymous();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/openapi/v1.json", "ProCargo API v1");
                options.DocumentTitle = "ProCargo API";
            });
        }
        else
        {
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseCors(CorsExtensions.PolicyName);
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseMiddleware<ActiveAccountMiddleware>();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHealthChecks("/health").AllowAnonymous().DisableRateLimiting();

        // The API has no home page: in Development "/" opens Swagger, elsewhere it says where the API lives.
        if (app.Environment.IsDevelopment())
        {
            app.MapGet("/", () => Results.Redirect("/swagger")).AllowAnonymous().ExcludeFromDescription();
        }
        else
        {
            app.MapGet("/", () => Results.Ok(new { name = "ProCargo API", api = $"/{ApiRoutes.Base}", health = "/health" }))
                .AllowAnonymous()
                .ExcludeFromDescription();
        }

        // An address that matches nothing is 404, not 401: without this the "signed in" fallback policy
        // would answer every unknown address with Unauthorized.
        app.MapFallback(() => Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not found",
                detail: "There is nothing at this address. See /swagger (Development) or docs/API.md for the routes.",
                type: ProblemTypes.NotFound))
            .AllowAnonymous()
            .ExcludeFromDescription();

        return app;
    }

    /// <summary>Creates the first admin account (Seed:AdminMobile) if the database has none.</summary>
    public static async Task EnsureFirstAdminAsync(this WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();
        SeedSettings seed = scope.ServiceProvider.GetRequiredService<IOptions<SeedSettings>>().Value;

        string? mobile = MobileNumber.TryNormalize(seed.AdminMobile);
        if (mobile is null)
        {
            app.Logger.LogWarning("Seed:AdminMobile is not a valid mobile number; no admin was created.");
            return;
        }

        try
        {
            IUserRepository users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            bool created = await users.EnsureAdminAsync(mobile, seed.AdminName, CancellationToken.None);
            if (created)
            {
                app.Logger.LogInformation("Created the first admin user, mobile ending {MobileEnd}", mobile[^4..]);
            }
        }
        catch (Exception exception)
        {
            app.Logger.LogWarning(exception, "Could not check for an admin user. Is SQL Server running and the database installed?");
        }
    }
}
