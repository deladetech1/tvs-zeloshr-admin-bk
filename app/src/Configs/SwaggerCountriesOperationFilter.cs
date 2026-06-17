using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ZelosHR.Api.Configs;

/// <summary>Country catalog OpenAPI examples — same workflow as currencies on employees.</summary>
public sealed class SwaggerCountriesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.ApiDescription.HttpMethod ?? "";
        var path = context.ApiDescription.RelativePath ?? "";
        if (!path.StartsWith("api/v1/countries", StringComparison.OrdinalIgnoreCase))
            return;

        switch (path)
        {
            case "api/v1/countries/list" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.CountryListResponse());
                operation.Summary ??= "List countries";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    "Use returned `name` as `country` on POST /leave/holidays/add and PUT /leave/holidays/update.");
                return;

            case "api/v1/countries/get" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.CountryGetResponse());
                SetJsonResponseExample(operation, 404, SwaggerExamples.CountryNotFoundResponse());
                operation.Summary ??= "Get country";
                AppendParameterDescription(operation, "country_id",
                    "Country id from GET /countries/list (e.g. ctr_gh).");
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
