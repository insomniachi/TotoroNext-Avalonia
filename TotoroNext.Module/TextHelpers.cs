using System.Reflection;
using System.Text;
using System.Text.Unicode;
using NTextCat;

namespace TotoroNext.Module;

public static class TextHelpers
{
    private static RankedLanguageIdentifier? _identifier; 
    
    public static bool IsEnglish(string input)
    {
        if (_identifier is null)
        {
            var factory = new RankedLanguageIdentifierFactory();
            var assembly = Assembly.GetExecutingAssembly();
            const string resourceName = "TotoroNext.Module.Core14.profile.xml";
            var stream = assembly.GetManifestResourceStream(resourceName);
            _identifier = factory.Load(stream);
        }
        
        var languages = _identifier.Identify(input);
        var mostLikelyLanguage = languages.FirstOrDefault();
        return mostLikelyLanguage != null && mostLikelyLanguage.Item1.Iso639_3 == "eng";
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