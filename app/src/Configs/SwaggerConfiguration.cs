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
                    Enterprise multi-tenant HR platform API.

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

                    **Validation (400):** `detail` summarizes the problem; `field_errors` maps field paths → messages (e.g. `identity.full_name`: "Full name is required."). Use `field_errors` keys to highlight form fields.

                    **Example values:** Pipe-separated strings in Swagger (`true|false`, `200|400|500`) list allowed shapes — send **one** value per field on real API calls.

                    ---

                    ### Employee create workflow

                    1. **(Optional) Custom fields** — Admin form: `GET /api/v1/custom-fields/entity-types` → `GET /api/v1/custom-fields/sections?entity_type=` → `POST /api/v1/custom-fields/add` (use section `value` as `section_name`)  
                       Update definition: `PUT /api/v1/custom-fields/update?custom_field_id=`  
                       Employee form schema: `GET /api/v1/custom-fields/schema?entity_type=employee`
                    2. **(Optional) Documents** — `POST /api/v1/file/post/multiple` → attach returned IDs as `document_ids` (strings) on employee create/update. On read, `GET /employees/get` returns `documents[]` (MyStoreGuard `DocumentReadDto`: `doc_id`, `name`, `presigned_url`, `description`).
                    3. **Currency** — `GET /api/v1/currencies/list` → use returned `id` as `compensation.currency_id` (not a currency code string)
                    4. **Countries (leave holidays)** — `GET /api/v1/countries/list` → use returned `name` as `country` on `POST /api/v1/leave/holidays/add`
                    5. **Create** — `POST /api/v1/employees/add` (finalises automatically when `identity.work_email` is set)
                    6. **Read / update** — `GET /api/v1/employees/get?employee_id=` (`documents[]` with presigned URLs on read) · `PUT /api/v1/employees/update?employee_id=` (string `document_ids` / `delete_document_ids` on write)
                    7. **Bulk import** — `GET /api/v1/employees/bulk/template` → fill CSV → `POST /api/v1/employees/bulk?status=`
                    8. **Export** — `GET /api/v1/employees/export` (`start_date`, `end_date`, and list filters)

                    ---

                    ### Organisation / org chart workflow

                    1. **Summary tabs** — `GET /api/v1/org-structure/statistics`
                    2. **Org chart tree** — `GET /api/v1/org-structure/chart` → `data.roots[]` reporting hierarchy (`node_type`: employee; dept heads include `department` badge)
                    3. **Departments** — list `GET …/departments/list` (`sort_by`: name | employeeCount · `sort_order`: asc | desc · `include_archived`: false | true)
                       · create `POST …/departments/add` · update `PUT …/departments/update?department_id=` · delete `DELETE …/departments/delete?department_id=`
                    4. **Branches** — list `GET …/branches/list` · create `POST …/branches/add` · update `PUT …/branches/update?branch_id=` · delete `DELETE …/branches/delete?branch_id=`

                    ---

                    ### Audit logs (read-only)

                    1. **KPI cards** — `GET /api/v1/audit-logs/statistics`
                    2. **Table** — `GET /api/v1/audit-logs/list` (`search`, `action`, `severity`, `actor`, `start_date`, `end_date`)
                    3. **Export** — `GET /api/v1/audit-logs/export` (same filters as list)
                    4. **Purge preview** — `GET /api/v1/audit-logs/purge/preview?retention_window=` (90, 180, or 365 days)
                    5. **Purge** — `DELETE /api/v1/audit-logs/purge?retention_window=` (permanent delete)
                    6. **Detail** — `GET /api/v1/audit-logs/get?audit_log_id=`

                    Entries append automatically on employee create/update.

                    ---

                    ### Platform users

                    `GET /api/v1/users/get-users` — paginated Trovesuite user directory (`is_active`, `delete_status`, `can_login`, `email`, `fullname`, `gender`, `use_or`). Same data scope as Core Platform; use from ZelosHR with your existing Trove headers.

                    ---

                    ### Documented modules

                    **Employees** · **Users** · **Currencies** · **Countries** · **Custom Fields** · **File Management** · **Organisation** (org chart, departments, branches) · **Company Settings** (employment types, ID card types, company info & offices, localization) · **Lifecycle Events** · **Audit Logs** · **Leave**

                    ---

                    ### Company Settings — employment types

                    1. **List** — `GET /api/v1/employment-types/list` (Ghana defaults seed on first access)
                    2. **Add custom** — `POST /api/v1/employment-types/add` (name · description)
                    3. **Update** — `PUT /api/v1/employment-types/update?employment_type_id=` (system defaults: description only; custom: full edit)
                    4. **Delete** — `DELETE /api/v1/employment-types/delete?employment_type_id=` (custom only; blocked when assigned)
                    5. **Employee write** — pass `employment.employment_type_id` from list on POST/PUT `/employees/*`
                    6. **Employee read** — nested `employment.employment_type.id` (same UUID; flat FK omitted)

                    ---

                    ### Company Settings — ID card types

                    1. **List** — `GET /api/v1/id-card-types/list` (Ghana defaults seed on first access per org: National ID · Voter's ID · Driver's License · National Health Insurance)
                    2. **Get** — `GET /api/v1/id-card-types/get?id_card_type_id=` (single row for view/edit drawer)
                    3. **Add custom** — `POST /api/v1/id-card-types/add` (name · description; `type=custom`)
                    4. **Update** — `PUT /api/v1/id-card-types/update?id_card_type_id=` (system defaults: description + is_active only; custom: full edit including name)
                    5. **Delete** — `DELETE /api/v1/id-card-types/delete?id_card_type_id=` (custom only; system defaults return 400)

                    ---

                    ### Company Settings — company info & offices

                    One profile per org; offices are embedded — not an independent list/get/add/delete resource.

                    1. **Get** — `GET /api/v1/company/info/get` (profile with `offices[]` embedded; 404 if not created yet)
                    2. **Create** — `POST /api/v1/company/info/add` (legal_name required; optional initial `offices[]`; 400 if a profile already exists)
                    3. **Update** — `PUT /api/v1/company/info/update` (same shape as create, plus `id` — full replacement, not a partial patch; optional `offices[]` replaces the org's entire office list — diffed server-side: no `office_id` creates, a matching `office_id` replaces that office's fields in full, an office missing from the array is deleted)
                    4. **Delete** — `DELETE /api/v1/company/info/delete` (also deletes every office for the org, in one transaction)
                    5. **Logo / banner** — `logo_url` / `banner_url`: same convention as `identity.profile_url` — write accepts a document id from `POST /file/post/multiple` or the read-shaped object (`doc_id`/`id`/`presigned_url`) for round-trip from GET; reads resolve to the same embedded-document shape

                    ---

                    ### Company Settings — localization

                    One row per org — regional formats and the leave/financial year start date. Same CRUD shape as company info.

                    1. **Get** — `GET /api/v1/company/localization/get` (404 if not created yet)
                    2. **Create** — `POST /api/v1/company/localization/add` (all fields required: `time_zone` IANA id, `currency_id` from `GET /currencies/list`, `date_format`, `number_format`, `first_day_of_week`, `year_start_month`, `year_start_day`; 400 if settings already exist)
                    3. **Update** — `PUT /api/v1/company/localization/update` (same shape as create, plus `id` — full replacement, not a partial patch)
                    4. **Delete** — `DELETE /api/v1/company/localization/delete?id=` (`id` must match the settings' current id from `GET /get`)

                    `currency_id` is validated against the tenant's currencies (`GET /currencies/list`); `time_zone` is validated as a real IANA time zone id. `date_format` / `number_format` / `first_day_of_week` / `year_start_month` are free text for now (no fixed allowed-values list yet).

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
            options.SchemaFilter<SwaggerRequiredSchemaFilter>();
            options.SchemaFilter<SwaggerSchemaExamplesFilter>();
            options.ParameterFilter<SwaggerAllowedValuesParameterFilter>();
            options.ParameterFilter<SwaggerQueryParameterExamplesFilter>();
            options.OperationFilter<TroveStandardHeadersOperationFilter>();
            options.OperationFilter<StandardResponsesOperationFilter>();
            options.OperationFilter<SwaggerRequestExamplesOperationFilter>();
            options.OperationFilter<SwaggerEmployeesOperationFilter>();
            options.OperationFilter<SwaggerCurrenciesOperationFilter>();
            options.OperationFilter<SwaggerFileManagementOperationFilter>();
            options.OperationFilter<SwaggerOrgStructureOperationFilter>();
            options.OperationFilter<SwaggerLeaveOperationFilter>();
            options.OperationFilter<SwaggerEmploymentTypesOperationFilter>();
            options.OperationFilter<SwaggerIdCardTypesOperationFilter>();
            options.OperationFilter<SwaggerCompanyInfoOperationFilter>();
            options.OperationFilter<SwaggerCompanyLocalizationOperationFilter>();
            options.OperationFilter<SwaggerCountriesOperationFilter>();
            options.OperationFilter<SwaggerAuditLogsOperationFilter>();
            options.OperationFilter<SwaggerUsersOperationFilter>();
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
        "Users" => SwaggerGroups.Users,
        "Attendance" => SwaggerGroups.Attendance,
        "Leave" => SwaggerGroups.Leave,
        "Countries" => SwaggerGroups.Countries,
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
        operation.Parameters ??= [];
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
        operation.Responses ??= [];
        operation.Responses.TryAdd("400", new OpenApiResponse { Description = "Validation error or missing required Trove header (`fieldErrors` when applicable)" });
        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Missing or invalid Bearer token" });
        operation.Responses.TryAdd("404", new OpenApiResponse { Description = "Resource not found" });
        operation.Responses.TryAdd("409", new OpenApiResponse { Description = "Conflict (duplicate unique field)" });
        operation.Responses.TryAdd("500", new OpenApiResponse { Description = "Unexpected server error" });
    }
}
