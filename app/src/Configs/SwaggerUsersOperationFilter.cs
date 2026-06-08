using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Entities.Users;

namespace ZelosHR.Api.Configs;

/// <summary>Platform users OpenAPI examples and query-parameter hints.</summary>
public sealed class SwaggerUsersOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.ApiDescription.HttpMethod ?? "";
        var path = context.ApiDescription.RelativePath ?? "";
        if (!path.Equals("api/v1/users/get-users", StringComparison.OrdinalIgnoreCase))
            return;

        if (!method.Equals("GET", StringComparison.OrdinalIgnoreCase))
            return;

        SetJsonResponseExample(operation, 200, SwaggerExamples.PlatformUsersListResponse());
        operation.Summary ??= "Platform user directory";
        operation.Description = SwaggerOptionFormat.Append(operation.Description,
            "Paginated `core_platform.cp_users` joined to `cp_members` (Core Platform parity). "
            + "Filters: is_active, delete_status, can_login, email, fullname, gender, use_or. "
            + "HR-only users without a cp_members row are excluded. For import picker use GET /employees/import/search.");
        AppendParameterDescription(operation, "is_active", "Filter by cp_users.is_active.");
        AppendParameterDescription(operation, "delete_status",
            $"Exact match on delete_status. Allowed: {string.Join(", ", PlatformUserFieldOptions.DeleteStatuses)}.");
        AppendParameterDescription(operation, "can_login", "Filter by cp_users.can_login.");
        AppendParameterDescription(operation, "email", "Partial match on email (ILIKE).");
        AppendParameterDescription(operation, "fullname", "Partial match on fullname (ILIKE).");
        AppendParameterDescription(operation, "gender",
            $"Exact match on gender. Allowed: {string.Join(", ", PlatformUserFieldOptions.Genders)}.");
        AppendParameterDescription(operation, "use_or",
            "When true, optional filters are combined with OR instead of AND (tenant scope always applies).");
    }

    private static void SetJsonResponseExample(OpenApiOperation operation, int statusCode, JsonObject? example)
    {
        if (example is null)
            return;
        var key = statusCode.ToString();
        if (!operation.Responses.TryGetValue(key, out var response) || response.Content is null)
            return;
        if (!response.Content.TryGetValue("application/json", out var media))
            return;
        SwaggerMediaExamples.SetSingleExample(media, example);
    }

    private static void AppendParameterDescription(OpenApiOperation operation, string name, string addition)
    {
        if (operation.Parameters is null)
            return;
        foreach (var parameter in operation.Parameters)
        {
            if (!string.Equals(parameter.Name, name, StringComparison.OrdinalIgnoreCase))
                continue;
            parameter.Description = SwaggerOptionFormat.Append(parameter.Description, addition);
            break;
        }
    }
}
