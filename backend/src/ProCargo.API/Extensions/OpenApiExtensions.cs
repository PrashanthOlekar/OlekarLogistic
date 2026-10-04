using ProCargo.API.OpenApi;

namespace ProCargo.API.Extensions;

public static class OpenApiExtensions
{
    /// <summary>The OpenAPI document at /openapi/v1.json, with JWT bearer support for Swagger UI.</summary>
    public static IServiceCollection AddProCargoOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi("v1", options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());
        return services;
    }
}
