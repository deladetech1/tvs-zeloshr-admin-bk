namespace ZelosHR.Api.Configs;

/// <summary>
/// Maps Core Platform's <c>SECRET_KEY</c> env var into Trovesuite.Package JWT settings.
/// </summary>
internal static class JwtSecretConfiguration
{
    private const string CorePlatformEnvName = "SECRET_KEY";
    private const string TrovesuiteJwtKey = "Trovesuite:Jwt:SecretKey";
    private const string AppSecretKey = $"{AppSettings.SectionName}:SecretKey";

    internal static void Apply(ConfigurationManager configuration)
    {
        var fromCorePlatform = NullIfWhiteSpace(configuration[CorePlatformEnvName]);
        var trovesuiteJwt = NullIfWhiteSpace(configuration[TrovesuiteJwtKey]);
        var appSecret = NullIfWhiteSpace(configuration[AppSecretKey]);

        var resolved = trovesuiteJwt ?? appSecret ?? fromCorePlatform;
        if (resolved is null)
            return;

        configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [TrovesuiteJwtKey] = resolved,
            [AppSecretKey] = resolved,
        });
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
