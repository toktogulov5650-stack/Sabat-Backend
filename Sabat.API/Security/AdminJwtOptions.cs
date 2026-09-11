namespace Sabat.API.Security;

public sealed class AdminJwtOptions
{
    public const string SectionName = "Admin:Jwt";
    public const string SchemeName = "AdminSession";
    public const string AuthenticationSourceClaim = "sabat_auth_source";
    public const string SessionSource = "session";
    public const string ApiKeySource = "api_key";
    public const string SessionVersionClaim = "sabat_session_version";
    public const string CsrfHeaderName = "X-CSRF-Token";

    public string SigningKey { get; init; } = string.Empty;
    public string Issuer { get; init; } = "sabat-api";
    public string Audience { get; init; } = "sabat-admin";
    public string CookieName { get; init; } = "sabat_admin_session";
    public string CsrfCookieName { get; init; } = "sabat_admin_csrf";
    public int ExpirationMinutes { get; init; } = 480;
}
