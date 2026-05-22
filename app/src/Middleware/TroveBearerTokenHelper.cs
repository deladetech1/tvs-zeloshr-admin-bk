using System.IdentityModel.Tokens.Jwt;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Middleware;

internal static class TroveBearerTokenHelper
{
    public static string? ExtractBearerToken(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(TroveStandardHeaders.Authorization, out var values)
            && !context.Request.Headers.TryGetValue("Authorization", out values))
            return null;

        var header = values.ToString();
        if (string.IsNullOrWhiteSpace(header))
            return null;

        const string prefix = "Bearer ";
        return header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? header[prefix.Length..].Trim()
            : null;
    }

    /// <summary>Reads JWT claims without signature validation (dev / header bootstrap only).</summary>
    public static IReadOnlyDictionary<string, string> ReadUnvalidatedClaims(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        if (!handler.CanReadToken(token))
            return new Dictionary<string, string>();

        var jwt = handler.ReadJwtToken(token);
        return jwt.Claims
            .GroupBy(c => c.Type, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First().Value, StringComparer.Ordinal);
    }
}
