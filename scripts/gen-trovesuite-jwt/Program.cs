using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

var secret = args.Length > 0 ? args[0] : "change-me-in-production";
var userId = args.Length > 1 ? args[1] : "u1000001-0000-4000-8000-000000000001";
var tenantId = args.Length > 2 ? args[2] : "demo-tenant";

var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
var claims = new[]
{
    new Claim("user_id", userId),
    new Claim("tenant_id", tenantId),
};
var token = new JwtSecurityToken(
    claims: claims,
    expires: DateTime.UtcNow.AddHours(1),
    signingCredentials: creds);
Console.WriteLine(new JwtSecurityTokenHandler().WriteToken(token));
