// ProCargo API — start-up.
// Each concern is registered in its own file under Extensions/:
//   services: ApiServiceExtensions, AuthenticationExtensions, CorsExtensions, RateLimitingExtensions, OpenApiExtensions
//   pipeline: WebApplicationExtensions

using ProCargo.API.Extensions;
using ProCargo.Application;
using ProCargo.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddApiServices(builder.Configuration, builder.Environment);
builder.Services.AddProCargoAuthentication();
builder.Services.AddProCargoCors(builder.Configuration);
builder.Services.AddProCargoRateLimiting(builder.Configuration);
builder.Services.AddProCargoOpenApi();

WebApplication app = builder.Build();

app.UseProCargoPipeline();
await app.EnsureFirstAdminAsync();

app.Run();

/// <summary>Visible to the integration tests (WebApplicationFactory).</summary>
public partial class Program;
