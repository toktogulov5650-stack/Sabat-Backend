namespace Sabat.API.Security;

public sealed class AdminApiKeyOptions
{
    public const string SectionName = "Admin";
    public const string SchemeName = "AdminApiKey";
    public const string ApiKeyPolicyName = "AdminApiKeyOnly";
    public const string SessionPolicyName = "AdminSessionOnly";
    public const string SelectorSchemeName = "AdminAuthentication";
    public const string HeaderName = "X-Admin-Key";

    public string ApiKey { get; init; } = string.Empty;
}
