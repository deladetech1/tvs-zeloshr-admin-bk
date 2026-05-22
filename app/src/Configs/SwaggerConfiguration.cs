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

                    **Required headers** on every `/api/v1/*` request (exact names, lowercase):

                    | Header | Example |
                    |--------|---------|
                    | `app-id` | `app-hr` |
                    | `authorization` | `Bearer <JWT>` |
                    | `bus-id` | `bus_…` |
                    | `loc-id` | `loc_…` |
                    | `org-id` | `org_…` |

                    Tenant scope comes from the JWT claim `tenant_id` (or legacy `X-Tenant-Id` when header enforcement is off).

                    **Envelope:** `{ success, statusCode, detail, data, pagination?, fieldErrors? }`

                    Route map: `GET /api/v1/navigation` · Contracts: `docs/ENTERPRISE_API.md`
                    """,
                Contact = new OpenApiContact { Name = "Deladetech — ZelosHR" },
            });

            options.AddSecurityDefinition(BearerScheme, new OpenApiSecurityScheme
            {
                Description = "Trove JWT in the `authorization` header. Example: `Bearer eyJhbGciOiJIUzI1NiIs...`",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(BearerScheme, document)] = [],
            });

            options.DocInclusionPredicate((docName, _) => docName == "v1");

            options.OperationFilter<TroveStandardHeadersOperationFilter>();
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

/// <summary>Adds Trove standard headers to every <c>/api/v1/*</c> operation (Try it out).</summary>
public sealed class TroveStandardHeadersOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var path = context.ApiDescription.RelativePath ?? "";
        if (!path.StartsWith("api/v1", StringComparison.OrdinalIgnoreCase))
            return;
        if (path.StartsWith("api/v1/health", StringComparison.OrdinalIgnoreCase))
            return;

        operation.Parameters ??= [];

        AddHeader(operation, TroveStandardHeaders.AppId, required: true,
            $"Must be `{TroveStandardHeaders.HrAppId}`.", TroveStandardHeaders.HrAppId);
        AddHeader(operation, TroveStandardHeaders.Authorization, required: true,
            "Bearer JWT from Trove platform login.", "Bearer <paste-token>");
        AddHeader(operation, TroveStandardHeaders.BusId, required: true,
            "Business scope from platform context.", TenantContext.DefaultBusId);
        AddHeader(operation, TroveStandardHeaders.LocId, required: true,
            "Location scope from platform context.", TenantContext.DefaultLocId);
        AddHeader(operation, TroveStandardHeaders.OrgId, required: true,
            "Organisation scope.", TenantContext.DefaultOrgId);
    }

    private static void AddHeader(
        OpenApiOperation operation,
        string name,
        bool required,
        string description,
        string defaultValue)
    {
        if (operation.Parameters.Any(p =>
                string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
            return;

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = name,
            In = ParameterLocation.Header,
            Required = required,
            Description = description,
            Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Default = JsonValue.Create(defaultValue),
            },
        });
    }
}

/// <summary>Documents common HTTP status codes on all operations.</summary>
public sealed class StandardResponsesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Responses.TryAdd("400", new OpenApiResponse { Description = "Validation error or missing required Trove header (`fieldErrors` when applicable)" });
        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Missing or invalid Bearer token" });
        operation.Responses.TryAdd("404", new OpenApiResponse { Description = "Resource not found" });
        operation.Responses.TryAdd("409", new OpenApiResponse { Description = "Conflict (duplicate unique field)" });
        operation.Responses.TryAdd("500", new OpenApiResponse { Description = "Unexpected server error" });
    }
}
