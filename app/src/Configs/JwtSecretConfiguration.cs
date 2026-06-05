namespace ZelosHR.Api.Configs;

/// <summary>
/// Maps Core Platform's <c>SECRET_KEY</c> env var into Trovesuite.Package JWT settings.
/// </summary>
internal static class JwtSecretConfiguration
{
    public const int MinimumKeyLength = 32;

    private const string CorePlatformEnvName = "SECRET_KEY";
    private const string TrovesuiteJwtKey = "Trovesuite:Jwt:SecretKey";
    private const string AppSecretKey = $"{AppSettings.SectionName}:SecretKey";

    public const string MissingKeyMessage =
        "JWT signing key is not configured. On the Container App set SECRET_KEY (same secret as Core Platform) "
        + "or Trovesuite__Jwt__SecretKey (≥ 32 characters). See docs/PRODUCTION_CONFIG.md.";

    internal static void Apply(ConfigurationManager configuration)
    {
        var resolved = TryResolve(configuration);
        if (resolved is null)
            return;

        configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [TrovesuiteJwtKey] = resolved,
            [AppSecretKey] = resolved,
        });
    }

    internal static string? TryResolve(IConfiguration configuration)
    {
        var fromCorePlatform = NullIfWhiteSpace(configuration[CorePlatformEnvName]);
        var trovesuiteJwt = NullIfWhiteSpace(configuration[TrovesuiteJwtKey]);
        var appSecret = NullIfWhiteSpace(configuration[AppSecretKey]);

        return trovesuiteJwt ?? appSecret ?? fromCorePlatform;
    }

    internal static bool IsConfigured(IConfiguration configuration) =>
        TryResolve(configuration) is { Length: >= MinimumKeyLength };

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
