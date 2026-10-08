using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace JoFit.API.OpenApi.Transformers;

/// <summary>
/// Adds the optional <c>language</c> query parameter to endpoints marked with <see cref="LocalizedEndpointAttribute"/>.
/// </summary>
internal sealed class LanguageParameterTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        if (!context.Description.ActionDescriptor.EndpointMetadata.OfType<LocalizedEndpointAttribute>().Any())
            return Task.CompletedTask;

        operation.Parameters ??= [];

        if (operation.Parameters.Any(p => p.In == ParameterLocation.Query && p.Name == "language"))
            return Task.CompletedTask;

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "language",
            In = ParameterLocation.Query,
            Required = false,
            Description = "Response language. Overrides the Language claim and Accept-Language header. Defaults to Arabic.",
            Schema = new OpenApiSchema
            {
                Type = "string",
                Enum = [new OpenApiString("ar"), new OpenApiString("en")],
                Default = new OpenApiString("ar")
            }
        });

        return Task.CompletedTask;
    }
}
