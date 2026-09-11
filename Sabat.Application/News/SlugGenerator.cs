using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Sabat.Application.News;

public static partial class SlugGenerator
{
    private static readonly IReadOnlyDictionary<char, string> Transliteration =
        new Dictionary<char, string>
        {
            ['а'] = "a", ['б'] = "b", ['в'] = "v", ['г'] = "g", ['д'] = "d",
            ['е'] = "e", ['ё'] = "yo", ['ж'] = "zh", ['з'] = "z", ['и'] = "i",
            ['й'] = "y", ['к'] = "k", ['л'] = "l", ['м'] = "m", ['н'] = "n",
            ['о'] = "o", ['п'] = "p", ['р'] = "r", ['с'] = "s", ['т'] = "t",
            ['у'] = "u", ['ф'] = "f", ['х'] = "h", ['ц'] = "ts", ['ч'] = "ch",
            ['ш'] = "sh", ['щ'] = "sch", ['ъ'] = "", ['ы'] = "y", ['ь'] = "",
            ['э'] = "e", ['ю'] = "yu", ['я'] = "ya", ['ң'] = "ng", ['ө'] = "o",
            ['ү'] = "u"
        };

    public static string Generate(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(Transliteration.TryGetValue(character, out var replacement)
                ? replacement
                : character);
        }

        var slug = InvalidCharacters().Replace(builder.ToString(), "-");
        return RepeatedHyphens().Replace(slug, "-").Trim('-');
    }

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex InvalidCharacters();

    [GeneratedRegex("-{2,}", RegexOptions.CultureInvariant)]
    private static partial Regex RepeatedHyphens();
}
