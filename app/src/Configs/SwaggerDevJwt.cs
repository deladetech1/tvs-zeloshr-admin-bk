using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using ZelosHR.Api.Shared.Constants;

namespace ZelosHR.Api.Configs;

/// <summary>Issues short-lived JWTs for local Swagger (development only).</summary>
internal static class SwaggerDevJwt
{
    public static string CreateToken(
        IConfiguration configuration,
        string userId,
        string tenantId,
        TimeSpan? lifetime = null)
    {
        var secret = configuration["Trovesuite:Jwt:SecretKey"]
            ?? configuration["App:SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey is not configured.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(CorePlatformConstants.JwtClaims.UserId, userId),
            new Claim(CorePlatformConstants.JwtClaims.TenantId, tenantId),
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.Add(lifetime ?? TimeSpan.FromHours(8)),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
