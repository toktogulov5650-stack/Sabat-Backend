using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Sabat.Application.Admin;

namespace Sabat.API.Security;

public sealed class JwtAdminTokenService(
    IOptions<AdminJwtOptions> options,
    TimeProvider timeProvider) : IAdminTokenService
{
    private readonly AdminJwtOptions _options = options.Value;

    public IssuedAdminToken Issue(AuthenticatedAdmin authenticatedAdmin)
    {
        var issuedAt = timeProvider.GetUtcNow();
        var expiresAt = issuedAt.AddMinutes(_options.ExpirationMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, authenticatedAdmin.Admin.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, authenticatedAdmin.Admin.Id.ToString()),
            new Claim(ClaimTypes.Email, authenticatedAdmin.Admin.Email),
            new Claim(ClaimTypes.Name, authenticatedAdmin.Admin.DisplayName),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(AdminJwtOptions.AuthenticationSourceClaim, AdminJwtOptions.SessionSource),
            new Claim(AdminJwtOptions.SessionVersionClaim, authenticatedAdmin.SessionVersion.ToString())
        };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: issuedAt.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new IssuedAdminToken(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt,
            WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32)));
    }
}
