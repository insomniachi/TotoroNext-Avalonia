using System.Text;
using System.Text.Unicode;
using LanguageDetection;

namespace TotoroNext.Module;

public static class TextHelpers
{
    private static LanguageDetector? _detector;
    private static readonly string[] ForeignLanguages = 
    [
        "deu", "spa", "ita", "fra", "ron", "por", "tur", "pol",
        "nor", "lav", "vie", "dan", "ces", "hun"
    ];

    public static bool IsNotEnglishOrRomaji(string input)
    {
        if (_detector is null)
        {
            _detector = new LanguageDetector();
            _detector.AddAllLanguages();
        }

        var language = _detector.Detect(input);
        return ForeignLanguages.Contains(language);
    }

    public static bool IsLatin(string input)
    {
        foreach (var rune in input.EnumerateRunes())
        {
            if (!Rune.IsLetter(rune))
            {
                continue;
            }

            var code = rune.Value;
            var inLatin = InRange(code, UnicodeRanges.BasicLatin) || InRange(code, UnicodeRanges.Latin1Supplement) ||
                          InRange(code, UnicodeRanges.LatinExtendedA) || InRange(code, UnicodeRanges.LatinExtendedB) ||
                          InRange(code, UnicodeRanges.LatinExtendedAdditional);

            if (!inLatin)
            {
                return false;
            }
        }

        return true;
    }

    private static bool InRange(int codePoint, UnicodeRange range)
    {
        return codePoint >= range.FirstCodePoint && codePoint < range.FirstCodePoint + range.Length;
    }
}