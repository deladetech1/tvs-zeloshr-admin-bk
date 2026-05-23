namespace ZelosHR.Api.Shared.Constants;

/// <summary>Values aligned with <c>core_platform</c> schema and Trovesuite JWT conventions.</summary>
public static class CorePlatformConstants
{
    public static class DeleteStatus
    {
        public const string NotDeleted = "NOT_DELETED";
    }

    public static class JwtClaims
    {
        public const string TenantId = "tenant_id";
        public const string UserId = "user_id";
    }
}
