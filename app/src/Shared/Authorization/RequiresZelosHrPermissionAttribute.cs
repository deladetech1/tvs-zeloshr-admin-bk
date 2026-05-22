using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Abstractions;

namespace ZelosHR.Api.Shared.Authorization;

/// <summary>
/// Enforces a ZelosHR permission when <see cref="TrovesuiteIntegrationOptions.RequireAuthentication"/> is true.
/// Demo mode (header-based tenant) skips the check so local UI development keeps working.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequiresZelosHrPermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    public RequiresZelosHrPermissionAttribute(string permission) => Permission = permission;

    public string Permission { get; }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var integration = context.HttpContext.RequestServices
            .GetRequiredService<IOptions<TrovesuiteIntegrationOptions>>().Value;
        if (!integration.RequireAuthentication)
            return;

        var currentUser = context.HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();
        if (currentUser.HasPermission(Permission))
            return;

        context.Result = new ObjectResult(Respons<object>.Fail(
            $"Missing permission: {Permission}.",
            statusCode: StatusCodes.Status403Forbidden))
        {
            StatusCode = StatusCodes.Status403Forbidden,
        };

        await Task.CompletedTask;
    }
}
