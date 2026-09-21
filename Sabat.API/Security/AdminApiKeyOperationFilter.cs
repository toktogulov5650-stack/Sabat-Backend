using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Sabat.API.Security;

public sealed class AdminApiKeyOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var controllerAttributes = context.MethodInfo.DeclaringType?
            .GetCustomAttributes(inherit: true) ?? [];
        var actionAttributes = context.MethodInfo.GetCustomAttributes(inherit: true);
        var attributes = controllerAttributes.Concat(actionAttributes).ToArray();

        var authorizeAttributes = attributes.OfType<AuthorizeAttribute>().ToArray();
        if (attributes.OfType<AllowAnonymousAttribute>().Any() || authorizeAttributes.Length == 0)
        {
            return;
        }

        var apiKeyScheme = new OpenApiSecuritySchemeReference(
            AdminApiKeyOptions.SchemeName,
            context.Document);
        var sessionScheme = new OpenApiSecuritySchemeReference(
            AdminJwtOptions.SchemeName,
            context.Document);
        var policies = authorizeAttributes
            .Select(attribute => attribute.Policy)
            .Where(policy => !string.IsNullOrWhiteSpace(policy))
            .ToHashSet(StringComparer.Ordinal);

        operation.Security = policies.Contains(AdminApiKeyOptions.ApiKeyPolicyName)
            ? [new OpenApiSecurityRequirement { [apiKeyScheme] = [] }]
            : policies.Contains(AdminApiKeyOptions.SessionPolicyName)
                ? [new OpenApiSecurityRequirement { [sessionScheme] = [] }]
                :
                [
                    new OpenApiSecurityRequirement { [apiKeyScheme] = [] },
                    new OpenApiSecurityRequirement { [sessionScheme] = [] }
                ];

        var httpMethod = context.ApiDescription.HttpMethod;
        if (httpMethod is not null &&
            !HttpMethods.IsGet(httpMethod) &&
            !HttpMethods.IsHead(httpMethod) &&
            !policies.Contains(AdminApiKeyOptions.ApiKeyPolicyName))
        {
            operation.Parameters ??= [];
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = AdminJwtOptions.CsrfHeaderName,
                In = ParameterLocation.Header,
                Required = false,
                Description = "Required for state-changing requests authenticated with the administrator session cookie.",
                Schema = new OpenApiSchema { Type = JsonSchemaType.String }
            });
        }

        operation.Responses ??= [];
        operation.Responses.TryAdd("401", new OpenApiResponse
        {
            Description = "Administrator authentication is required."
        });
        operation.Responses.TryAdd("403", new OpenApiResponse
        {
            Description = "The administrative session or CSRF token is not authorized."
        });
    }
}
