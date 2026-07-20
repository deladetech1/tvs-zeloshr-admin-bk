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
                SetJsonResponseExample(operation, 200, SwaggerExamples.EnvelopeOk(SwaggerExamples.CompanyInfoStubData()));
                operation.Summary ??= "Get company profile";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    """
                    Always 200 for a valid org. If no profile exists, creates an empty stub on this GET (side effect — see docs/COMPANY_INFO_GET_AUTO_INIT.md).
                    Response includes configured: false until legal_name is saved via PUT /update. offices[] is empty on a fresh stub.
                    """);
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
                    Same shape as POST /add, plus id (must match the profile's current id from GET /get). This is a full replacement, not a partial patch — omitted optional fields are cleared.
                    offices[] is a full-replacement array when present: an entry with no office_id is created, an entry whose office_id matches an existing office replaces that office's fields in full, and any existing office missing from the array is deleted. Omit offices entirely to leave them untouched; send [] to delete every office.
                    """);
                return;

            case "api/v1/company/info/delete" when method.Equals("DELETE", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<object>), 404));
                operation.Summary ??= "Delete company profile";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    "Also deletes every office for this org, in one transaction.");
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
}
