using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Olekar.Api.Data;
using Olekar.Api.Endpoints;
using Olekar.Api.Services;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// ---------- database ----------
builder.Services.AddDbContext<OlekarDbContext>(o =>
    o.UseSqlServer(config.GetConnectionString("Olekar"), sql => sql.EnableRetryOnFailure(3)));

// ---------- services ----------
builder.Services.AddScoped<SettingsService>();
builder.Services.AddScoped<OtpService>();
builder.Services.AddScoped<PricingService>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<PiiProtector>();
builder.Services.AddSingleton<FileStorage>();
builder.Services.AddDataProtection()
    .SetApplicationName("Olekar")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys")));

// ---------- auth ----------
var jwt = config.GetSection("Jwt").Get<JwtOptions>()!;
if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
    throw new InvalidOperationException("Set Jwt:Key to a random secret of at least 32 characters (user-secrets or environment variable Jwt__Key).");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = jwt.Issuer,
        ValidateAudience = true, ValidAudience = jwt.Audience,
        ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
        ValidateLifetime = true, ClockSkew = TimeSpan.FromMinutes(1),
        NameClaimType = ClaimTypes.Name, RoleClaimType = ClaimTypes.Role
    });
builder.Services.AddAuthorization(o =>
{
    o.AddPolicy(Roles.Customer, p => p.RequireRole(Roles.Customer));
    o.AddPolicy(Roles.Owner, p => p.RequireRole(Roles.Owner));
    o.AddPolicy(Roles.Driver, p => p.RequireRole(Roles.Driver));
    o.AddPolicy(Roles.Admin, p => p.RequireRole(Roles.Admin));
});

// ---------- web ----------
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(config.GetSection("Cors:Origins").Get<string[]>() ?? new[] { "http://localhost:5173" })
    .AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Olekar Logistics API", Version = "v1" });
    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT", In = ParameterLocation.Header,
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    c.AddSecurityDefinition("Bearer", scheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { scheme, Array.Empty<string>() } });
});

var app = builder.Build();

// Expected errors become { "error": "..." } with the right status code; anything else is a 500 with a safe message.
app.Use(async (ctx, next) =>
{
    try { await next(); }
    catch (ApiException ex)
    {
        ctx.Response.StatusCode = ex.Status;
        await ctx.Response.WriteAsJsonAsync(new { error = ex.Message });
    }
    catch (DbUpdateConcurrencyException)
    {
        ctx.Response.StatusCode = 409;
        await ctx.Response.WriteAsJsonAsync(new { error = "Someone else changed this just now. Refresh and try again." });
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Unhandled error on {Path}", ctx.Request.Path);
        ctx.Response.StatusCode = 500;
        await ctx.Response.WriteAsJsonAsync(new { error = "Something went wrong on our side. Please try again." });
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();

// Blocked accounts lose access immediately, even with a valid token.
app.Use(async (ctx, next) =>
{
    if (ctx.User.Identity?.IsAuthenticated == true && long.TryParse(ctx.User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid))
    {
        var db = ctx.RequestServices.GetRequiredService<OlekarDbContext>();
        var status = await db.Users.Where(u => u.UserId == uid).Select(u => u.Status).FirstOrDefaultAsync();
        if (status is null or "Blocked" or "Closed")
        {
            ctx.Response.StatusCode = 403;
            await ctx.Response.WriteAsJsonAsync(new { error = "This account is not active. Contact Olekar support." });
            return;
        }
    }
    await next();
});
app.UseAuthorization();

var api = app.MapGroup("/api");
api.MapMetaEndpoints();
api.MapAuthEndpoints();
api.MapCustomerEndpoints();
api.MapOwnerEndpoints();
api.MapDriverEndpoints();
api.MapDocumentEndpoints();
api.MapAdminEndpoints();
api.MapGet("/health", async (OlekarDbContext db) => Results.Ok(new { ok = await db.Database.CanConnectAsync() }));

await Seed.EnsureAdminAsync(app);
app.Run();
