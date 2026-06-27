using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using ZelosHR.Api.Entities.CompanyLocalization;
using ZelosHR.Api.Entities.Shared;

namespace ZelosHR.Api.Configs;

/// <summary>Company Settings — localization (regional formats + leave/financial year) OpenAPI examples.</summary>
public sealed class SwaggerCompanyLocalizationOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.ApiDescription.HttpMethod ?? "";
        var path = context.ApiDescription.RelativePath ?? "";
        if (!path.StartsWith("api/v1/company/localization", StringComparison.OrdinalIgnoreCase))
            return;

        switch (path)
        {
            case "api/v1/company/localization/get" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<CompanyLocalizationReadDto>), 404));
                operation.Summary ??= "Get localization settings";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    "One row per org — time zone, currency, regional formats, and the leave/financial year start date.");
                return;

            case "api/v1/company/localization/add" when method.Equals("POST", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 400, SwaggerExamples.EnvelopeFor(typeof(Respons<CompanyLocalizationReadDto>), 400));
                operation.Summary ??= "Create localization settings";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    """
                    All fields required: time_zone (IANA id, e.g. Africa/Accra) · currency_id (from GET /currencies/list) · date_format · number_format · first_day_of_week · year_start_month · year_start_day.
                    400 if settings already exist for this org — use PUT /update instead.
                    """);
                return;

            case "api/v1/company/localization/update" when method.Equals("PUT", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 400, SwaggerExamples.EnvelopeFor(typeof(Respons<CompanyLocalizationReadDto>), 400));
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<CompanyLocalizationReadDto>), 404));
                operation.Summary ??= "Update localization settings";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    "Same shape as POST /add, plus id (must match the settings' current id from GET /get). Full replacement, not a partial patch — all fields are required.");
                return;

            case "api/v1/company/localization/delete" when method.Equals("DELETE", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 400, SwaggerExamples.EnvelopeFor(typeof(Respons<object>), 400));
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<object>), 404));
                operation.Summary ??= "Delete localization settings";
                AppendParameterDescription(operation, "id", "Must match the settings' current id from GET /get.");
                return;
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
