using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using ZelosHR.Api.Entities.EmployeePortal;
using ZelosHR.Api.Entities.Shared;

namespace ZelosHR.Api.Configs;

/// <summary>Company Settings + Employee Portal — portal subdomain OpenAPI examples.</summary>
public sealed class SwaggerEmployeePortalSubdomainOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.ApiDescription.HttpMethod ?? "";
        var path = context.ApiDescription.RelativePath ?? "";

        if (path.StartsWith("api/v1/company/portal-subdomain", StringComparison.OrdinalIgnoreCase))
        {
            ApplyCompanySettings(operation, method, path);
            return;
        }

        if (path.StartsWith("api/v1/employee-portal/activation", StringComparison.OrdinalIgnoreCase))
        {
            ApplyActivation(operation, method, path);
            return;
        }

        if (path.StartsWith("api/v1/employee-portal/password-reset", StringComparison.OrdinalIgnoreCase))
        {
            ApplyPasswordReset(operation, method, path);
            return;
        }

        if (path.Equals("api/v1/public/employee-portal/resolve", StringComparison.OrdinalIgnoreCase)
            && method.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            SetJsonResponseExample(operation, 200, SwaggerExamples.EmployeePortalResolveResponse());
            SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<EmployeePortalResolveDto>), 404));
            operation.Summary ??= "Resolve employee portal subdomain";
            operation.Description = SwaggerOptionFormat.Append(operation.Description,
                """
                Public bootstrap for the employee portal app. No JWT or Trove headers required.
                Returns tenant_id, org_id, bus_id, loc_id, and app_id for login/API calls.
                """);
            return;
        }
    }

    private static void ApplyActivation(OpenApiOperation operation, string method, string path)
    {
        switch (path)
        {
            case "api/v1/employee-portal/activation/validate" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.EmployeeActivationValidateResponse());
                operation.Summary ??= "Validate employee activation token";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    "No auth. Returns whether the token is valid, expired, or already used.");
                return;

            case "api/v1/employee-portal/activation/set-password" when method.Equals("POST", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.EmployeeActivationSetPasswordResponse());
                SetJsonResponseExample(operation, 400, SwaggerExamples.EnvelopeFor(typeof(Respons<EmployeeActivationSetPasswordResultDto>), 400));
                operation.Summary ??= "Set password via activation token";
                return;

            case "api/v1/employee-portal/activation/resend" when method.Equals("POST", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.EmployeeActivationResendResponse());
                operation.Summary ??= "Resend employee activation link";
                return;
        }
    }

    private static void ApplyPasswordReset(OpenApiOperation operation, string method, string path)
    {
        switch (path)
        {
            case "api/v1/employee-portal/password-reset/request" when method.Equals("POST", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.EmployeePasswordResetRequestResponse());
                SetJsonResponseExample(operation, 429, SwaggerExamples.EnvelopeFor(typeof(Respons<EmployeePasswordResetRequestResultDto>), 429));
                operation.Summary ??= "Request employee password reset link";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    "No auth. Always returns a generic success message when the account is eligible.");
                return;

            case "api/v1/employee-portal/password-reset/validate" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.EmployeePasswordResetValidateResponse());
                operation.Summary ??= "Validate employee password reset token";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    "No auth. Returns whether the token is valid, expired, or the account is not yet activated.");
                return;

            case "api/v1/employee-portal/password-reset/set-password" when method.Equals("POST", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.EmployeePasswordResetSetPasswordResponse());
                SetJsonResponseExample(operation, 400, SwaggerExamples.EnvelopeFor(typeof(Respons<EmployeePasswordResetSetPasswordResultDto>), 400));
                operation.Summary ??= "Reset password via reset token";
                return;

            case "api/v1/employee-portal/password-reset/resend" when method.Equals("POST", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.EmployeePasswordResetRequestResponse());
                SetJsonResponseExample(operation, 429, SwaggerExamples.EnvelopeFor(typeof(Respons<EmployeePasswordResetRequestResultDto>), 429));
                operation.Summary ??= "Resend employee password reset link";
                return;
        }
    }

    private static void ApplyCompanySettings(OpenApiOperation operation, string method, string path)
    {
        switch (path)
        {
            case "api/v1/company/portal-subdomain/list" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.EmployeePortalSubdomainListResponse());
                operation.Summary ??= "List employee portal subdomain settings";
                return;

            case "api/v1/company/portal-subdomain/get" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.EmployeePortalSubdomainGetResponse());
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<EmployeePortalSubdomainReadDto>), 404));
                operation.Summary ??= "Get employee portal subdomain settings";
                return;

            case "api/v1/company/portal-subdomain/add" when method.Equals("POST", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 400, SwaggerExamples.EnvelopeFor(typeof(Respons<EmployeePortalSubdomainReadDto>), 400));
                operation.Summary ??= "Create employee portal subdomain";
                return;

            case "api/v1/company/portal-subdomain/update" when method.Equals("PUT", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 400, SwaggerExamples.EnvelopeFor(typeof(Respons<EmployeePortalSubdomainReadDto>), 400));
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<EmployeePortalSubdomainReadDto>), 404));
                operation.Summary ??= "Update employee portal subdomain";
                return;

            case "api/v1/company/portal-subdomain/delete" when method.Equals("DELETE", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<object>), 404));
                operation.Summary ??= "Delete employee portal subdomain";
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
