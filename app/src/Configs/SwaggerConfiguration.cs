using System.Reflection;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using ZelosHR.Api.Middleware;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Configs;

public static class SwaggerConfiguration
{
    public const string BearerScheme = "Bearer";
    public const string TenantHeader = "X-Tenant-Id";
    public const string OrgHeader = "X-Org-Id";

    public static IServiceCollection AddZelosHrSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "ZelosHR API",
                Version = "v1",
                Description = """
                    Enterprise multi-tenant HR platform API.

                    **Tenancy:** All `/api/v1/*` routes are scoped by tenant and organisation.
                    - Production: `Authorization: Bearer <JWT>` (claims `user_id`, `tenant_id`).
                    - Local dev (auth off): `X-Tenant-Id` and `X-Org-Id` headers.

                    **Envelope:** `{ success, statusCode, detail, data, pagination?, fieldErrors? }`

                    **CRUD:** List, get-by-id, create (`POST`), update (`PATCH`), delete (`DELETE`) on HR modules.
                    Audit logs are read-only.

                    Route map: `GET /api/v1/navigation` · Contracts: `docs/ENTERPRISE_API.md`
                    """,
                Contact = new OpenApiContact { Name = "Deladetech — ZelosHR" },
            });

            options.AddSecurityDefinition(BearerScheme, new OpenApiSecurityScheme
            {
                Description = "Trovesuite JWT. Example: `Bearer eyJhbGciOiJIUzI1NiIs...`",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(BearerScheme, document)] = [],
            });

            // GroupName on controllers is for Swagger UI tags only — not the OpenAPI doc id ("v1").
            options.DocInclusionPredicate((docName, _) => docName == "v1");

            options.OperationFilter<TenantHeadersOperationFilter>();
            options.OperationFilter<StandardResponsesOperationFilter>();
            options.TagActionsBy(api =>
            {
                if (api.GroupName is { Length: > 0 } group)
                    return [group];

                var controller = api.ActionDescriptor.RouteValues.TryGetValue("controller", out var c) && c is not null
                    ? c
                    : "Other";
                return [MapControllerTag(controller)];
            });
            options.OrderActionsBy(api => api.RelativePath ?? string.Empty);
            options.CustomSchemaIds(type => type.FullName?.Replace('+', '.') ?? type.Name);

            var xml = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
            if (File.Exists(xml))
                options.IncludeXmlComments(xml, includeControllerXmlComments: true);
        });

        return services;
    }

    public static WebApplication UseZelosHrSwagger(this WebApplication app)
    {
        app.UseMiddleware<SwaggerOpenApiVersionCompatMiddleware>();
        app.UseSwagger(options => options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0);
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "ZelosHR API v1");
            options.RoutePrefix = "swagger";
            options.DocumentTitle = "ZelosHR API";
            options.DisplayRequestDuration();
            options.EnablePersistAuthorization();
            options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
        });
        return app;
    }

    private static string MapControllerTag(string controller) => controller switch
    {
        "Employees" => SwaggerGroups.Employees,
        "OrgStructure" => SwaggerGroups.Organisation,
        "Departments" => SwaggerGroups.OrganisationLegacy,
        "Branches" => SwaggerGroups.OrganisationLegacy,
        "LifecycleEvents" => SwaggerGroups.LifecycleEvents,
        "AuditLogs" => SwaggerGroups.AuditLogs,
        "Attendance" => SwaggerGroups.Attendance,
        "Leave" => SwaggerGroups.Leave,
        "Recruitment" => SwaggerGroups.Recruitment,
        "Onboarding" => SwaggerGroups.Onboarding,
        "Performance" => SwaggerGroups.Performance,
        "Disciplinary" => SwaggerGroups.Disciplinary,
        "Documents" => SwaggerGroups.Documents,
        "Dashboard" => SwaggerGroups.Dashboard,
        "Navigation" => SwaggerGroups.Discovery,
        "TrovesuitePlatform" => SwaggerGroups.TrovesuitePlatform,
        "Health" => SwaggerGroups.Health,
        _ => controller,
    };
}

/// <summary>Adds tenant/org headers to every business API operation (Try it out).</summary>
public sealed class TenantHeadersOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var path = context.ApiDescription.RelativePath ?? "";
        if (!path.StartsWith("api/v1", StringComparison.OrdinalIgnoreCase))
            return;

        operation.Parameters ??= [];

        if (!operation.Parameters.Any(p => p.Name == SwaggerConfiguration.TenantHeader))
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = SwaggerConfiguration.TenantHeader,
                In = ParameterLocation.Header,
                Required = false,
                Description = $"Tenant scope. Default dev value: `{TenantContext.DefaultTenantId}`.",
                Schema = new OpenApiSchema
                {
                    Type = JsonSchemaType.String,
                    Default = JsonValue.Create(TenantContext.DefaultTenantId),
                },
            });
        }

        if (!operation.Parameters.Any(p => p.Name == SwaggerConfiguration.OrgHeader))
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = SwaggerConfiguration.OrgHeader,
                In = ParameterLocation.Header,
                Required = false,
                Description = $"Organisation scope. Default dev value: `{TenantContext.DefaultOrgId}`.",
                Schema = new OpenApiSchema
                {
                    Type = JsonSchemaType.String,
                    Default = JsonValue.Create(TenantContext.DefaultOrgId),
                },
            });
        }
    }
}

/// <summary>Documents common HTTP status codes on all operations.</summary>
public sealed class StandardResponsesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Responses.TryAdd("400", new OpenApiResponse { Description = "Validation error (`fieldErrors` populated)" });
        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Missing or invalid Bearer token (when auth required)" });
        operation.Responses.TryAdd("404", new OpenApiResponse { Description = "Resource not found" });
        operation.Responses.TryAdd("409", new OpenApiResponse { Description = "Conflict (duplicate unique field)" });
        operation.Responses.TryAdd("500", new OpenApiResponse { Description = "Unexpected server error" });
    }
}
