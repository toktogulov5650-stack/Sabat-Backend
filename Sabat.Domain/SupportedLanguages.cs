namespace Sabat.Domain;

public static class SupportedLanguages
{
    public static readonly string[] All = ["ru", "ky", "en"];

    public static bool Contains(string? language) =>
        language is not null && All.Contains(language, StringComparer.OrdinalIgnoreCase);

    public static string Normalize(string? language) =>
        Contains(language) ? language!.ToLowerInvariant() : "ru";
}
