using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using ZelosHR.Api.Entities.Currencies;
using ZelosHR.Api.Entities.Shared;

namespace ZelosHR.Api.Configs;

/// <summary>Path-specific currency OpenAPI examples (Mystoreguard-aligned).</summary>
public sealed class SwaggerCurrenciesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.ApiDescription.HttpMethod ?? "";
        var path = context.ApiDescription.RelativePath ?? "";

        if (method.Equals("GET", StringComparison.OrdinalIgnoreCase)
            && path.Equals("api/v1/currencies/list", StringComparison.OrdinalIgnoreCase))
        {
            SetJsonResponseExample(operation, 200, SwaggerExamples.CurrencyListResponse());
            AppendParameterDescription(operation, "is_active",
                "Optional filter: true = active only, false = inactive only, omit = all non-deleted currencies.");
            return;
        }

        if (method.Equals("GET", StringComparison.OrdinalIgnoreCase)
            && path.Equals("api/v1/currencies/get", StringComparison.OrdinalIgnoreCase))
        {
            SetJsonResponseExample(operation, 200, SwaggerExamples.CurrencyGetResponse());
            SetJsonResponseExample(operation, 404, SwaggerExamples.CurrencyNotFoundResponse());
            AppendParameterDescription(operation, "currency_id",
                "Currency ID from GET /currencies/list (e.g. cur_ghs_default). Used as compensation.currency_id on employees.");
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

        media.Example = example;
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
