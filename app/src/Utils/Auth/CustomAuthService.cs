namespace ZelosHR.Api.Utils.Auth;

/// <summary>
/// JWT authentication and authorization helpers.
/// Integrate with Trovesuite shared library when available.
/// </summary>
public static class CustomAuthService
{
    public const string AppId = "app-hr";

    public static readonly HashSet<string> BlockingErrors = new(StringComparer.OrdinalIgnoreCase)
    {
        "TENANT_NOT_VERIFIED",
        "TENANT_NOT_FOUND",
        "INVALID_TENANT_ID",
        "USER_SUSPENDED",
        "LOGIN_TIME_RESTRICTED",
        "LOGIN_DAY_RESTRICTED",
    };
}
