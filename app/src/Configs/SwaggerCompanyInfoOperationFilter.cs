using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using ZelosHR.Api.Entities.CompanyInfo;
using ZelosHR.Api.Entities.Shared;

namespace ZelosHR.Api.Configs;

/// <summary>Company Settings — company profile and offices OpenAPI examples and parameter hints.</summary>
public sealed class SwaggerCompanyInfoOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.ApiDescription.HttpMethod ?? "";
        var path = context.ApiDescription.RelativePath ?? "";
        if (!path.StartsWith("api/v1/company/info", StringComparison.OrdinalIgnoreCase))
            return;

        switch (path)
        {
            case "api/v1/company/info/get" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<CompanyInfoReadDto>), 404));
                operation.Summary ??= "Get company profile";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    "One profile per org. Every office for the org is embedded in offices[] — no pagination, no separate list call.");
                return;

            case "api/v1/company/info/add" when method.Equals("POST", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 400, SwaggerExamples.EnvelopeFor(typeof(Respons<CompanyInfoReadDto>), 400));
                operation.Summary ??= "Create company profile";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    """
                    Required: legal_name. Optional: trading_name · industry · company_size · business_registration_number · tin · primary_work_country · company_email · website · logo_url · banner_url (document ids from POST /file/post/multiple) · an initial offices[] array.
                    400 if a profile already exists for this org — use PUT /update instead.
                    """);
                return;

            case "api/v1/company/info/update" when method.Equals("PUT", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 400, SwaggerExamples.EnvelopeFor(typeof(Respons<CompanyInfoReadDto>), 400));
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<CompanyInfoReadDto>), 404));
                operation.Summary ??= "Update company profile";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    """
                    Partial body — send only fields to change.
                    offices[] is a full-replacement array when present: an entry with no office_id is created, an entry whose office_id matches an existing office replaces that office's fields in full, and any existing office missing from the array is deleted. Omit offices entirely to leave them untouched; send [] to delete every office.
                    Use PUT /offices/update?office_id= instead for a single-field edit on one office.
                    """);
                return;

            case "api/v1/company/info/delete" when method.Equals("DELETE", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<object>), 404));
                operation.Summary ??= "Delete company profile";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    "Also deletes every office for this org, in one transaction.");
                return;

            case "api/v1/company/info/offices/update" when method.Equals("PUT", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 400, SwaggerExamples.EnvelopeFor(typeof(Respons<CompanyOfficeReadDto>), 400));
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<CompanyOfficeReadDto>), 404));
                operation.Summary ??= "Update one office";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    "Partial body — name · country · city · phone · is_head_office. For one-field edits, instead of resending the full offices[] array on PUT /update.");
                AppendParameterDescription(operation, "office_id", "Office UUID from the offices[] array on GET /get.");
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
