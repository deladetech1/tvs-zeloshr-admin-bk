using ZelosHR.Api.Shared.Abstractions;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Shared.Infrastructure;

public sealed class HttpContextCurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid? UserId
    {
        get
        {
            var raw = httpContextAccessor.HttpContext?.Items[TrovesuiteHttpContextKeys.UserId] as string;
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public string? Role => null;

    public bool HasPermission(string permission) =>
        httpContextAccessor.HttpContext?.Items[TrovesuiteHttpContextKeys.Permissions] is IReadOnlyList<string> list
        && list.Contains(permission, StringComparer.OrdinalIgnoreCase);
}
