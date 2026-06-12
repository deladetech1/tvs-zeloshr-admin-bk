using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ZelosHR.Api.Configs;

/// <summary>Leave management OpenAPI hints aligned with Figma modules.</summary>
public sealed class SwaggerLeaveOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var path = context.ApiDescription.RelativePath ?? "";
        if (!path.StartsWith("api/v1/leave", StringComparison.OrdinalIgnoreCase))
            return;

        if (path.Equals("api/v1/leave/my/summary", StringComparison.OrdinalIgnoreCase))
        {
            operation.Summary ??= "My Leave summary";
            operation.Description = "Employee self-service: total remaining days, pending count, approved-this-year, and per-type balances for the logged-in platform user.";
            return;
        }

        if (path.Equals("api/v1/leave/requests/list", StringComparison.OrdinalIgnoreCase))
        {
            operation.Summary ??= "Leave Management list";
            operation.Description = "Admin list of requests with embedded balances. Each request includes `remaining_days` when a balance row exists for the employee and leave type.";
            return;
        }

        if (path.Equals("api/v1/leave/types/list", StringComparison.OrdinalIgnoreCase))
        {
            operation.Summary ??= "Leave types (Settings)";
            operation.Description = "Configurable leave types per org; optional `country_code` filter for international setups.";
            return;
        }

        if (path.Equals("api/v1/leave/holidays/list", StringComparison.OrdinalIgnoreCase))
        {
            operation.Summary ??= "Public holidays";
            operation.Description = "Country-based public holidays; filter by `country_code`, `year`, and optional `branch_id`.";
            return;
        }

        if (path.Equals("api/v1/leave/requests/approve", StringComparison.OrdinalIgnoreCase))
        {
            operation.Summary ??= "Approve leave request";
            operation.Description = "Pending only. Decrements `remaining_days` on the matching balance row when one exists.";
        }
    }
}
