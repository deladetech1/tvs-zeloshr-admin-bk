using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using ZelosHR.Api.Entities.Branches;
using ZelosHR.Api.Entities.Departments;
using ZelosHR.Api.Entities.OrgStructure;
using ZelosHR.Api.Entities.Shared;

namespace ZelosHR.Api.Configs;

/// <summary>Org chart / organisation structure OpenAPI examples and parameter hints.</summary>
public sealed class SwaggerOrgStructureOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.ApiDescription.HttpMethod ?? "";
        var path = context.ApiDescription.RelativePath ?? "";
        if (!path.StartsWith("api/v1/org-structure", StringComparison.OrdinalIgnoreCase))
            return;

        if (method.Equals("GET", StringComparison.OrdinalIgnoreCase) && path.Equals("api/v1/org-structure/chart", StringComparison.OrdinalIgnoreCase))
        {
            SetNamedJsonResponseExamples(operation, 200, new Dictionary<string, IOpenApiExample>
            {
                ["empty"] = new OpenApiExample
                {
                    Summary = "No departments",
                    Description = "New tenant or org with no department rows yet.",
                    Value = SwaggerExamples.OrgChartEmptyResponse(),
                },
                ["with_tree"] = new OpenApiExample
                {
                    Summary = "Nested department tree",
                    Description = "Roots have null parent_id; children nest under their parent department.",
                    Value = SwaggerExamples.OrgChartResponse(),
                },
            });
            operation.Summary ??= "Org chart";
            operation.Description = "Returns `{ data: { roots: [...] } }`. Each node: `id`, `name`, `node_type`, `parent_id`, `head_of_department`, `employee_count`, `children`.";
            return;
        }

        if (method.Equals("GET", StringComparison.OrdinalIgnoreCase) && path.Equals("api/v1/org-structure/statistics", StringComparison.OrdinalIgnoreCase))
        {
            SetJsonResponseExample(operation, 200, SwaggerExamples.EnvelopeFor(typeof(Respons<OrganisationSummaryDto>), 200));
            operation.Description = SwaggerOptionFormat.Append(operation.Description,
                "Tab counts for Organisation UI: department_count, branch_count, archived_count.");
            return;
        }

        if (method.Equals("GET", StringComparison.OrdinalIgnoreCase) && path.Equals("api/v1/org-structure/departments", StringComparison.OrdinalIgnoreCase))
        {
            SetJsonResponseExample(operation, 200, SwaggerExamples.EnvelopeFor(typeof(Respons<DepartmentListDto>), 200));
            AppendParameterDescription(operation, "sort_by",
                $"Sort column. Allowed: {SwaggerExampleHints.OrgDepartmentSortBy}.");
            AppendParameterDescription(operation, "sort_order",
                $"Sort direction. Allowed: {SwaggerExampleHints.OrgSortOrder}.");
            AppendParameterDescription(operation, "include_archived",
                $"Include archived departments. Allowed: {SwaggerExampleHints.OrgIncludeArchived}.");
            AppendParameterDescription(operation, "search",
                "Optional name filter (min 3 characters, case-insensitive).");
            return;
        }

        if (method.Equals("GET", StringComparison.OrdinalIgnoreCase) && path.Equals("api/v1/org-structure/branches", StringComparison.OrdinalIgnoreCase))
        {
            SetJsonResponseExample(operation, 200, SwaggerExamples.EnvelopeFor(typeof(Respons<BranchListDto>), 200));
            AppendParameterDescription(operation, "include_archived",
                $"Include archived branches. Allowed: {SwaggerExampleHints.OrgIncludeArchived}.");
            AppendParameterDescription(operation, "search",
                "Optional name filter (min 3 characters, case-insensitive).");
            return;
        }

        if (method.Equals("POST", StringComparison.OrdinalIgnoreCase) && path.Equals("api/v1/org-structure/departments/add", StringComparison.OrdinalIgnoreCase))
        {
            SetJsonResponseExample(operation, 200, SwaggerExamples.EnvelopeFor(typeof(Respons<CreateDepartmentResponseDto>), 200));
            return;
        }

        if (method.Equals("PUT", StringComparison.OrdinalIgnoreCase) && path.Equals("api/v1/org-structure/departments/update", StringComparison.OrdinalIgnoreCase))
        {
            SetJsonResponseExample(operation, 200, SwaggerExamples.EnvelopeFor(typeof(Respons<CreateDepartmentResponseDto>), 200));
            AppendParameterDescription(operation, "department_id",
                "Required. Department UUID from POST /org-structure/departments/add or GET /org-structure/departments.");
            return;
        }

        if (method.Equals("DELETE", StringComparison.OrdinalIgnoreCase) && path.Equals("api/v1/org-structure/departments/delete", StringComparison.OrdinalIgnoreCase))
        {
            SetJsonResponseExample(operation, 200, SwaggerExamples.EnvelopeFor(typeof(Respons<object>), 200));
            AppendParameterDescription(operation, "department_id", "Department UUID to archive (soft-delete).");
            return;
        }

        if (method.Equals("POST", StringComparison.OrdinalIgnoreCase) && path.Equals("api/v1/org-structure/branches/add", StringComparison.OrdinalIgnoreCase))
        {
            SetJsonResponseExample(operation, 200, SwaggerExamples.EnvelopeFor(typeof(Respons<BranchMutationResponseDto>), 200));
            return;
        }

        if (method.Equals("PUT", StringComparison.OrdinalIgnoreCase) && path.Equals("api/v1/org-structure/branches/update", StringComparison.OrdinalIgnoreCase))
        {
            SetJsonResponseExample(operation, 200, SwaggerExamples.EnvelopeFor(typeof(Respons<BranchMutationResponseDto>), 200));
            AppendParameterDescription(operation, "branch_id",
                "Required. Branch UUID from POST /org-structure/branches/add or GET /org-structure/branches.");
            return;
        }

        if (method.Equals("DELETE", StringComparison.OrdinalIgnoreCase) && path.Equals("api/v1/org-structure/branches/delete", StringComparison.OrdinalIgnoreCase))
        {
            SetJsonResponseExample(operation, 200, SwaggerExamples.EnvelopeFor(typeof(Respons<object>), 200));
            AppendParameterDescription(operation, "branch_id", "Branch UUID to archive (soft-delete).");
        }
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

    private static void SetNamedJsonResponseExamples(
        OpenApiOperation operation,
        int statusCode,
        Dictionary<string, IOpenApiExample> examples)
    {
        var key = statusCode.ToString();
        if (!operation.Responses.TryGetValue(key, out var response) || response.Content is null)
            return;
        if (!response.Content.TryGetValue("application/json", out var media))
            return;
        SwaggerMediaExamples.SetNamedExamples(media, examples);
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
