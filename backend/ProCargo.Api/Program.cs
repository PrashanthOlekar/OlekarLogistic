// ProCargo API — start-up.
// Services are registered in Configuration/ServiceRegistration.cs,
// endpoints in Endpoints/EndpointMappings.cs.

using ProCargo.Api.Configuration;
using ProCargo.Api.Data;
using ProCargo.Api.Endpoints;
using ProCargo.Api.Middleware;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddProCargoDatabase(builder.Configuration);
builder.Services.AddProCargoServices(builder.Environment);
builder.Services.AddProCargoAuthentication(builder.Configuration);
builder.Services.AddProCargoWeb(builder.Configuration);

WebApplication app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    // Test every endpoint at http://localhost:5080/swagger
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseMiddleware<ActiveAccountMiddleware>();
app.UseAuthorization();

app.MapProCargoEndpoints();

await DatabaseSeeder.EnsureAdminAsync(app);
app.Run();
