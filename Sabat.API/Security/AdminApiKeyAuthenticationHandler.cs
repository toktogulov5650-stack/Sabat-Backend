using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Sabat.API.Security;

public sealed class AdminApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> schemes,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptions<AdminApiKeyOptions> adminOptions)
    : AuthenticationHandler<AuthenticationSchemeOptions>(schemes, logger, encoder)
{
    private readonly AdminApiKeyOptions _adminOptions = adminOptions.Value;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (string.IsNullOrWhiteSpace(_adminOptions.ApiKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("Administrative API is not configured."));
        }

        if (!Request.Headers.TryGetValue(AdminApiKeyOptions.HeaderName, out var suppliedValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var supplied = suppliedValues.ToString();
        if (!FixedTimeEquals(supplied, _adminOptions.ApiKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid administrative API key."));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "sabat-admin"),
            new Claim(ClaimTypes.Name, "Sabat administrator"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(AdminJwtOptions.AuthenticationSourceClaim, AdminJwtOptions.ApiKeySource)
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }

    private static bool FixedTimeEquals(string supplied, string expected)
    {
        var suppliedBytes = Encoding.UTF8.GetBytes(supplied);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        return suppliedBytes.Length == expectedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(suppliedBytes, expectedBytes);
    }
}
