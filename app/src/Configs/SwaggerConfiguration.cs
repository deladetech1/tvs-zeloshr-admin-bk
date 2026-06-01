using System.Reflection;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.Extensions.Options;
using ZelosHR.Api.Middleware;
using ZelosHR.Api.Shared.Constants;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Configs;

public static class SwaggerConfiguration
{
    public const string BearerScheme = AuthConstants.BearerScheme;

    public static IServiceCollection AddZelosHrSwagger(this IServiceCollection services, IConfiguration configuration)
    {
        var buildVersion = ResolveBuildVersion(configuration);

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "ZelosHR API",
                Version = $"v1 · build {buildVersion}",
                Description = $"""
                    Enterprise multi-tenant HR platform API (Mystoreguard-aligned).

                    **Deployed build:** `{buildVersion}`

                    ---

                    ### Required headers (every `/api/v1/*` request)

                    | Header | Example |
                    |--------|---------|
                    | `app-id` | `app-hr` |
                    | `authorization` | `Bearer <JWT>` |
                    | `bus-id` | `bus_…` |
                    | `loc-id` | `loc_…` |
                    | `org-id` | `org_…` |

                    Tenant scope: JWT claim `tenant_id`.

                    ---

                    ### Response envelope (snake_case JSON)

                    `success`, `status_code`, `detail`, `data`, `pagination`, `field_errors`

                    ---

                    ### File Management workflow (Azure Blob — Mystoreguard)

                    | Step | Route | What you send | What you get |
                    |------|-------|---------------|--------------|
                    | 1 Upload | `POST /file/post/multiple` | `blob_paths` + multipart `files` | an `id` per uploaded file |
                    | 2 Attach | `POST /employees/add` | `document_ids: ["id", …]` | Employee record |
                    | 3 Download | `GET /file/list` | `document_ids=id1,id2` | `presigned_url` (24h) |
                    | 4 Replace | `PUT /file/put` | `document_id` + multipart `file` | Updated metadata + URL |
                    | 5 Delete | `DELETE /file/delete` | `document_id` | `blob_path`, `container_name` |

                    **Client sends:** `blob_paths` (logical path inside container), `document_id(s)` (registry strings).  
                    **Server config (ops only):** `Trovesuite:AzureStorage:AccountName`, `AzureStorage:DocumentsContainer` — see **File Management** tag.

                    ---

                    ### Employee create workflow

                    1. **(Optional) Custom fields** — Admin defines fields: `POST /api/v1/custom-fields/add`  
                       Frontend loads schema: `GET /api/v1/custom-fields/schema?entityType=employee`  
                       Employee `section_name` values: `employee-directory-identity` | `employee-directory-employment` | `employee-directory-compensation` | `employee-directory-education` | `employee-directory-certification`
                    2. **(Optional) Documents** — Upload: `POST /api/v1/file/post/multiple?blob_paths=…`  
                       Attach returned IDs on employee: `document_ids: ["doc_…"]`  
                       Resolve URLs: `GET /api/v1/file/list?document_ids=…`
                    3. **Currency** — `GET /api/v1/currencies/list` → use returned `id` as `compensation.currency_id` (not a currency code string)
                    4. **Create** — `POST /api/v1/employees/add` with `status: draft | finalised`
                    5. **Read / update** — `GET /api/v1/employees/get?employee_id=` · `PUT /api/v1/employees/update` (full profile or partial; body includes `id`)
                    6. **Bulk import** — `GET /api/v1/employees/bulk/template` → fill CSV → `POST /api/v1/employees/bulk?status=`

                    ---

                    ### Documented modules

                    **Employees** · **Currencies** · **Custom Fields** · **File Management**

                    Conformance: `docs/MYSTOREGUARD_API_CONFORMANCE.md` · Navigation: `GET /api/v1/navigation`
                    """,
                Contact = new OpenApiContact { Name = "Deladetech — ZelosHR" },
            });

            options.AddSecurityDefinition(BearerScheme, new OpenApiSecurityScheme
            {
                Description = """
                    Trove JWT (`tenant_id`, `user_id` claims). In Development the token is prefilled on load.
                    Manual: `./scripts/gen-trovesuite-jwt.sh` or paste `Bearer eyJ...` into the `authorization` header.
                    """,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(BearerScheme, document)] = [],
            });

            options.DocInclusionPredicate((docName, apiDesc) =>
                docName == "v1" && SwaggerGroups.IsVisibleInSwagger(apiDesc.GroupName));

            options.SchemaFilter<SwaggerEnvelopeSchemaFilter>();
            options.SchemaFilter<SwaggerAllowedValuesSchemaFilter>();
            options.SchemaFilter<SwaggerSchemaExamplesFilter>();
            options.ParameterFilter<SwaggerAllowedValuesParameterFilter>();
            options.ParameterFilter<SwaggerQueryParameterExamplesFilter>();
            options.OperationFilter<TroveStandardHeadersOperationFilter>();
            options.OperationFilter<StandardResponsesOperationFilter>();
            options.OperationFilter<SwaggerRequestExamplesOperationFilter>();
            options.OperationFilter<SwaggerEmployeesOperationFilter>();
            options.OperationFilter<SwaggerCurrenciesOperationFilter>();
            options.OperationFilter<SwaggerFileManagementOperationFilter>();
            options.OperationFilter<SwaggerResponseExamplesOperationFilter>();
            options.DocumentFilter<SwaggerFileManagementTagDocumentFilter>();
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
        if (app.Environment.IsDevelopment())
            app.UseStaticFiles();

        app.UseMiddleware<SwaggerOpenApiVersionCompatMiddleware>();
        app.UseSwagger(options => options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0);
        var buildVersion = ResolveBuildVersion(app.Configuration);

        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint($"/swagger/v1/swagger.json?build={Uri.EscapeDataString(buildVersion)}", "ZelosHR API v1");
            options.RoutePrefix = "swagger";
            options.DocumentTitle = $"ZelosHR API · {buildVersion}";
            options.DisplayRequestDuration();
            options.EnablePersistAuthorization();
            options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);

            if (app.Environment.IsDevelopment())
            {
                options.HeadContent = """
                    <div style="margin:0.5rem 0;padding:0.5rem 1rem;background:#e8f4fc;border-left:4px solid #0b6efd;font-size:14px;">
                      <strong>Development:</strong> Bearer JWT and Trove headers (<code>org_*</code> / <code>bus_*</code> / <code>loc_*</code>) are prefilled — see <code>LocalDevelopment</code> in appsettings.
                      Use <strong>Authorize</strong> or expand any operation → <strong>Try it out</strong> → <strong>Execute</strong>.
                    </div>
                    """;
                options.InjectJavascript("/swagger/swagger-dev-bootstrap.js");
                options.UseRequestInterceptor(
                    """
                    (req) => {
                      const h = window.__zeloshrSwaggerHeaders;
                      if (!h) return req;
                      req.headers['app-id'] = h.appId;
                      req.headers['bus-id'] = h.busId;
                      req.headers['loc-id'] = h.locId;
                      req.headers['org-id'] = h.orgId;
                      if (h.authorization) req.headers['authorization'] = h.authorization;
                      return req;
                    }
                    """);
            }
        });
        return app;
    }

    private static string ResolveBuildVersion(IConfiguration configuration)
    {
        var fromEnv = Environment.GetEnvironmentVariable("BUILD_VERSION");
        if (!string.IsNullOrWhiteSpace(fromEnv))
            return fromEnv.Trim();

        var fromConfig = configuration[$"{AppSettings.SectionName}:AppVersion"];
        if (!string.IsNullOrWhiteSpace(fromConfig))
            return fromConfig.Trim();

        return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
    }

    /// <summary>Development-only JSON used by Swagger UI to prefill Trove headers and JWT.</summary>
    public static WebApplication MapZelosHrSwaggerDevBootstrap(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            return app;

        app.MapGet("/swagger/dev-bootstrap.json", (
            IConfiguration configuration,
            IOptions<LocalDevelopmentOptions> localDev) =>
        {
            var dev = localDev.Value;
            var token = SwaggerDevJwt.CreateToken(configuration, dev.DemoAdminUserId, dev.TenantId);

            return Results.Json(new SwaggerDevBootstrapResponse(
                TroveStandardHeaders.HrAppId,
                dev.OrgId,
                dev.BusId,
                dev.LocId,
                $"{AuthConstants.BearerPrefix}{token}",
                token,
                dev.DemoAdminUserId,
                dev.TenantId));
        }).ExcludeFromDescription();

        return app;
    }

    private sealed record SwaggerDevBootstrapResponse(
        string AppId,
        string OrgId,
        string BusId,
        string LocId,
        string Authorization,
        string BearerToken,
        string UserId,
        string TenantId);

    private static string MapControllerTag(string controller) => controller switch
    {
        "Employees" => SwaggerGroups.Employees,
        "Currencies" => SwaggerGroups.Currencies,
        "EmployeesPlatform" => SwaggerGroups.Employees,
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
        "CustomFields" => SwaggerGroups.CustomFields,
        "Dashboard" => SwaggerGroups.Dashboard,
        "Navigation" => SwaggerGroups.Discovery,
        "TrovesuitePlatform" => SwaggerGroups.TrovesuitePlatform,
        "Health" => SwaggerGroups.Health,
        _ => controller,
    };
}

/// <summary>Adds Trove standard headers to every <c>/api/v1/*</c> operation (Try it out).</summary>
public sealed class TroveStandardHeadersOperationFilter(IHostEnvironment environment) : IOperationFilter
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
        var authHint = environment.IsDevelopment()
            ? "Prefilled in Development (see Authorize and Try it out)."
            : "Bearer JWT from Trove platform login.";
        AddHeader(operation, TroveStandardHeaders.Authorization, required: true,
            authHint,
            environment.IsDevelopment()
                ? $"{AuthConstants.BearerPrefix}<loaded-on-open>"
                : $"{AuthConstants.BearerPrefix}<paste-token>");
        AddHeader(operation, TroveStandardHeaders.BusId, required: true,
            "Business scope from platform context.", LocalDevelopmentDefaults.BusId);
        AddHeader(operation, TroveStandardHeaders.LocId, required: true,
            "Location scope from platform context.", LocalDevelopmentDefaults.LocId);
        AddHeader(operation, TroveStandardHeaders.OrgId, required: true,
            "Organisation scope.", LocalDevelopmentDefaults.OrgId);
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
                Example = JsonValue.Create(defaultValue),
            },
            Example = JsonValue.Create(defaultValue),
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
