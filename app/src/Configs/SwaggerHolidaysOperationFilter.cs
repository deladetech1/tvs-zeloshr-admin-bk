using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using ZelosHR.Api.Entities.Leave;
using ZelosHR.Api.Entities.Shared;

namespace ZelosHR.Api.Configs;

/// <summary>Public holidays OpenAPI examples and parameter hints.</summary>
public sealed class SwaggerHolidaysOperationFilter : IOperationFilter
{
    private const string PipeExampleNote =
        "Pipe-separated values in examples list allowed shapes — send **one** value on real API calls.";

    private const string RecurringNote =
        "**Recurring holidays:** `is_recurring_annually: true` stores **one** row with an anchor month/day (`date`). " +
        "The API does **not** insert a new row each year. Leave working-day logic and list with `year=` project that anchor into the requested calendar year (`occurrence_date`).";

    private const string CountryNote =
        "`country_id` — list options via `GET /api/v1/countries/list`, then use returned `id` (same workflow as `compensation.currency_id` on employees).";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.ApiDescription.HttpMethod ?? "";
        var path = context.ApiDescription.RelativePath ?? "";
        if (!path.StartsWith("api/v1/holidays", StringComparison.OrdinalIgnoreCase))
            return;

        operation.Description = SwaggerOptionFormat.Append(operation.Description, PipeExampleNote);

        switch (path)
        {
            case "api/v1/holidays/list" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveHolidayListResponse());
                operation.Summary ??= "Public holidays";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    "Country-based holidays for leave planning. " + RecurringNote + " " + CountryNote);
                AppendParameterDescription(operation, "country_id",
                    "Filter by catalog country id from GET /countries/list (e.g. ctr_gh).");
                AppendParameterDescription(operation, "year",
                    "Calendar year filter. Includes recurring holidays from any stored year; response sets occurrence_date for that year.");
                return;

            case "api/v1/holidays/get" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveHolidayGetResponse());
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<PublicHolidayListItemDto>), 404));
                operation.Summary ??= "Get public holiday";
                operation.Description = SwaggerOptionFormat.Append(operation.Description, RecurringNote);
                AppendParameterDescription(operation, "holiday_id", "Holiday UUID.");
                return;

            case "api/v1/holidays/add" when method.Equals("POST", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveHolidayGetResponse());
                operation.Summary ??= "Create public holiday (admin)";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    "Body: holiday_name · date · is_recurring_annually · country_id. " + CountryNote + " " + RecurringNote);
                return;

            case "api/v1/holidays/update" when method.Equals("PUT", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveHolidayGetResponse());
                operation.Summary ??= "Update public holiday (admin)";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    RecurringNote + " " + CountryNote);
                AppendParameterDescription(operation, "holiday_id", "Holiday UUID.");
                return;

            case "api/v1/holidays/delete" when method.Equals("DELETE", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveDeleteHolidayResponse());
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<PublicHolidayListItemDto>), 404));
                operation.Summary ??= "Remove public holiday (admin)";
                AppendParameterDescription(operation, "holiday_id", "Soft-deactivates the holiday row.");
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
