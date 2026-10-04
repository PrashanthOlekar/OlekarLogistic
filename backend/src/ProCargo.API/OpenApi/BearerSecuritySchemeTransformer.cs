using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ProCargo.API.OpenApi;

/// <summary>
/// Adds JWT bearer authentication to the OpenAPI document, which gives Swagger UI its Authorize button.
/// Sign in with POST /api/v1/auth/login, then paste the accessToken there.
/// </summary>
internal sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public const string SchemeName = "Bearer";

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Info = new OpenApiInfo
        {
            Title = "ProCargo API",
            Version = "v1",
            Description = "Lorry booking marketplace: customers, lorry owners, drivers and operations. "
                + "Sign in with an SMS code (POST /api/v1/auth/otp, then /api/v1/auth/login), then press Authorize and paste the accessToken.",
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Paste the accessToken from POST /api/v1/auth/login.",
        };

        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(SchemeName, document)] = [],
        });

        return Task.CompletedTask;
    }
}
